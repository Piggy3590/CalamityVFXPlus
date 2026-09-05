using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;
using VFXPlus.Common.Drawing;

namespace CalamityVFXPlus.Core.Systems;

/// <summary>
/// Hardens CalamiFX+ draw actions that are deferred through VFX+'s PixelationSystem.
///
/// A failed deferred draw must not escape into PixelationTarget.DrawPixelTarget: VFX+ only
/// clears its action list and restores the SpriteBatch after all queued actions complete.
/// If one beam throws in the middle, the remaining queue and graphics state can otherwise be
/// left dirty for the rest of the frame (or longer).
///
/// This system also disables VFX+'s legacy screen-flash state when that implementation exists.
/// That renderer hooks FilterManager.EndCapture and has historically been a source of unstable
/// render-target state, especially when other full-screen filters are active.
/// </summary>
public sealed class RenderSafetySystem : ModSystem
{
    private const int MaxCalamiFxActionsPerLayer = 128;

    private Hook _queueLayerHook;
    private Hook _queueStringHook;

    private static FieldInfo _legacyFlashActiveField;
    private static FieldInfo _legacyFlashTimeField;
    private static bool _legacyFlashReflectionResolved;

    private static readonly HashSet<string> LoggedFailures = new();
    private static readonly HashSet<RenderLayer> LoggedBudgetDrops = new();

    private delegate void QueueLayerOrig(PixelationSystem self, RenderLayer renderType, Action renderAction, int order);
    private delegate void QueueStringOrig(PixelationSystem self, string id, Action renderAction, int order);

    public override void Load()
    {
        if (Main.dedServ)
            return;

        MethodInfo queueLayer = typeof(PixelationSystem).GetMethod(
            nameof(PixelationSystem.QueueRenderAction),
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(RenderLayer), typeof(Action), typeof(int) },
            modifiers: null);

