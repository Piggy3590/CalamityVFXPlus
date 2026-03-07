using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.Projectiles.Magic;
using VFXPlus.Common.Drawing;

namespace CalamityVFXPlus.Projectiles.Magic
{
    public class PhantasmalFuryVFXGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        private const int TrailCount = 30;

        private int timer;
        private float starPower;
        private float overallAlpha;
        private float randomSineOffset;

        private readonly List<Vector2> previousPositions = new();
        private readonly List<float> previousRotations = new();

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        {
            return entity.type == ModContent.ProjectileType<PhantasmalFuryProj>();
        }

        public override void OnSpawn(Projectile projectile, Terraria.DataStructures.IEntitySource source)
        {
            randomSineOffset = Main.rand.NextFloat(0f, 100f);
        }

        public override void PostAI(Projectile projectile)
        {
            previousPositions.Add(projectile.Center);
            previousRotations.Add(projectile.velocity.ToRotation());

            if (previousPositions.Count > TrailCount)
                previousPositions.RemoveAt(0);

            if (previousRotations.Count > TrailCount)
                previousRotations.RemoveAt(0);

            if (timer % 2 == 0 && timer > 12)
            {
                Color col = Main.rand.NextBool() ? Color.White : Color.SkyBlue;

                Dust d = Dust.NewDustPerfect(
                    projectile.Center,
                    DustID.SpectreStaff,
                    projectile.velocity * Main.rand.NextFloat(-0.35f, -0.15f),
                    100,
                    col,
                    Main.rand.NextFloat(0.9f, 1.2f)
                );

                d.noGravity = true;
                d.fadeIn = 0.9f;
            }

            starPower = MathHelper.Clamp(MathHelper.Lerp(starPower, 1.25f, 0.02f), 0f, 1f);
            overallAlpha = MathHelper.Clamp(MathHelper.Lerp(overallAlpha, 1.25f, 0.03f), 0f, 1f);

            Lighting.AddLight(projectile.Center, Color.LightSkyBlue.ToVector3() * 0.6f);

            timer++;
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            
            ModContent.GetInstance<PixelationSystem>().QueueRenderAction(RenderLayer.OverPlayers, () =>
            {
                DrawTrail(projectile);
                DrawOverlay(projectile);
            });

            return false;
        }

