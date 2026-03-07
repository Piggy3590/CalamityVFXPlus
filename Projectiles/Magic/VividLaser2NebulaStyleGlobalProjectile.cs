using System;
using System.Collections.Generic;
using CalamityMod.Projectiles.Magic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using VFXPlus.Common;
using VFXPlus.Common.Drawing;
using VFXPlus.Content.Dusts;
using VFXPlus.Common.Utilities;

namespace CalamityVFXPlus.Projectiles.Magic
{
    public sealed class VividLaser2NebulaStyleGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        {
            return entity.type == ModContent.ProjectileType<VividLaser2>();
        }

        private readonly BaseTrailInfo trail1 = new();
        private readonly BaseTrailInfo trail2 = new();

        private readonly List<float> previousRotations = new();
        private readonly List<Vector2> previousPositions = new();

        private int timer;
        private float overallAlpha;
        private float overallScale;

        public override bool PreAI(Projectile projectile)
        {
            if (projectile.velocity.LengthSquared() <= 0.001f)
                return true;

            Vector2 dir = projectile.velocity.SafeNormalize(Vector2.UnitX);
            float rot = dir.ToRotation();

            ModContent.GetInstance<PixelationSystem>().QueueRenderAction(RenderLayer.Dusts, () =>
            {
                if (timer == 0)
                {
                    SpawnMuzzlePulse(projectile, dir);
                }

                CacheAfterimages(projectile);
                SpawnTravelDust(projectile, dir);
                SetupTrails(projectile, dir, rot);
            });
            float timeForPopInAnim = 22f;
            float animProgress = Math.Clamp((timer + 8f) / timeForPopInAnim, 0f, 1f);
            
            overallScale = MathHelper.Lerp(0f, 1f, MathHelper.SmoothStep(0f, 1f, animProgress));
            overallAlpha = MathHelper.Clamp(MathHelper.Lerp(overallAlpha, 1f, 0.08f), 0f, 1f);

            timer++;
            
            return true;
        }

        private void SpawnMuzzlePulse(Projectile projectile, Vector2 dir)
        {
            Color c1 = GetVividColor(0.08f) * 0.65f;
            Color c2 = GetVividColor(0.16f) * 0.75f;

            Dust d1 = Dust.NewDustPerfect(
                projectile.Center - projectile.velocity * 0.35f,
                ModContent.DustType<CirclePulse>(),
                projectile.velocity * 0.55f,
                0,
                c1,
                0.01f);

            CirclePulseBehavior b1 = new CirclePulseBehavior(0.65f, true, 3, 0.2f, 0.4f);
            b1.drawLayer = "OverPlayers";
            d1.customData = b1;

            Dust d2 = Dust.NewDustPerfect(
                projectile.Center,
                ModContent.DustType<CirclePulse>(),
                projectile.velocity * 0.7f,
                0,
                c2,
                0.01f);

            CirclePulseBehavior b2 = new CirclePulseBehavior(0.65f, true, 2, 0.15f, 0.3f);
            b2.drawLayer = "OverPlayers";
            d2.customData = b2;
            
            d1.rotation = dir.ToRotation() + MathHelper.PiOver2;
            d2.rotation = dir.ToRotation() + MathHelper.PiOver2;
        }

        private void CacheAfterimages(Projectile projectile)
        {
            const int trailCount = 22;

            previousRotations.Add(projectile.rotation);
            previousPositions.Add(projectile.Center);

            if (previousRotations.Count > trailCount)
                previousRotations.RemoveAt(0);

            if (previousPositions.Count > trailCount)
                previousPositions.RemoveAt(0);
        }

