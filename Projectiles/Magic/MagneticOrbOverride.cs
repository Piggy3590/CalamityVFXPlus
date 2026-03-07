using System;
using CalamityMod.Projectiles.Magic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using VFXPlus.Common;
using VFXPlus.Common.Drawing;
using VFXPlus.Common.Utilities;

namespace CalamityVFXPlus.Projectiles.Magic
{
    public sealed class MagneticOrbOverride : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        private float overallScale = 1f;
        private float overallAlpha = 0f;
        private int timer = 0;

        private float orbRot = 0f;
        private float rotSpeed = 0f;
        private float pulseOffset = 0f;
        private float scaleJitter = 1f;
        private bool initialized = false;
        
        private float ringRotOffset = 0f;
        private float ringRotSpeed = 0f;

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        {
            return lateInstantiation && entity.type == ModContent.ProjectileType<MagneticOrb>();
        }

        public override bool PreAI(Projectile projectile)
        {
            if (!initialized)
            {
                initialized = true;

                orbRot = Main.rand.NextFloat(MathHelper.TwoPi);
                rotSpeed = Main.rand.NextFloat(0.010f, 0.028f);
                if (Main.rand.NextBool())
                    rotSpeed *= -1f;

                pulseOffset = Main.rand.NextFloat(MathHelper.TwoPi);
                scaleJitter = Main.rand.NextFloat(0.92f, 1.08f);
                
                ringRotOffset = Main.rand.NextFloat(MathHelper.TwoPi);
                ringRotSpeed = Main.rand.NextFloat(0.6f, 1.4f);
                if (Main.rand.NextBool())
                    ringRotSpeed *= -1f;
            }

            if (projectile.timeLeft > 30)
            {
                float timeForPopInAnim = 30f;
                float animProgress = Math.Clamp((timer + 10f) / timeForPopInAnim, 0f, 1f);
                overallScale = MathHelper.Lerp(0f, 1f, Easings.easeInOutBack(animProgress, 0f, 1.5f));
                overallAlpha = Math.Clamp(MathHelper.Lerp(overallAlpha, 1.5f, 0.08f), 0f, 1f);
            }
            else
            {
                float timeForPopOutAnim = 30f;
                float animProgress = Math.Clamp((projectile.timeLeft - 5f) / timeForPopOutAnim, 0f, 1f);
                overallScale = MathHelper.Lerp(0f, 1f, Easings.easeInOutBack(animProgress, 0f, 0f));
                overallAlpha = Math.Clamp(MathHelper.Lerp(overallAlpha, -0.5f, 0.06f), 0f, 1f);
            }

            timer++;
            return true;
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            Vector2 drawPos = projectile.Center - Main.screenPosition;

            orbRot += rotSpeed;

            float rot = orbRot;
            
            float drawScale = projectile.scale * overallScale * 0.45f * scaleJitter;
            
            float ballScale = 1f - (MathF.Sin((float)Main.timeForVisualEffects * 0.14f + pulseOffset) * 0.06f);

            Texture2D ball = VFXPlusTextures.feather_circle128PMA.Value;
            Texture2D lightning = VFXPlusTextures.bigCircle2.Value;

            Effect radialScroll = ModContent.Request<Effect>("VFXPlus/Effects/Radial/NewRadialScroll", AssetRequestMode.ImmediateLoad).Value;
            
            ModContent.GetInstance<PixelationSystem>().QueueRenderAction(RenderLayer.Dusts, () =>
            {
                Main.spriteBatch.Draw(
                    ball,
                    drawPos,
                    null,
                    Color.Black * 0.5f * overallAlpha,
                    0f,
                    ball.Size() / 2f,
                    1.6f * drawScale,
                    SpriteEffects.None,
                    0f
                );

                Color col = new Color(210, 0, 198) with { A = 0 } * overallAlpha;
                Color col2 = new Color(240, 0, 200) with { A = 0 } * overallAlpha;

                Main.spriteBatch.Draw(
                    ball,
                    drawPos,
                    null,
                    Color.White with { A = 0 } * overallAlpha,
                    0f,
                    ball.Size() / 2f,
                    0.90f * drawScale,
                    SpriteEffects.None,
                    0f
                );

                Main.spriteBatch.Draw(
                    ball,
                    drawPos,
                    null,
                    col * 0.7f,
                    rot,
                    ball.Size() / 2f,
                    1.2f * drawScale,
                    SpriteEffects.None,
                    0f
                );

                Main.spriteBatch.Draw(
                    ball,
                    drawPos,
                    null,
                    col2 * 0.15f,
                    rot,
                    ball.Size() / 2f,
                    2.4f * drawScale,
                    SpriteEffects.None,
                    0f
                );
            });
            
            ModContent.GetInstance<AdditivePixelationSystem>().QueueRenderAction(RenderLayer.UnderProjectiles, () =>
            {
                radialScroll.Parameters["causticTexture"].SetValue(
                    VFXPlusTextures.WaterEnergyNoise.Value
                );
                radialScroll.Parameters["gradientTexture"].SetValue(
                        ModContent.Request<Texture2D>("CalamityVFXPlus/Assets/Gradient/MagneticOrbGrad").Value
                );
                radialScroll.Parameters["distortTexture"].SetValue(
                    VFXPlusTextures.Swirl.Value
                );

                radialScroll.Parameters["flowSpeed"].SetValue(1f);
                radialScroll.Parameters["distortStrength"].SetValue(0.1f);
                radialScroll.Parameters["vignetteSize"].SetValue(0.2f);
                radialScroll.Parameters["vignetteBlend"].SetValue(0.8f);
                radialScroll.Parameters["colorIntensity"].SetValue(1.75f * Easings.easeInCirc(overallAlpha));
                radialScroll.Parameters["uTime"].SetValue(timer * 0.015f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.Additive,
                    Main.DefaultSamplerState,
                    DepthStencilState.None,
                    RasterizerState.CullCounterClockwise,
                    radialScroll,
                    Main.GameViewMatrix.EffectMatrix
                );
                float ringRot = rot * ringRotSpeed + ringRotOffset;

                Main.spriteBatch.Draw(
                    lightning,
                    drawPos,
                    null,
                    new Color(255, 255, 255, 0),
                    ringRot,
                    lightning.Size() / 2f,
                    0.4f * drawScale * (ballScale + 0.4f),
                    SpriteEffects.None,
                    0f
                );

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    Main.DefaultSamplerState,
                    DepthStencilState.None,
                    RasterizerState.CullCounterClockwise,
                    null,
                    Main.GameViewMatrix.TransformationMatrix
                );

                Main.graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            });
            
            return false;
        }
    }
}