        MethodInfo queueString = typeof(PixelationSystem).GetMethod(
            nameof(PixelationSystem.QueueRenderAction),
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(string), typeof(Action), typeof(int) },
            modifiers: null);

        if (queueLayer != null)
            _queueLayerHook = new Hook(queueLayer, QueueLayerImpl);

        if (queueString != null)
            _queueStringHook = new Hook(queueString, QueueStringImpl);

        ResolveLegacyFlashFields();
    }

    public override void Unload()
    {
        _queueLayerHook?.Dispose();
        _queueStringHook?.Dispose();
        _queueLayerHook = null;
        _queueStringHook = null;

        _legacyFlashActiveField = null;
        _legacyFlashTimeField = null;
        _legacyFlashReflectionResolved = false;

        LoggedFailures.Clear();
        LoggedBudgetDrops.Clear();
    }

    public override void OnWorldLoad()
    {
        LoggedFailures.Clear();
        LoggedBudgetDrops.Clear();
    }

    public override void PostUpdateEverything()
    {
        // Older VFX+ builds expose FlashSystem and hook FilterManager.EndCapture. CalamiFX+
        // used SetCAFlashEffect on several beam weapons. Keep that legacy state disabled;
        // the visual flash is non-essential and should never be able to poison the screen target.
        DisableLegacyScreenFlash();
    }

    private static void QueueLayerImpl(
        QueueLayerOrig orig,
        PixelationSystem self,
        RenderLayer renderType,
        Action renderAction,
        int order)
    {
        if (!IsCalamiFxAction(renderAction))
        {
            orig(self, renderType, renderAction, order);
            return;
        }

        if (ShouldDropAction(self, renderType))
            return;

        orig(self, renderType, SafeRenderAction.Wrap(renderAction, renderType.ToString()), order);
    }

    private static void QueueStringImpl(
        QueueStringOrig orig,
        PixelationSystem self,
        string id,
        Action renderAction,
        int order)
    {
        if (!IsCalamiFxAction(renderAction))
        {
            orig(self, id, renderAction, order);
            return;
        }

        RenderLayer renderType = ResolveRenderLayer(id);
        if (ShouldDropAction(self, renderType))
            return;

        orig(self, id, SafeRenderAction.Wrap(renderAction, id ?? renderType.ToString()), order);
    }

    private static bool IsCalamiFxAction(Action action)
    {
        Assembly declaringAssembly = action?.Method?.DeclaringType?.Assembly;
        return declaringAssembly == typeof(RenderSafetySystem).Assembly;
    }

    private static bool ShouldDropAction(PixelationSystem system, RenderLayer layer)
    {
        if (system?.pixelationTargets == null)
            return false;

        PixelationTarget target = system.pixelationTargets.Find(t => t.renderType == layer);
        int queuedCount = target?.pixelationDrawActions?.Count ?? 0;

        if (queuedCount < MaxCalamiFxActionsPerLayer)
            return false;

        if (LoggedBudgetDrops.Add(layer))
        {
            LogWarning($"Dropped excess CalamiFX+ pixelation draw actions on layer {layer}. " +
                       $"The per-pass safety limit is {MaxCalamiFxActionsPerLayer} actions.");
        }

        return true;
    }

    private static RenderLayer ResolveRenderLayer(string id)
    {
        return id switch
        {
            "UnderTiles" => RenderLayer.UnderTiles,
            "UnderNPCs" => RenderLayer.UnderNPCs,
            "UnderProjectiles" => RenderLayer.UnderProjectiles,
            "OverPlayers" => RenderLayer.OverPlayers,
            _ => RenderLayer.Dusts
        };
    }

    private static void ResolveLegacyFlashFields()
    {
        if (_legacyFlashReflectionResolved)
            return;

        _legacyFlashReflectionResolved = true;

        try
        {
            Type flashSystem = typeof(PixelationSystem).Assembly.GetType("VFXPlus.Common.FlashSystem");
            if (flashSystem == null)
                return;

            _legacyFlashActiveField = flashSystem.GetField(
                "FlashActive",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            _legacyFlashTimeField = flashSystem.GetField(
                "FlashTime",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        }
        catch (Exception ex)
        {
            LogFailureOnce("legacy-flash-reflection", ex);
        }
    }

    private static void DisableLegacyScreenFlash()
    {
        if (!_legacyFlashReflectionResolved)
            ResolveLegacyFlashFields();

        try
        {
            _legacyFlashActiveField?.SetValue(null, false);
            _legacyFlashTimeField?.SetValue(null, 0);
        }
        catch (Exception ex)
        {
            LogFailureOnce("legacy-flash-disable", ex);
        }
    }

    private sealed class SafeRenderAction
    {
        private readonly Action _inner;
        private readonly string _context;
        private readonly ProjectileStamp[] _projectiles;

        private SafeRenderAction(Action inner, string context)
        {
            _inner = inner;
            _context = context;
            _projectiles = CaptureProjectiles(inner);
        }

        public static Action Wrap(Action inner, string context)
        {
            if (inner == null || inner.Target is SafeRenderAction)
                return inner;

            return new SafeRenderAction(inner, context).Invoke;
        }

        private void Invoke()
        {
            if (!CapturedProjectilesAreValid())
                return;

            try
            {
                _inner();
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                LogFailureOnce(_context, ex);
                RestorePixelationSpriteBatch();
            }
        }

        private bool CapturedProjectilesAreValid()
        {
            for (int i = 0; i < _projectiles.Length; i++)
            {
                if (!_projectiles[i].IsValid())
                    return false;
            }

            return true;
        }

        private static ProjectileStamp[] CaptureProjectiles(Action action)
        {
            object target = action?.Target;
            if (target == null)
                return Array.Empty<ProjectileStamp>();

            List<ProjectileStamp> result = null;

            try
            {
                FieldInfo[] fields = target.GetType().GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (FieldInfo field in fields)
                {
                    if (!typeof(Projectile).IsAssignableFrom(field.FieldType))
                        continue;

                    if (field.GetValue(target) is not Projectile projectile)
                        continue;

                    result ??= new List<ProjectileStamp>(2);
                    result.Add(new ProjectileStamp(projectile));
                }
            }
            catch
            {
                // Validation is an additional guard. Failure to inspect a compiler-generated
                // closure should not stop the actual draw action from being queued.
            }

            return result?.ToArray() ?? Array.Empty<ProjectileStamp>();
        }
    }

    private readonly struct ProjectileStamp
    {
        private readonly int _whoAmI;
        private readonly int _identity;
        private readonly int _type;

        public ProjectileStamp(Projectile projectile)
        {
            _whoAmI = projectile.whoAmI;
            _identity = projectile.identity;
            _type = projectile.type;
        }

        public bool IsValid()
        {
            if ((uint)_whoAmI >= (uint)Main.maxProjectiles)
                return false;

            Projectile projectile = Main.projectile[_whoAmI];
            return projectile != null &&
                   projectile.active &&
                   projectile.identity == _identity &&
                   projectile.type == _type;
        }
    }

    private static void RestorePixelationSpriteBatch()
    {
        SpriteBatch spriteBatch = Main.spriteBatch;
        if (spriteBatch == null)
            return;

        try
        {
            if (spriteBatch.beginCalled)
                spriteBatch.End();
        }
        catch
        {
            // Best-effort recovery; Begin below is the important part.
        }

        try
        {
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null);

            if (Main.graphics?.GraphicsDevice != null)
                Main.graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
        }
        catch (Exception ex)
        {
            LogFailureOnce("spritebatch-recovery", ex);
        }
    }

    private static void LogFailureOnce(string context, Exception exception)
    {
        string key = $"{context}|{exception.GetType().FullName}|{exception.Message}";
        if (!LoggedFailures.Add(key))
            return;

        if (ModLoader.TryGetMod("CalamiFXPlus", out Mod mod))
        {
            mod.Logger.Error(
                $"Suppressed a deferred VFX draw failure in '{context}' to keep the pixelation render queue valid.\n{exception}");
        }
    }

    private static void LogWarning(string message)
    {
        if (ModLoader.TryGetMod("CalamiFXPlus", out Mod mod))
            mod.Logger.Warn(message);
    }
}