        private void SpawnTravelDust(Projectile projectile, Vector2 dir)
        {
            if (timer % 4 == 0 && Main.rand.NextBool(2))
            {
                Dust p = Dust.NewDustPerfect(
                    projectile.Center,
                    ModContent.DustType<GlowStarSharp>(),
                    dir.RotatedBy(Main.rand.NextFloat(-0.25f, 0.25f)) * Main.rand.NextFloat(2f, 4f),
                    0,
                    GetVividColor(0.35f),
                    Main.rand.NextFloat(0.2f, 0.25f) * 1.5f);

                p.velocity += projectile.velocity * -0.5f;
            }

            if (timer > 3 && Main.rand.NextBool())
            {
                int d = Dust.NewDust(
                    projectile.Center - new Vector2(7, 7),
                    7,
                    7,
                    ModContent.DustType<PixelGlowOrb>(),
                    0f,
                    0f,
                    0,
                    GetVividColor(0.55f),
                    Main.rand.NextFloat(0.3f, 0.4f) * 2f);

                Main.dust[d].velocity -= projectile.velocity * 0.25f;
                Main.dust[d].velocity *= 0.45f;
            }
        }

        private void SetupTrails(Projectile projectile, Vector2 dir, float rot)
        {
            Color vivid = GetVividColor(0.2f) * overallAlpha;
            int trueTrailWidth = (int)(22f * overallScale);

            if (trueTrailWidth < 3)
                trueTrailWidth = 0;

            trail1.trailTexture = ModContent.Request<Texture2D>("VFXPlus/Assets/Trails/EvenThinnerGlowLine").Value;
            trail1.trailPointLimit = 90;
            trail1.trailWidth = trueTrailWidth;
            trail1.trailMaxLength = 250;
            trail1.timesToDraw = 2;
            trail1.shouldSmooth = false;
            trail1.pinchHead = true;
            trail1.useEffectMatrix = true;
            trail1.trailColor = vivid;

            float offsetAmount1 = 18f * MathF.Sin(timer / 30f * Math.Max(1, projectile.extraUpdates));
            Vector2 offsetPosition1 = new Vector2(0f, offsetAmount1).RotatedBy(rot);

            trail1.trailRot = rot;
            trail1.trailPos = offsetPosition1 + projectile.Center + projectile.velocity;
            trail1.TrailLogic();

            trail2.trailTexture = trail1.trailTexture;
            trail2.trailPointLimit = 90;
            trail2.trailWidth = trueTrailWidth;
            trail2.trailMaxLength = 250;
            trail2.timesToDraw = 2;
            trail2.shouldSmooth = false;
            trail2.pinchHead = true;
            trail2.useEffectMatrix = true;
            trail2.trailColor = GetVividColor(0.55f) * overallAlpha;

            float offsetAmount2 = 18f * MathF.Sin(timer / 30f * Math.Max(1, projectile.extraUpdates));
            Vector2 offsetPosition2 = new Vector2(0f, -offsetAmount2).RotatedBy(rot);

            trail2.trailRot = rot;
            trail2.trailPos = offsetPosition2 + projectile.Center + projectile.velocity;
            trail2.TrailLogic();
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            trail1.gradientTime = (float)Main.timeForVisualEffects * 0.02f;
            trail1.trailTime = (float)Main.timeForVisualEffects * 0.03f;

            trail2.gradientTime = (float)Main.timeForVisualEffects * 0.02f;
            trail2.trailTime = (float)Main.timeForVisualEffects * 0.03f;

            ModContent.GetInstance<AdditivePixelationSystem>().QueueRenderAction(RenderLayer.UnderProjectiles, () =>
            {
                trail1.TrailDrawing(Main.spriteBatch, doAdditiveReset: true);
                trail2.TrailDrawing(Main.spriteBatch, doAdditiveReset: true);
            });

            Texture2D vanillaTex = TextureAssets.Projectile[projectile.type].Value;
            Vector2 drawPos = projectile.Center - Main.screenPosition;
            float rot = projectile.velocity.ToRotation();
            float drawScale = projectile.scale * Math.Max(overallScale, 0.001f);

            Rectangle sourceRectangle = vanillaTex.Frame(1, Main.projFrames[projectile.type], frameY: projectile.frame);
            Vector2 origin = sourceRectangle.Size() / 2f;

            SpriteEffects se = projectile.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically;

            // center orb
            Texture2D orb = ModContent.Request<Texture2D>("VFXPlus/Assets/Orbs/circle_05").Value;
            float orbScale = 0.45f * drawScale;
            float orbAlpha = overallAlpha * 0.3f;

            ModContent.GetInstance<PixelationSystem>().QueueRenderAction(RenderLayer.Dusts, () =>
            {
                Main.EntitySpriteDraw(orb, drawPos, null, Color.White * orbAlpha, rot, orb.Size() * 0.5f,
                    orbScale * new Vector2(1f, 0.8f), se);
                Main.EntitySpriteDraw(orb, drawPos, null, GetVividColor(0.15f) * (orbAlpha * 0.1f), rot,
                    orb.Size() * 0.5f, orbScale * 1.6f * new Vector2(1f, 0.8f), se);
                Main.EntitySpriteDraw(orb, drawPos, null, GetVividColor(0.55f) * (orbAlpha * 0.05f), rot,
                    orb.Size() * 0.5f, orbScale * 2.3f * new Vector2(1f, 0.8f), se);
                
                // afterimage
                for (int i = 0; i < previousPositions.Count; i++)
                {
                    float progress = (float)i / previousPositions.Count;
                    float size = progress * drawScale;
                    Color col = GetVividColor(progress) * (progress * overallAlpha * 0.5f);

                    Main.EntitySpriteDraw(
                        vanillaTex,
                        previousPositions[i] - Main.screenPosition,
                        sourceRectangle,
                        col,
                        previousRotations[i],
                        origin,
                        size,
                        se);
                }
                
                // bloom test
                    Main.EntitySpriteDraw(
                        vanillaTex,
                        drawPos + Main.rand.NextVector2Circular(2.5f, 2.5f),
                        sourceRectangle,
                        GetVividColor(0.25f) * (0.7f * overallAlpha),
                        projectile.rotation,
                        origin,
                        1.15f * drawScale,
                        se);

                // body
                Main.EntitySpriteDraw(vanillaTex, drawPos, sourceRectangle, Color.White * overallAlpha,
                    projectile.rotation, origin, drawScale, se);
                Main.EntitySpriteDraw(vanillaTex, drawPos, sourceRectangle,
                    GetVividColor(0.1f) * (0.35f * overallAlpha), projectile.rotation, origin, drawScale, se);
            });
            return false;
        }

