using System;
using CalamityVFXPlus.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using VFXPlus.Common.Drawing;
using VFXPlus.Common.Utilities;
using VFXPlus.Content.Dusts;

namespace CalamityVFXPlus.Projectiles.Magic
{
    public sealed class VividBeamVisualProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_1";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 4000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 12;
            Projectile.hide = false;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void Kill(int timeLeft)
        {
            VividBeamVisualStorage.Data.Remove(Projectile.whoAmI);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!VividBeamVisualStorage.Data.TryGetValue(Projectile.whoAmI, out var data))
                return false;

            float fade = Projectile.timeLeft / (float)Math.Max(1, data.MaxLifetime);
            fade = MathHelper.Clamp(fade, 0f, 1f);
            
            float widthFade = fade * fade;
            
            float flareFade = widthFade * 0.3f;


            ModContent.GetInstance<PixelationSystem>().QueueRenderAction(RenderLayer.UnderProjectiles, () =>
            {
                DrawBeam(data.Start, data.End, widthFade, data);
                DrawBeamFlares(data.Start, data.End, flareFade);
            });
            
            return false;
        }

        private static void DrawBeam(Vector2 start, Vector2 end, float widthFade, VividBeamVisualData data)
        {
            Vector2 beam = end - start;
            if (beam.LengthSquared() <= 0.001f)
                return;

            float rotation = beam.ToRotation();
            Vector2[] positions = { start, end };
            float[] rotations = { rotation, rotation };

            Color StripColor(float progress)
            {
                float alpha = MathHelper.Lerp(0.35f, 1f, widthFade);
                return Color.White * alpha;
            }

            float OuterWidth(float progress)
            {
                float taper = MathHelper.Lerp(0.92f, 0.60f, progress);
                return data.BaseOuterWidth * widthFade * taper;
            }

            float CoreWidth(float progress)
            {
                float taper = MathHelper.Lerp(0.80f, 0.42f, progress);
                return data.BaseCoreWidth * widthFade * taper;
            }

            VertexStrip outerStrip = new();
            outerStrip.PrepareStrip(positions, rotations, StripColor, OuterWidth, -Main.screenPosition, includeBacksides: true);

            VertexStrip coreStrip = new();
            coreStrip.PrepareStrip(positions, rotations, StripColor, CoreWidth, -Main.screenPosition, includeBacksides: true);

            Effect laserEffect = ModContent.Request<Effect>(
                "VFXPlus/Effects/Scroll/ComboLaserVertexGradient",
                AssetRequestMode.ImmediateLoad).Value;

            Texture2D onTex = ModContent.Request<Texture2D>(
                "VFXPlus/Assets/Trails/TextureLaser",
                AssetRequestMode.ImmediateLoad).Value;
            //Clear/GlowTrailClear

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

            float distance = beam.Length();
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

            ApplyComboLaserParameters(
                laserEffect,
                onTex,
                gradientTex,
                tex1, tex2, tex3, tex4,
                repVal,
                timeMult: -0.024f,
                satPower: 0.18f,
                totalMult: 1.55f * MathHelper.Lerp(0.65f, 1f, widthFade),
                baseColor: new Vector3(1.1f, 1.1f, 1.1f));

            outerStrip.DrawTrail();

            ApplyComboLaserParameters(
                laserEffect,
                onTex,
                gradientTex,
                tex1, tex2, tex3, tex4,
                repVal,
                timeMult: -0.031f,
                satPower: 0.72f,
                totalMult: 1.05f * MathHelper.Lerp(0.75f, 1f, widthFade),
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

        private static void DrawBeamFlares(Vector2 start, Vector2 end, float fade)
        {
            if (fade <= 0.001f)
                return;

            Texture2D star1 = ModContent.Request<Texture2D>(
                "VFXPlus/Assets/Flare/Simple Lens Flare_11",
                AssetRequestMode.ImmediateLoad).Value;

            Texture2D star2 = ModContent.Request<Texture2D>(
                "VFXPlus/Assets/Flare/flare_16",
                AssetRequestMode.ImmediateLoad).Value;

            Texture2D orbGlow = ModContent.Request<Texture2D>(
                "VFXPlus/Assets/Orbs/circle_05",
                AssetRequestMode.ImmediateLoad).Value;

            Vector2 beam = end - start;
            float rot = beam.ToRotation();
            float t = (float)Main.timeForVisualEffects;

            Vector2 startDraw = start - Main.screenPosition;
            Vector2 endDraw = end - Main.screenPosition;

            float startScale = 0.4f * fade;
            float endScale = 0.5f * fade;

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
                new Color(80, 255, 255, 0) * (0.45f * fade),
                t * 0.03f,
                orbGlow.Size() * 0.5f,
                startScale * 1.5f,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.Draw(
                star1,
                startDraw,
                null,
                new Color(150, 255, 255, 0) * (0.75f * fade),
                rot,
                star1.Size() * 0.5f,
                startScale * 0.85f,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.Draw(
                orbGlow,
                endDraw,
                null,
                Color.White * (0.4f * fade),
                t * 0.08f,
                orbGlow.Size() * 0.5f,
                endScale * 1.35f,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.Draw(
                star1,
                endDraw,
                null,
                Color.White * fade,
                t * 0.02f,
                star1.Size() * 0.5f,
                endScale,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.Draw(
                star2,
                endDraw,
                null,
                new Color(255, 180, 255, 0) * (0.95f * fade),
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
    
    public static class VividBeamColorHelper
    {
        public static Color GetBeamRainbowColor(float progress, float seed = 0f, float timeMult = 0.024f)
        {
            float t = (float)Main.timeForVisualEffects * timeMult + seed + progress;
                
            t %= 1f;
            if (t < 0f)
                t += 1f;

            return Main.hslToRgb(t, 1f, 0.65f);
        }
    }
}