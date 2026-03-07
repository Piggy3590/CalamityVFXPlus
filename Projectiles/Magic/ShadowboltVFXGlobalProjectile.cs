using System;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

using CalamityMod.Projectiles.Magic;
using VFXPlus.Common;
using VFXPlus.Common.Drawing;

/*
namespace CalamityVFXPlus.Projectiles.Magic
{
    public sealed class ShadowboltVFXGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        private static Effect _tendrilEffect;
        private static Asset<Texture2D> _trailBloom;
        private static Asset<Texture2D> _trailThin;
        private static Asset<Texture2D> _portalFlare;
        private static Asset<Texture2D> _starFlare;
        private static Asset<Texture2D> _platformTex;
        private static Asset<Texture2D> _bloomCircleTex;
        private static Asset<Texture2D> _glowSparkTex;

        private Vector2 _oldTargetCenter;
        private float _aimMotion;
        private float _beamOpacity;
        private float _beamWidthFade;

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            => entity.ModProjectile is Shadowbolt;

        public override bool PreAI(Projectile projectile)
        {
            if (projectile.ModProjectile is not Shadowbolt sb)
                return true;

            Player owner = Main.player[projectile.owner];
            if (!owner.active || owner.dead)
            {
                projectile.Kill();
                return false;
            }

            RunReplacementAI(projectile, sb, owner);
            return false;
        }

        private void RunReplacementAI(Projectile projectile, Shadowbolt sb, Player owner)
        {
            float targetDist = Vector2.Distance(owner.Center, projectile.Center);

            if (!sb.spawnPlat && !sb.hasReboundOffPlat && sb.targetCenter != Vector2.Zero)
                sb.platFade = MathHelper.Lerp(sb.platFade, 1f, 0.008f);

            if (sb.hasReboundOffPlat)
                sb.platFade -= 0.0007f;

            sb.platFade = MathHelper.Clamp(sb.platFade, 0f, 1f);

            if (sb.reflecting)
            {
                Vector2 targetDir = (sb.targetCenter - sb.platPosCenter).SafeNormalize(Vector2.UnitX);

                if (sb.reflectionTimer > 15)
                    sb.platPosCenter += targetDir * -2.7f * Utils.GetLerpValue(15f, 40f, sb.reflectionTimer, true);
                else
                    sb.platPosCenter += targetDir * 9f * Utils.GetLerpValue(10f, 0f, sb.reflectionTimer, true);

                projectile.extraUpdates = 0;
                projectile.velocity = Vector2.Zero;
                projectile.Center = sb.platPosWall;
                sb.reflectionTimer--;

                if (sb.reflectionTimer <= 0)
                {
                    SoundStyle bounce = new("CalamityMod/Sounds/Item/ShadowboltReflect");
                    SoundEngine.PlaySound(
                        bounce with { Volume = 0.6f, Pitch = Main.rand.NextFloat(-0.1f, 0.1f), MaxInstances = -1 },
                        sb.platPosWall
                    );

                    for (int i = 0; i < Main.maxNPCs; i++)
                        projectile.localNPCImmunity[i] = 0;

                    projectile.velocity = targetDir * 12f;
                    sb.reflecting = false;
                    sb.hasReboundOffPlat = true;
                    sb.time = 10;
                    projectile.extraUpdates = 100;
                }
            }

            if (sb.spawnPlat && sb.hasSetPlatSpawn)
            {
                sb.chosenTarget = projectile.Center.ClosestNPCAt(2000);

                if (sb.chosenTarget == null)
                    sb.targetCenter = owner.Calamity().mouseWorld;
                else
                    sb.targetCenter = sb.chosenTarget.Center;

                sb.platPosCenter = projectile.Center + projectile.velocity * Main.rand.Next(70, 121);
                sb.platRot = (projectile.Center - sb.platPosCenter).SafeNormalize(Vector2.UnitX).ToRotation();
                sb.platPosWall = sb.platPosCenter + new Vector2(0f, 8f).RotatedBy(sb.platRot);
                sb.spawnPlat = false;
            }

            if (!sb.spawnPlat && sb.reflecting && sb.targetCenter != Vector2.Zero)
            {
                if (sb.chosenTarget == null)
                    sb.targetCenter = owner.Calamity().mouseWorld;
                else
                    sb.targetCenter = sb.chosenTarget.Center;

                sb.platRot = sb.platRot.AngleLerp(
                    (sb.targetCenter - sb.platPosCenter).SafeNormalize(Vector2.UnitX).ToRotation(),
                    0.1f
                );

                sb.platPosWall = sb.platPosCenter + new Vector2(0f, -30f).RotatedBy(sb.platRot + MathHelper.PiOver2);
            }

            if (!sb.spawnPlat && !sb.hasReboundOffPlat && sb.targetCenter != Vector2.Zero)
            {
                float beamDist = Vector2.Distance(sb.platPosWall, projectile.Center);
                if (beamDist <= 25f && !sb.reflecting)
                {
                    SoundStyle wall = new("CalamityMod/Sounds/Item/ShadowboltWallHit");
                    SoundEngine.PlaySound(
                        wall with { Volume = 0.5f, Pitch = 0f, MaxInstances = -1 },
                        sb.platPosWall
                    );

                    sb.reflecting = true;
                    projectile.extraUpdates = 0;
                }
            }

            if (!sb.hasReboundOffPlat && projectile.numHits == 0 && !sb.hasSetPlatSpawn && sb.time > 70)
            {
                sb.hasSetPlatSpawn = true;
                sb.spawnPlat = true;
            }

            if (!sb.reflecting && projectile.velocity.LengthSquared() > 0.001f)
                projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (targetDist > 2600f && !sb.reflecting)
                projectile.Kill();
                
            if (sb.time == 14 && projectile.velocity.LengthSquared() > 0.001f)
            {
                projectile.velocity = projectile.velocity.RotatedByRandom(0.15f);
                _beamOpacity = Math.Max(_beamOpacity, 0.45f);
                _beamWidthFade = Math.Max(_beamWidthFade, 0.55f);
            }
            
            float targetMotion = 0f;
            if (_oldTargetCenter != Vector2.Zero && sb.targetCenter != Vector2.Zero)
                targetMotion = Vector2.Distance(_oldTargetCenter, sb.targetCenter);

            _aimMotion = MathHelper.Lerp(_aimMotion, Utils.Clamp(targetMotion * 0.09f, 0f, 1f), 0.18f);
            _oldTargetCenter = sb.targetCenter;
            
            float appear = Utils.GetLerpValue(10f, 20f, sb.time, true);
            float disappear = Utils.GetLerpValue(0f, 12f, projectile.timeLeft, true);
            
            float phaseMult = sb.reflecting ? 0.75f : 0.58f;
            if (sb.hasReboundOffPlat)
                phaseMult = 0.66f;

            float targetOpacity = appear * disappear * phaseMult;
            
            _beamOpacity = MathHelper.Lerp(_beamOpacity, targetOpacity, 0.25f);
            _beamWidthFade = MathHelper.Lerp(_beamWidthFade, targetOpacity, 0.18f);

            projectile.scale = 1f + (float)Math.Cos(Main.timeForVisualEffects * 0.18f) * 0.01f;

            sb.time++;
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            if (projectile.ModProjectile is not Shadowbolt sb)
                return true;

            LoadAssets();

            ModContent.GetInstance<PixelationSystem>().QueueRenderAction(RenderLayer.OverPlayers, () =>
            {
                DrawEverythingPixelated(projectile, sb);
            });

            return false;
        }

        private void DrawEverythingPixelated(Projectile projectile, Shadowbolt sb)
        {
            Vector2 beamStart = projectile.Center;
            Vector2 beamEnd;

            if (sb.reflecting)
                beamEnd = sb.platPosWall;
            else if (!sb.hasReboundOffPlat && sb.platPosWall != Vector2.Zero)
                beamEnd = sb.platPosWall;
            else if (sb.targetCenter != Vector2.Zero)
                beamEnd = sb.targetCenter;
            else
                beamEnd = projectile.Center + projectile.velocity.SafeNormalize(Vector2.UnitX) * 1200f;

            if (_beamOpacity > 0.015f)
                DrawPrettyBeam(projectile, sb, beamStart, beamEnd, _beamOpacity, _beamWidthFade);
                
            if (_beamOpacity > 0.08f)
                DrawBeamStartFlare(beamStart, projectile.velocity, sb.hasReboundOffPlat, _beamOpacity * 0.45f);


            if (_beamOpacity > 0.10f)
                DrawShadowboltDispersion(projectile, sb, beamStart, beamEnd, _beamOpacity * 0.22f);

            if (sb.targetCenter != Vector2.Zero && sb.platFade > 0f)
                DrawPlatform(sb);

            if (sb.reflecting)
                DrawRechargeFlare(sb);
        }

        private void BuildBeamPoints(
            Projectile projectile,
            Shadowbolt sb,
            Vector2 start,
            Vector2 end,
            out Vector2[] positions,
            out float[] rotations)
        {
            const int pointCount = 18;

            positions = new Vector2[pointCount];
            rotations = new float[pointCount];

            Vector2 beam = end - start;
            float beamLength = beam.Length();

            if (beamLength <= 4f)
            {
                for (int i = 0; i < pointCount; i++)
                {
                    positions[i] = start;
                    rotations[i] = 0f;
                }
                return;
            }

            Vector2 dir = beam / beamLength;
            Vector2 normal = dir.RotatedBy(MathHelper.PiOver2);
            float time = (float)Main.timeForVisualEffects * 0.03f;

            float scatterStrength = 2.5f + _aimMotion * 4.5f;
            if (sb.hasReboundOffPlat)
                scatterStrength += 1.5f;
            if (sb.reflecting)
                scatterStrength *= 0.6f;

            float seedA = projectile.identity * 0.713f;
            float seedB = projectile.identity * 1.137f;

            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);
                Vector2 basePos = Vector2.Lerp(start, end, t);

                float centerEnvelope = (float)Math.Sin(t * Math.PI);
                centerEnvelope = (float)Math.Pow(centerEnvelope, 1.25f);

                float endStraighten = 1f - Utils.GetLerpValue(0.72f, 1f, t, true);

                float wave1 = (float)Math.Sin(t * 6.5f - time * 1.4f + seedA);
                float wave2 = (float)Math.Cos(t * 11f + time * 1.9f + seedB);

                float offset = (wave1 * 0.7f + wave2 * 0.3f) * scatterStrength * centerEnvelope * endStraighten;
                positions[i] = basePos + normal * offset;

                if (i == 0)
                    positions[i] = start;
                else if (i == pointCount - 1)
                    positions[i] = end;
            }

            int tailStart = pointCount - 4;
            for (int i = tailStart; i < pointCount; i++)
            {
                float t = (i - tailStart) / 3f;
                Vector2 linePos = Vector2.Lerp(start, end, i / (float)(pointCount - 1));
                positions[i] = Vector2.Lerp(positions[i], linePos, t * 0.95f);
            }

            for (int i = 0; i < pointCount - 1; i++)
            {
                Vector2 diff = positions[i + 1] - positions[i];
                rotations[i] = diff.LengthSquared() > 0.001f ? diff.ToRotation() : dir.ToRotation();
            }

            rotations[pointCount - 1] = rotations[pointCount - 2];
        }

        private void DrawPrettyBeam(
            Projectile projectile,
            Shadowbolt sb,
            Vector2 start,
            Vector2 end,
            float opacity,
            float widthFade)
        {
            Vector2 beam = end - start;
            float length = beam.Length();
            if (length <= 8f || opacity <= 0.01f)
                return;

            BuildBeamPoints(projectile, sb, start, end, out Vector2[] positions, out float[] rotations);

            float pulse = 1f + (float)Math.Cos(Main.timeForVisualEffects * 0.24f) * 0.035f;
            float widthMult = sb.hasReboundOffPlat ? 3.5f : 3f;
            if (sb.reflecting)
                widthMult *= 0.96f;
            
            float outerBase = 11f * projectile.scale * widthMult * pulse * MathHelper.Lerp(0.65f, 1f, widthFade);
            float midBase   = 4.6f * projectile.scale * widthMult * pulse * MathHelper.Lerp(0.70f, 1f, widthFade);
            float coreBase  = 1.6f * projectile.scale * widthMult * pulse * MathHelper.Lerp(0.76f, 1f, widthFade);

            Color StripColor(float progress) => Color.White * opacity;

            float OuterWidth(float progress)
            {
                float tipFade = MathHelper.Lerp(1f, 0.35f, Utils.GetLerpValue(0.82f, 1f, progress, true));
                float body = 0.65f + (float)Math.Sin(progress * Math.PI) * 0.35f;
                return outerBase * body * tipFade;
            }

            float MidWidth(float progress)
            {
                float tipFade = MathHelper.Lerp(1f, 0.42f, Utils.GetLerpValue(0.86f, 1f, progress, true));
                float body = 0.78f + (float)Math.Sin(progress * Math.PI) * 0.22f;
                return midBase * body * tipFade;
            }

            float CoreWidth(float progress)
            {
                float tipFade = MathHelper.Lerp(1f, 0.50f, Utils.GetLerpValue(0.88f, 1f, progress, true));
                float body = 0.9f + (float)Math.Sin(progress * Math.PI) * 0.1f;
                return coreBase * body * tipFade;
            }

            VertexStrip outerStrip = new();
            outerStrip.PrepareStrip(positions, rotations, StripColor, OuterWidth, -Main.screenPosition, includeBacksides: true);

            VertexStrip midStrip = new();
            midStrip.PrepareStrip(positions, rotations, StripColor, MidWidth, -Main.screenPosition, includeBacksides: true);

            VertexStrip coreStrip = new();
            coreStrip.PrepareStrip(positions, rotations, StripColor, CoreWidth, -Main.screenPosition, includeBacksides: true);

            Color outerColor = sb.hasReboundOffPlat ? new Color(115, 45, 220) : new Color(72, 14, 120);
            Color midColor = sb.hasReboundOffPlat ? new Color(255, 135, 255) : new Color(228, 60, 245);
            Color coreColor = new Color(255, 255, 255);

            _tendrilEffect.Parameters["reps"]?.SetValue(Math.Max(length / 1100f, 0.45f));
            _tendrilEffect.Parameters["WorldViewProjection"]?.SetValue(Main.GameViewMatrix.NormalizedTransformationmatrix);
            _tendrilEffect.Parameters["progress"]?.SetValue((float)Main.timeForVisualEffects * 0.015f);

            _tendrilEffect.Parameters["TrailTexture"]?.SetValue(_trailThin.Value);
            _tendrilEffect.Parameters["ColorOne"]?.SetValue(outerColor.ToVector3() * (2.0f * opacity));
            _tendrilEffect.Parameters["glowThreshold"]?.SetValue(0.97f);
            _tendrilEffect.Parameters["glowIntensity"]?.SetValue(0.95f + opacity * 0.5f);
            _tendrilEffect.CurrentTechnique.Passes["MainPS"].Apply();
            outerStrip.DrawTrail();

            _tendrilEffect.Parameters["TrailTexture"]?.SetValue(_trailBloom.Value);
            _tendrilEffect.Parameters["ColorOne"]?.SetValue(midColor.ToVector3() * (6.8f * opacity));
            _tendrilEffect.Parameters["glowThreshold"]?.SetValue(0.74f);
            _tendrilEffect.Parameters["glowIntensity"]?.SetValue(1.6f + opacity * 0.8f);
            _tendrilEffect.CurrentTechnique.Passes["MainPS"].Apply();
            midStrip.DrawTrail();

            _tendrilEffect.Parameters["TrailTexture"]?.SetValue(_trailThin.Value);
            _tendrilEffect.Parameters["ColorOne"]?.SetValue(coreColor.ToVector3() * (4.4f * opacity));
            _tendrilEffect.Parameters["glowThreshold"]?.SetValue(0.60f);
            _tendrilEffect.Parameters["glowIntensity"]?.SetValue(1.9f + opacity * 0.6f);
            _tendrilEffect.CurrentTechnique.Passes["MainPS"].Apply();
            coreStrip.DrawTrail();

            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );
        }
        
        private void DrawShadowboltDispersion(
            Projectile projectile,
            Shadowbolt sb,
            Vector2 start,
            Vector2 end,
            float opacity)
        {
            Vector2 beam = end - start;
            float length = beam.Length();
            if (length <= 12f)
                return;

            Vector2 dir = beam.SafeNormalize(Vector2.UnitX);
            Vector2 normal = dir.RotatedBy(MathHelper.PiOver2);
            float time = (float)Main.timeForVisualEffects;

            float motionScatter = 0.10f + _aimMotion * 0.22f;
            if (sb.hasReboundOffPlat)
                motionScatter += 0.05f;
            if (sb.reflecting)
                motionScatter *= 0.75f;

            int streakCount = 4;
            
            for (int i = 0; i < streakCount; i++)
            {
                float seed = projectile.identity * 0.381f + i * 0.917f;
                float t = (i + 0.5f) / streakCount;
                float slide = (float)Math.Sin(time * 0.06f + seed) * 0.08f;
                t = MathHelper.Clamp(t + slide, 0.08f, 0.92f);

                Vector2 pos = Vector2.Lerp(start, end, t);
                float spread = (float)Math.Sin(time * 0.11f + seed * 2.4f) * (1.8f + motionScatter * 4.2f);
                float spread2 = (float)Math.Cos(time * 0.18f + seed * 1.7f) * (0.8f + motionScatter * 2.1f);

                Vector2 offset = normal * spread + dir * spread2;
                Vector2 drawPos = pos + offset - Main.screenPosition;

                float rot = dir.ToRotation() + MathHelper.PiOver2 + spread * 0.008f;
                Color c = Color.Lerp(Color.Indigo, Color.Orchid, 0.25f + 0.5f * (i / (float)streakCount));
                c *= opacity * 0.85f;

                Main.EntitySpriteDraw(
                    _glowSparkTex.Value,
                    drawPos,
                    null,
                    c with { A = 0 },
                    rot,
                    _glowSparkTex.Value.Size() * 0.5f,
                    new Vector2(0.08f + motionScatter * 0.03f, 0.42f + motionScatter * 0.18f),
                    SpriteEffects.None
                );

                Main.EntitySpriteDraw(
                    _glowSparkTex.Value,
                    drawPos,
                    null,
                    (Color.White with { A = 0 }) * opacity * 0.45f,
                    rot,
                    _glowSparkTex.Value.Size() * 0.5f,
                    new Vector2(0.04f, 0.18f + motionScatter * 0.08f),
                    SpriteEffects.None
                );
            }
            
            int moteCount = 3;
            for (int i = 0; i < moteCount; i++)
            {
                float seed = projectile.identity * 0.219f + i * 1.331f;
                float t = (float)((Math.Sin(time * 0.09f + seed) + 1f) * 0.5f);
                t = MathHelper.Lerp(0.12f, 0.88f, t);

                Vector2 pos = Vector2.Lerp(start, end, t);

                float radial = 1.5f + 3.2f * motionScatter;
                Vector2 moteOffset =
                    normal * (float)Math.Sin(time * 0.21f + seed * 2.7f) * radial +
                    dir * (float)Math.Cos(time * 0.14f + seed * 1.1f) * radial * 0.35f;

                float scale = 0.03f + 0.025f * motionScatter;

                Main.EntitySpriteDraw(
                    _bloomCircleTex.Value,
                    pos + moteOffset - Main.screenPosition,
                    null,
                    (Color.Lerp(Color.White, Color.Purple, 0.7f) with { A = 0 }) * opacity * 0.42f,
                    0f,
                    _bloomCircleTex.Value.Size() * 0.5f,
                    scale,
                    SpriteEffects.None
                );
            }
            
            if (!sb.reflecting && sb.time >= 22 && !sb.hasReboundOffPlat)
            {
                for (int i = 0; i < 4; i++)
                {
                    float f = (i + 1) * 0.22f;
                    Vector2 drawPos = projectile.Center - Main.screenPosition - dir * (10f + i * 4f);

                    Main.EntitySpriteDraw(
                        _glowSparkTex.Value,
                        drawPos,
                        null,
                        (Color.White with { A = 0 }) * (opacity * 0.55f),
                        dir.ToRotation() + MathHelper.PiOver2,
                        _glowSparkTex.Value.Size() * 0.5f,
                        new Vector2(0.28f, 0.85f) * f * (sb.hasReboundOffPlat ? 1.1f : 0.5f),
                        SpriteEffects.None
                    );
                }
            }
        }

        private static void DrawBeamStartFlare(Vector2 worldPos, Vector2 velocity, bool rebounded, float opacity)
        {
            if (opacity <= 0.01f)
                return;

            Vector2 drawPos = worldPos - Main.screenPosition;
            float rot = velocity.SafeNormalize(Vector2.UnitX).ToRotation() + MathHelper.PiOver2;
            float pulse = 1f + (float)Math.Cos(Main.timeForVisualEffects * 0.35f) * 0.04f;

            Color purple = rebounded ? new Color(255, 125, 255) : new Color(121, 7, 179);
            Vector2 flareScale = new Vector2(0.42f, 0.34f) * pulse;

            Main.EntitySpriteDraw(
                _portalFlare.Value,
                drawPos,
                null,
                (purple with { A = 0 }) * opacity * 0.45f,
                rot,
                _portalFlare.Value.Size() * 0.5f,
                flareScale,
                SpriteEffects.None
            );

            Main.EntitySpriteDraw(
                _portalFlare.Value,
                drawPos,
                null,
                (Color.White with { A = 0 }) * opacity * 0.32f,
                rot,
                _portalFlare.Value.Size() * 0.5f,
                flareScale * 0.55f,
                SpriteEffects.None
            );
        }

        private static void DrawPlatform(Shadowbolt sb)
        {
            for (int i = 0; i < 5; i++)
            {
                Color auraColor = Color.Lerp(Color.Indigo, Color.Purple, Utils.GetLerpValue(0f, 5f, i))
                                  with { A = 0 } * 0.55f * sb.platFade;

                Vector2 offset = (MathHelper.TwoPi * i / 7f + Main.GlobalTimeWrappedHourly * 30f).ToRotationVector2();
                offset *= MathHelper.Lerp(3f, 5.25f, (float)Math.Cos(Main.GlobalTimeWrappedHourly * 6f) * 0.5f + 0.5f);

                Main.EntitySpriteDraw(
                    _platformTex.Value,
                    sb.platPosCenter - Main.screenPosition + offset,
                    null,
                    auraColor,
                    sb.platRot + MathHelper.PiOver2,
                    _platformTex.Value.Size() * 0.5f,
                    new Vector2(1f, 0.7f),
                    SpriteEffects.None
                );
            }

            for (int i = 0; i < 4; i++)
            {
                Main.EntitySpriteDraw(
                    _platformTex.Value,
                    sb.platPosCenter - Main.screenPosition,
                    null,
                    (Color.Purple with { A = 0 }) * sb.platFade,
                    sb.platRot + MathHelper.PiOver2,
                    _platformTex.Value.Size() * 0.5f,
                    new Vector2(1f, 0.7f),
                    SpriteEffects.None
                );
            }

            Main.EntitySpriteDraw(
                _platformTex.Value,
                sb.platPosCenter - Main.screenPosition,
                null,
                (Color.White with { A = 0 }) * sb.platFade * 0.6f,
                sb.platRot + MathHelper.PiOver2,
                _platformTex.Value.Size() * 0.5f,
                new Vector2(1f, 0.7f) * 0.93f,
                SpriteEffects.None
            );
        }

        private static void DrawRechargeFlare(Shadowbolt sb)
        {
            float randSize = 1f + (float)Math.Sin(Main.timeForVisualEffects * 0.35f) * 0.08f;
            float lerp = Utils.GetLerpValue(-25f, 20f, sb.reflectionTimer, true);

            Main.EntitySpriteDraw(
                _bloomCircleTex.Value,
                sb.platPosWall - Main.screenPosition,
                null,
                Color.Purple with { A = 0 },
                0f,
                _bloomCircleTex.Value.Size() * 0.5f,
                0.65f * lerp * randSize,
                SpriteEffects.None
            );

            Main.EntitySpriteDraw(
                _bloomCircleTex.Value,
                sb.platPosWall - Main.screenPosition,
                null,
                Color.White with { A = 0 },
                0f,
                _bloomCircleTex.Value.Size() * 0.5f,
                0.45f * lerp * randSize,
                SpriteEffects.None
            );
        }

        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (projectile.ModProjectile is not Shadowbolt sb)
                return;

            float minMult = 0.25f;
            int hitsToMinMult = 7;
            float damageMult = Utils.Remap(projectile.numHits, 0, hitsToMinMult, 1f, minMult, true);
            modifiers.SourceDamage *= (sb.hasReboundOffPlat ? 2.5f : 0.8f) * damageMult;

            if (!sb.hasReboundOffPlat && projectile.numHits == 0 && !sb.hasSetPlatSpawn)
            {
                sb.spawnPlat = true;
                sb.hasSetPlatSpawn = true;
            }
        }

        public override bool? CanDamage(Projectile projectile)
        {
            if (projectile.ModProjectile is Shadowbolt sb && sb.reflecting)
                return false;

            return null;
        }

        private static void LoadAssets()
        {
            _tendrilEffect ??= ModContent.Request<Effect>(
                "VFXPlus/Effects/TrailShaders/TendrilShader",
                AssetRequestMode.ImmediateLoad
            ).Value;

            _trailBloom ??= ModContent.Request<Texture2D>(
                "VFXPlus/Assets/Trails/s06sBloom",
                AssetRequestMode.ImmediateLoad
            );

            _trailThin ??= ModContent.Request<Texture2D>(
                "VFXPlus/Assets/Trails/ThinGlowLine",
                AssetRequestMode.ImmediateLoad
            );

            _portalFlare ??= ModContent.Request<Texture2D>(
                "VFXPlus/Assets/Pixel/GlowingFlare",
                AssetRequestMode.ImmediateLoad
            );

            _starFlare ??= ModContent.Request<Texture2D>(
                "VFXPlus/Assets/Pixel/CrispStarPMA",
                AssetRequestMode.ImmediateLoad
            );

            _platformTex ??= ModContent.Request<Texture2D>(
                "CalamityMod/Projectiles/Magic/ShadowPlatform",
                AssetRequestMode.ImmediateLoad
            );

            _bloomCircleTex ??= ModContent.Request<Texture2D>(
                "CalamityMod/Particles/BloomCircle",
                AssetRequestMode.ImmediateLoad
            );

            _glowSparkTex ??= ModContent.Request<Texture2D>(
                "CalamityMod/Particles/GlowSpark",
                AssetRequestMode.ImmediateLoad
            );
        }
    }
}
*/