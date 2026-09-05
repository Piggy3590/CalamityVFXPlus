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
/// Hardens CalamiFX+ draw actions deferred through VFX+'s pixelation systems.
///
/// VFX+'s PixelationTarget.DrawPixelTarget clears its queued actions and restores the
/// SpriteBatch only after every action has run. If one beam throws in the middle, the whole
/// render-target pass can be left dirty. CalamiFX+ actions are therefore wrapped so one bad
/// draw cannot escape into VFX+'s queue.
///
/// The wrapper also rejects stale Projectile references captured by deferred lambdas and
/// disables VFX+'s legacy screen-flash state when that implementation exists.
/// </summary>
public sealed class RenderSafetySystem : ModSystem
{
    private const int MaxCalamiFxActionsPerLayer = 128;

    private Hook _pixelQueueLayerHook;
    private Hook _pixelQueueStringHook;
    private Hook _additiveQueueLayerHook;
    private Hook _additiveQueueStringHook;

    private static FieldInfo _legacyFlashActiveField;
    private static FieldInfo _legacyFlashTimeField;
    private static bool _legacyFlashReflectionResolved;

    private static readonly HashSet<string> LoggedFailures = new();
    private static readonly HashSet<string> LoggedBudgetDrops = new();

    private delegate void PixelQueueLayerOrig(PixelationSystem self, RenderLayer renderType, Action renderAction, int order);
    private delegate void PixelQueueStringOrig(PixelationSystem self, string id, Action renderAction, int order);
    private delegate void AdditiveQueueLayerOrig(AdditivePixelationSystem self, RenderLayer renderType, Action renderAction, int order);
    private delegate void AdditiveQueueStringOrig(AdditivePixelationSystem self, string id, Action renderAction, int order);

    public override void Load()
    {
        if (Main.dedServ)
            return;

        _pixelQueueLayerHook = HookQueueMethod(
            typeof(PixelationSystem),
            new[] { typeof(RenderLayer), typeof(Action), typeof(int) },
            (PixelQueueLayerOrig)PixelQueueLayerImpl);

        _pixelQueueStringHook = HookQueueMethod(
            typeof(PixelationSystem),
            new[] { typeof(string), typeof(Action), typeof(int) },
            (PixelQueueStringOrig)PixelQueueStringImpl);

        _additiveQueueLayerHook = HookQueueMethod(
            typeof(AdditivePixelationSystem),
            new[] { typeof(RenderLayer), typeof(Action), typeof(int) },
            (AdditiveQueueLayerOrig)AdditiveQueueLayerImpl);

        _additiveQueueStringHook = HookQueueMethod(
            typeof(AdditivePixelationSystem),
            new[] { typeof(string), typeof(Action), typeof(int) },
            (AdditiveQueueStringOrig)AdditiveQueueStringImpl);

        ResolveLegacyFlashFields();
    }

    public override void Unload()
    {
        _pixelQueueLayerHook?.Dispose();
        _pixelQueueStringHook?.Dispose();
        _additiveQueueLayerHook?.Dispose();
        _additiveQueueStringHook?.Dispose();

        _pixelQueueLayerHook = null;
        _pixelQueueStringHook = null;
        _additiveQueueLayerHook = null;
        _additiveQueueStringHook = null;

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
        // calls SetCAFlashEffect from beam weapons. Keep that legacy state disabled; the flash
        // is cosmetic and should never be able to poison the final screen render target.
        DisableLegacyScreenFlash();
    }

    private static Hook HookQueueMethod(Type systemType, Type[] parameterTypes, Delegate detour)
    {
        MethodInfo method = systemType.GetMethod(
            "QueueRenderAction",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: parameterTypes,
            modifiers: null);

        return method == null ? null : new Hook(method, detour);
    }

    private static void PixelQueueLayerImpl(
        PixelQueueLayerOrig orig,
        PixelationSystem self,
        RenderLayer renderType,
        Action renderAction,
        int order)
    {
        if (!TryPrepareAction(self?.pixelationTargets, renderType, renderAction, "pixel", out Action safeAction))
            return;

        orig(self, renderType, safeAction, order);
    }

    private static void PixelQueueStringImpl(
        PixelQueueStringOrig orig,
        PixelationSystem self,
        string id,
        Action renderAction,
        int order)
    {
        RenderLayer layer = ResolveRenderLayer(id);
        if (!TryPrepareAction(self?.pixelationTargets, layer, renderAction, "pixel", out Action safeAction))
            return;

        orig(self, id, safeAction, order);
    }

    private static void AdditiveQueueLayerImpl(
        AdditiveQueueLayerOrig orig,
        AdditivePixelationSystem self,
        RenderLayer renderType,
        Action renderAction,
        int order)
    {
        if (!TryPrepareAction(self?.pixelationTargets, renderType, renderAction, "additive", out Action safeAction))
            return;

        orig(self, renderType, safeAction, order);
    }

    private static void AdditiveQueueStringImpl(
        AdditiveQueueStringOrig orig,
        AdditivePixelationSystem self,
        string id,
        Action renderAction,
        int order)
    {
        RenderLayer layer = ResolveRenderLayer(id);
        if (!TryPrepareAction(self?.pixelationTargets, layer, renderAction, "additive", out Action safeAction))
            return;

        orig(self, id, safeAction, order);
    }

    private static bool TryPrepareAction(
        List<PixelationTarget> targets,
        RenderLayer layer,
        Action renderAction,
        string queueKind,
        out Action safeAction)
    {
        safeAction = renderAction;

        if (!IsCalamiFxAction(renderAction))
            return true;

        PixelationTarget target = targets?.Find(t => t.renderType == layer);
        int queuedCount = target?.pixelationDrawActions?.Count ?? 0;
        if (queuedCount >= MaxCalamiFxActionsPerLayer)
        {
            string budgetKey = $"{queueKind}:{layer}";
            if (LoggedBudgetDrops.Add(budgetKey))
            {
                LogWarning($"Dropped excess CalamiFX+ {queueKind} draw actions on layer {layer}. " +
                           $"The per-pass safety limit is {MaxCalamiFxActionsPerLayer} actions.");
            }

            return false;
        }

        safeAction = SafeRenderAction.Wrap(renderAction, $"{queueKind}:{layer}");
        return true;
    }

    private static bool IsCalamiFxAction(Action action)
    {
        Assembly declaringAssembly = action?.Method?.DeclaringType?.Assembly;
        return declaringAssembly == typeof(RenderSafetySystem).Assembly;
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
                // Closure inspection is only an extra guard. Failure to inspect it must not
                // prevent the actual render action from being queued.
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
            // Best-effort recovery. Re-entering the batch below is the important part.
        }

        try
        {
            // This matches the state PixelationTarget.DrawPixelTarget establishes before
            // invoking queued actions.
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
