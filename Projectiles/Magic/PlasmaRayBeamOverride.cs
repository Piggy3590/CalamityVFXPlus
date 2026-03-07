using CalamityMod.Projectiles.Magic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics;
using Terraria.ModLoader;
using VFXPlus.Common.Drawing;
/*
namespace CalamityVFXPlus.Projectiles.Magic;

public class PlasmaRayBeamOverride : GlobalProjectile
{
    public override bool InstancePerEntity => true;

    private Effect beamEffect;

    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
    {
        return lateInstantiation && entity.type == ModContent.ProjectileType<PlasmaRay>();
    }

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (projectile.velocity == Vector2.Zero)
            return false;

        ModContent.GetInstance<PixelationSystem>().QueueRenderAction(RenderLayer.OverPlayers, () =>
        {
            DrawBeam(projectile);
        });

        return false;
    }

    private void DrawBeam(Projectile projectile)
    {
        beamEffect ??= ModContent.Request<Effect>(
            "VFXPlus/Effects/Scroll/ComboLaserVertexGradient",
            AssetRequestMode.ImmediateLoad).Value;

        Vector2 dir = projectile.velocity.SafeNormalize(Vector2.UnitX);
        
        float beamLength = MathHelper.Clamp(projectile.velocity.Length() * 10f, 80f, 220f);

        Vector2 startPoint = projectile.Center - dir * 6f;
        Vector2 endPoint = startPoint + dir * beamLength;

        Vector2[] posArr = { startPoint, endPoint };
        float rot = dir.ToRotation();
        float[] rotArr = { rot, rot };
        
        Color outerColor = new Color(80, 180, 255);
        Color innerColor = new Color(200, 255, 255);

        // ---------- Outer strip ----------
        float OuterWidth(float progress) => 42f * projectile.scale;

        VertexStrip outerStrip = new();
        outerStrip.PrepareStrip(
            posArr,
            rotArr,
            _ => Color.White,
            OuterWidth,
            -Main.screenPosition,
            includeBacksides: true
        );

        // ---------- Inner strip ----------
        float InnerWidth(float progress) => 18f * projectile.scale;

        VertexStrip innerStrip = new();
        innerStrip.PrepareStrip(
            posArr,
            rotArr,
            _ => Color.White,
            InnerWidth,
            -Main.screenPosition,
            includeBacksides: true
        );

        float dist = (endPoint - startPoint).Length();
        float repVal = dist / 700f;

        beamEffect.Parameters["WorldViewProjection"].SetValue(Main.GameViewMatrix.NormalizedTransformationmatrix);

        beamEffect.Parameters["onTex"].SetValue(ModContent.Request<Texture2D>(
            "VFXPlus/Assets/Trails/Clear/GlowTrailClear",
            AssetRequestMode.ImmediateLoad).Value);

        beamEffect.Parameters["gradientTex"].SetValue(VFXPlusTextures.YharimGrad.Value);

        beamEffect.Parameters["sampleTexture1"].SetValue(VFXPlusTextures.ThinGlowLine.Value);
        beamEffect.Parameters["sampleTexture2"].SetValue(VFXPlusTextures.spark_06.Value);
        beamEffect.Parameters["sampleTexture3"].SetValue(VFXPlusTextures.Extra_196_Black.Value);
        beamEffect.Parameters["sampleTexture4"].SetValue(VFXPlusTextures.Trail5Loop.Value);

        beamEffect.Parameters["grad1Speed"].SetValue(0.66f);
        beamEffect.Parameters["grad2Speed"].SetValue(0.66f);
        beamEffect.Parameters["grad3Speed"].SetValue(1.03f);
        beamEffect.Parameters["grad4Speed"].SetValue(0.77f);

        beamEffect.Parameters["gradientReps"].SetValue(0.6f * repVal);
        beamEffect.Parameters["tex1reps"].SetValue(1.15f * repVal);
        beamEffect.Parameters["tex2reps"].SetValue(1.15f * repVal);
        beamEffect.Parameters["tex3reps"].SetValue(1.15f * repVal);
        beamEffect.Parameters["tex4reps"].SetValue(1.15f * repVal);

        beamEffect.Parameters["uTime"].SetValue((float)Main.timeForVisualEffects * -0.03f);

        // ----- Outer pass -----
        beamEffect.Parameters["baseColor"].SetValue(outerColor.ToVector3() * 1.8f);
        beamEffect.Parameters["satPower"].SetValue(0.85f);
        beamEffect.Parameters["tex1Mult"].SetValue(1.1f);
        beamEffect.Parameters["tex2Mult"].SetValue(1.25f);
        beamEffect.Parameters["tex3Mult"].SetValue(1.0f);
        beamEffect.Parameters["tex4Mult"].SetValue(2.0f);
        beamEffect.Parameters["totalMult"].SetValue(1.15f);

        beamEffect.CurrentTechnique.Passes["MainPS"].Apply();
        outerStrip.DrawTrail();

        // ----- Inner pass -----
        beamEffect.Parameters["baseColor"].SetValue(innerColor.ToVector3() * 3.0f);
        beamEffect.Parameters["satPower"].SetValue(0.4f);
        beamEffect.Parameters["tex1Mult"].SetValue(0.8f);
        beamEffect.Parameters["tex2Mult"].SetValue(0.9f);
        beamEffect.Parameters["tex3Mult"].SetValue(0.7f);
        beamEffect.Parameters["tex4Mult"].SetValue(1.2f);
        beamEffect.Parameters["totalMult"].SetValue(1.0f);

        beamEffect.CurrentTechnique.Passes["MainPS"].Apply();
        innerStrip.DrawTrail();

        Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        
        Texture2D glow = VFXPlusTextures.PartiGlow.Value;
        float flareScale = 0.35f * projectile.scale;
        Main.EntitySpriteDraw(
            glow,
            endPoint - Main.screenPosition,
            null,
            Color.White with { A = 0 } * 0.9f,
            rot,
            glow.Size() / 2f,
            flareScale,
            SpriteEffects.None,
            0
        );
        
        Main.EntitySpriteDraw(
            glow,
            startPoint - Main.screenPosition,
            null,
            outerColor with { A = 0 } * 0.4f,
            rot,
            glow.Size() / 2f,
            flareScale * 0.7f,
            SpriteEffects.None,
            0
        );
    }
}
*/