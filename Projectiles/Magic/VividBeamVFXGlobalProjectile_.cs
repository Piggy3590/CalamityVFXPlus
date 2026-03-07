using System;
using CalamityMod.Items.Weapons.Magic;
using CalamityMod.Projectiles.Magic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
/*
namespace CalamityVFXPlus.Projectiles.Magic
{
    public sealed class VividBeamVFXGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        private bool initialized;
        private float seed;

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        {
            return lateInstantiation && entity.type == ModContent.ProjectileType<VividBeam>();
        }

        public override bool PreAI(Projectile projectile)
        {
            if (!initialized)
            {
                initialized = true;
                seed = Main.rand.NextFloat(MathHelper.TwoPi);
                SoundEngine.PlaySound(VividClarity.BeamSound, projectile.Center);
            }

            if (projectile.velocity.LengthSquared() > 0.001f)
                projectile.rotation = projectile.velocity.ToRotation();
            return false;
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            if (projectile.velocity.LengthSquared() <= 0.001f)
                return false;

            Player owner = Main.player[projectile.owner];
            if (!owner.active)
                return false;

            Vector2 unit = projectile.velocity.SafeNormalize(Vector2.UnitX);

            Vector2 muzzle = owner.MountedCenter;

            muzzle += unit * 24f;

            Vector2 end = projectile.Center;

            float dist = Vector2.Distance(muzzle, end);
            if (dist < 8f)
                return false;

            float time = (float)Main.timeForVisualEffects;
            float pulse = 1f + 0.08f * MathF.Sin(time * 0.22f + seed);

            DrawCombinedBeam(muzzle, end, projectile, pulse);
            DrawBeamFlares(muzzle, end, projectile, pulse);

            return false;
        }

        private static void DrawCombinedBeam(Vector2 start, Vector2 end, Projectile projectile, float pulse)
        {
            float rotation = (end - start).ToRotation();
            Vector2[] positions = { start, end };
            float[] rotations = { rotation, rotation };

            Color StripColor(float progress) => Color.White;

            float OuterWidth(float progress)
            {
                float taper = MathHelper.Lerp(0.92f, 0.60f, progress);
                return 62f * pulse * taper;
            }

            float CoreWidth(float progress)
            {
                float taper = MathHelper.Lerp(0.80f, 0.42f, progress);
                return 28f * pulse * taper;
            }

            VertexStrip outerStrip = new VertexStrip();
            outerStrip.PrepareStrip(positions, rotations, StripColor, OuterWidth, -Main.screenPosition, includeBacksides: true);

            VertexStrip coreStrip = new VertexStrip();
            coreStrip.PrepareStrip(positions, rotations, StripColor, CoreWidth, -Main.screenPosition, includeBacksides: true);

            Effect laserEffect = ModContent.Request<Effect>(
                "VFXPlus/Effects/Scroll/ComboLaserVertexGradient",
                AssetRequestMode.ImmediateLoad).Value;

            Texture2D onTex = ModContent.Request<Texture2D>(
                "VFXPlus/Assets/Trails/Clear/GlowTrailClear",
                AssetRequestMode.ImmediateLoad).Value;

            Texture2D gradientTex = ModContent.Request<Texture2D>(
                "VFXPlus/Assets/Gradients/RainbowGrad1",
                AssetRequestMode.ImmediateLoad).Value;

            Texture2D tex1 = ModContent.Request<Texture2D>(
                "VFXPlus/Assets/Trails/ThinGlowLine",
                AssetRequestMode.ImmediateLoad).Value;

            Texture2D tex2 = ModContent.Request<Texture2D>(
                "VFXPlus/Assets/Trails/spark_06",
                AssetRequestMode.ImmediateLoad).Value;

            Texture2D tex3 = ModContent.Request<Texture2D>(
                "VFXPlus/Assets/Trails/Extra_196_Black",
                AssetRequestMode.ImmediateLoad).Value;

            Texture2D tex4 = ModContent.Request<Texture2D>(
                "VFXPlus/Assets/Trails/Trail5Loop",
                AssetRequestMode.ImmediateLoad).Value;

            float distance = Vector2.Distance(start, end);
            float repVal = distance / 2000f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.EffectMatrix);

            // OUTER BLOOM
            ApplyComboLaserParameters(
                laserEffect,
                onTex,
                gradientTex,
                tex1, tex2, tex3, tex4,
                repVal,
                timeMult: -0.024f,
                satPower: 0.18f,
                totalMult: 1.55f,
                baseColor: new Vector3(1.1f, 1.1f, 1.1f));

            outerStrip.DrawTrail();

            // CORE
            ApplyComboLaserParameters(
                laserEffect,
                onTex,
                gradientTex,
                tex1, tex2, tex3, tex4,
                repVal,
                timeMult: -0.031f,
                satPower: 0.72f,
                totalMult: 1.05f,
                baseColor: new Vector3(1f, 1f, 1f));

            coreStrip.DrawTrail();

            Main.pixelShader.CurrentTechnique.Passes[0].Apply();

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.EffectMatrix);
        }

        private static void ApplyComboLaserParameters(
            Effect effect,
            Texture2D onTex,
            Texture2D gradientTex,
            Texture2D tex1,
            Texture2D tex2,
            Texture2D tex3,
            Texture2D tex4,
            float repVal,
            float timeMult,
            float satPower,
            float totalMult,
            Vector3 baseColor)
        {
            effect.Parameters["WorldViewProjection"]?.SetValue(Main.GameViewMatrix.NormalizedTransformationmatrix);

            effect.Parameters["onTex"]?.SetValue(onTex);
            effect.Parameters["gradientTex"]?.SetValue(gradientTex);
            effect.Parameters["baseColor"]?.SetValue(baseColor);
            effect.Parameters["satPower"]?.SetValue(satPower);

            effect.Parameters["sampleTexture1"]?.SetValue(tex1);
            effect.Parameters["sampleTexture2"]?.SetValue(tex2);
            effect.Parameters["sampleTexture3"]?.SetValue(tex3);
            effect.Parameters["sampleTexture4"]?.SetValue(tex4);

            effect.Parameters["grad1Speed"]?.SetValue(0.66f);
            effect.Parameters["grad2Speed"]?.SetValue(0.66f);
            effect.Parameters["grad3Speed"]?.SetValue(1.03f);
            effect.Parameters["grad4Speed"]?.SetValue(0.77f);

            effect.Parameters["tex1Mult"]?.SetValue(1.25f);
            effect.Parameters["tex2Mult"]?.SetValue(1.5f);
            effect.Parameters["tex3Mult"]?.SetValue(1.15f);
            effect.Parameters["tex4Mult"]?.SetValue(2.5f);
            effect.Parameters["totalMult"]?.SetValue(totalMult);

            effect.Parameters["gradientReps"]?.SetValue(0.75f * repVal);
            effect.Parameters["tex1reps"]?.SetValue(1.15f * repVal);
            effect.Parameters["tex2reps"]?.SetValue(1.15f * repVal);
            effect.Parameters["tex3reps"]?.SetValue(1.15f * repVal);
            effect.Parameters["tex4reps"]?.SetValue(1.15f * repVal);

            effect.Parameters["uTime"]?.SetValue((float)Main.timeForVisualEffects * timeMult);

            effect.CurrentTechnique.Passes["MainPS"].Apply();
        }

        private static void DrawBeamFlares(Vector2 start, Vector2 end, Projectile projectile, float pulse)
        {
            Texture2D star1 = ModContent.Request<Texture2D>(
                "VFXPlus/Assets/Flare/Simple Lens Flare_11",
                AssetRequestMode.ImmediateLoad).Value;

            Texture2D star2 = ModContent.Request<Texture2D>(
                "VFXPlus/Assets/Flare/flare_16",
                AssetRequestMode.ImmediateLoad).Value;

            Texture2D orbGlow = ModContent.Request<Texture2D>(
                "VFXPlus/Assets/Orbs/circle_05",
                AssetRequestMode.ImmediateLoad).Value;

            Vector2 startDraw = start - Main.screenPosition;
            Vector2 endDraw = end - Main.screenPosition;

            float rot = projectile.velocity.ToRotation();
            float t = (float)Main.timeForVisualEffects;

            float startScale = 0.30f * pulse;
            float endScale = 0.42f * pulse;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.EffectMatrix);

            Main.spriteBatch.Draw(
                orbGlow,
                startDraw,
                null,
                new Color(80, 255, 255, 0) * 0.55f,
                t * 0.03f,
                orbGlow.Size() * 0.5f,
                startScale * 1.6f,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.Draw(
                star1,
                startDraw,
                null,
                new Color(150, 255, 255, 0) * 0.9f,
                rot,
                star1.Size() * 0.5f,
                startScale * 0.95f,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.Draw(
                star2,
                startDraw,
                null,
                Color.White,
                -rot * 0.65f,
                star2.Size() * 0.5f,
                startScale * 0.78f,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.Draw(
                orbGlow,
                endDraw,
                null,
                Color.White * 0.35f,
                t * 0.08f,
                orbGlow.Size() * 0.5f,
                endScale * 1.35f,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.Draw(
                star1,
                endDraw,
                null,
                Color.White,
                t * 0.02f,
                star1.Size() * 0.5f,
                endScale * 1.00f,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.Draw(
                star2,
                endDraw,
                null,
                new Color(255, 180, 255, 0) * 0.95f,
                t * 0.055f,
                star2.Size() * 0.5f,
                endScale * 0.82f,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.EffectMatrix);
        }
    }
}
*/