        public override void OnKill(Projectile projectile, int timeLeft)
        {
            for (int i = 0; i < previousPositions.Count; i++)
            {
                if (i % 3 != 0)
                    continue;

                Vector2 pos = previousPositions[i];
                Vector2 vel = previousRotations[i].ToRotationVector2();

                Color col = Main.rand.NextBool() ? Color.White : Color.SkyBlue;

                Dust d = Dust.NewDustPerfect(
                    pos,
                    DustID.SpectreStaff,
                    (vel * Main.rand.NextFloat(-2f, -0.5f)).RotateRandom(0.3f) + Main.rand.NextVector2Circular(2f, 2f),
                    100,
                    col,
                    Main.rand.NextFloat(0.9f, 1.25f)
                );

                d.noGravity = true;
            }

            for (int i = 0; i < 6 + Main.rand.Next(2); i++)
            {
                Vector2 dustVel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1f, 3.5f);
                Color col = Main.rand.NextBool() ? Color.White : Color.SkyBlue;

                Dust d = Dust.NewDustPerfect(
                    projectile.Center,
                    DustID.SpectreStaff,
                    dustVel,
                    100,
                    col,
                    Main.rand.NextFloat(1f, 1.4f)
                );

                d.noGravity = true;
            }
        }

        private void DrawTrail(Projectile projectile)
        {
            Texture2D orb = VFXPlusTextures.PartiGlow.Value;
            Texture2D spike = VFXPlusTextures.SoulSpike.Value;

            Vector2 drawPos = projectile.Center - Main.screenPosition;

            Vector2 orbScale = new Vector2(0.85f * EaseOutSine(overallAlpha), 0.55f) * overallAlpha * 1.1f;
            Main.EntitySpriteDraw(
                orb,
                drawPos,
                null,
                Transparent(Color.LightSkyBlue),
                projectile.velocity.ToRotation(),
                orb.Size() / 2f,
                orbScale,
                SpriteEffects.None
            );

            Main.EntitySpriteDraw(
                orb,
                drawPos,
                null,
                Transparent(Color.White),
                projectile.velocity.ToRotation(),
                orb.Size() / 2f,
                orbScale * 0.5f,
                SpriteEffects.None
            );

            float sinWidth = 1f + (float)Math.Sin(randomSineOffset + Main.timeForVisualEffects * 0.25f) * 0.15f;

            for (int i = 0; i < previousPositions.Count; i++)
            {
                float scale = (float)i / previousPositions.Count;
                Vector2 trailScale = new Vector2(
                    scale * 0.5f * EaseOutSine(overallAlpha),
                    EaseOutQuad(scale) * 0.45f * sinWidth
                ) * projectile.scale;

                Vector2 trailDrawPos = previousPositions[i] - Main.screenPosition;
                Color betweenBlue = Color.Lerp(Color.SkyBlue, Color.LightSkyBlue, 0.75f);
                Color col = Color.Lerp(Color.DeepSkyBlue, betweenBlue, scale) * scale * overallAlpha;
                col.A = 0;

                Main.EntitySpriteDraw(
                    spike,
                    trailDrawPos,
                    null,
                    col * 0.75f,
                    previousRotations[i],
                    spike.Size() / 2f,
                    trailScale,
                    SpriteEffects.None
                );
            }
        }

        private void DrawOverlay(Projectile projectile)
        {
            Texture2D ghost = VFXPlusTextures.DungeonSpirit.Value;
            Texture2D ghostBorder = VFXPlusTextures.DungeonSpiritBorder.Value;
            Texture2D star = VFXPlusTextures.RainbowRod.Value;

            Vector2 drawPos = projectile.Center - Main.screenPosition;

            if (starPower < 1f)
            {
                float dir = projectile.velocity.X > 0f ? 1f : -1f;
                float starRotation = MathHelper.Lerp(0f, MathHelper.TwoPi * dir, EaseInOutQuad(starPower)) + projectile.rotation;
                float starScale = EaseOutQuint(1f - starPower) * projectile.scale * 1.35f;
                Vector2 starScaleVec = new Vector2(1f, 0.7f) * starScale * overallAlpha;

                Color starColor = Transparent(Color.SkyBlue) * starPower;

                Main.EntitySpriteDraw(star, drawPos, null, starColor, starRotation, star.Size() / 2f, starScaleVec, SpriteEffects.None);
                Main.EntitySpriteDraw(star, drawPos, null, starColor, starRotation, star.Size() / 2f, starScaleVec * 0.65f, SpriteEffects.None);
                Main.EntitySpriteDraw(star, drawPos, null, starColor, starRotation + MathHelper.PiOver2, star.Size() / 2f, starScaleVec, SpriteEffects.None);
                Main.EntitySpriteDraw(star, drawPos, null, starColor, starRotation + MathHelper.PiOver2, star.Size() / 2f, starScaleVec * 0.65f, SpriteEffects.None);
            }

            int frameCount = 3;
            int currentFrame = projectile.frameCounter % frameCount;
            int frameHeight = ghost.Height / frameCount;
            Rectangle frame = new Rectangle(0, frameHeight * currentFrame, ghost.Width, frameHeight);

            Vector2 origin = frame.Size() / 2f;
            float sinScale = 1f + (float)Math.Sin((randomSineOffset + 20f) + Main.timeForVisualEffects * 0.15f) * 0.07f;
            Vector2 ghostScale = new Vector2(1f, EaseOutSine(overallAlpha)) * sinScale;

            Main.EntitySpriteDraw(
                ghostBorder,
                drawPos,
                frame,
                Transparent(Color.White) * (overallAlpha * 0.5f),
                projectile.velocity.ToRotation(),
                origin,
                projectile.scale * 0.72f * 1.1f * ghostScale,
                SpriteEffects.None
            );

            Main.EntitySpriteDraw(
                ghost,
                drawPos,
                frame,
                Color.White * overallAlpha,
                projectile.velocity.ToRotation(),
                origin,
                projectile.scale * 0.72f * 1.1f * ghostScale,
                SpriteEffects.None
            );
        }

        private static Color Transparent(Color color)
        {
            color.A = 0;
            return color;
        }

        private static float EaseOutSine(float x) =>
            (float)Math.Sin((x * MathHelper.Pi) / 2f);

        private static float EaseOutQuad(float x) =>
            1f - (1f - x) * (1f - x);

        private static float EaseInOutQuad(float x) =>
            x < 0.5f ? 2f * x * x : 1f - (float)Math.Pow(-2f * x + 2f, 2f) / 2f;

        private static float EaseOutQuint(float x) =>
            1f - (float)Math.Pow(1f - x, 5f);
    }
}