        public override bool PreKill(Projectile projectile, int timeLeft)
        {
            foreach (Vector2 pos in trail1.trailPositions)
            {
                if (Main.rand.NextBool(2))
                {
                    int d = Dust.NewDust(pos, 1, 1, ModContent.DustType<GlowPixelAlts>(), 0f, 0f, 0, GetVividColor(0.25f), 0.25f + Main.rand.NextFloat(-0.1f, 0.1f));
                    Main.dust[d].velocity *= 0.2f;
                }
            }

            foreach (Vector2 pos in trail2.trailPositions)
            {
                if (Main.rand.NextBool(2))
                {
                    int d = Dust.NewDust(pos, 1, 1, ModContent.DustType<GlowPixelAlts>(), 0f, 0f, 0, GetVividColor(0.65f), 0.25f + Main.rand.NextFloat(-0.1f, 0.1f));
                    Main.dust[d].velocity *= 0.2f;
                }
            }

            for (int i = 0; i < 14; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(4f, 4f);
                Dust d = Dust.NewDustPerfect(
                    projectile.Center,
                    ModContent.DustType<RoaParticle>(),
                    vel,
                    0,
                    GetVividColor(Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.8f, 1.25f));

                d.noGravity = true;
            }

            return true;
        }

        private static Color GetVividColor(float progress)
        {
            float t = ((float)Main.timeForVisualEffects * 0.02f + progress) % 1f;
            return Main.hslToRgb(t, 1f, 0.62f);
        }
    }
}