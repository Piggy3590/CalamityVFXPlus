using System;
using System.Collections.Generic;
using CalamityMod.Projectiles.Magic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics;
using Terraria.ModLoader;
using VFXPlus.Common.Drawing;
using CalamityVFXPlus;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.ID;
using VFXPlus;
using VFXPlus.Common;
using VFXPLus.Common;
using VFXPlus.Common.Utilities;
using VFXPlus.Content.Dusts;
using VFXPlus.Content.Weapons.Magic.Hardmode.Misc;

namespace CalamityVFXPlus.Projectiles.Magic;

public class DarkSparkOverride : GlobalProjectile
{
    public override bool InstancePerEntity => true;

    public override void SetDefaults(Projectile entity)
    {
        entity.hide = true;
    }

    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
    {
        return lateInstantiation &&
               entity.type ==
               ModContent
                   .ProjectileType<
                       DarkSparkPrism>(); //&& ModContent.GetInstance<VFXPlusToggles>().MagicToggle.LastPrismToggle;
    }

    //How far away from prism does laser start
    private float offsetDistance = 18f;

    float endRot = 0f;
    int timer = 0;

    public override bool PreAI(Projectile projectile)
    {
        if (lpci == null)
            GetCombinedLaserInfo();
        if (projectile.ai[0] == 1)
        {
            drawSigil = true;

            for (int i = 0; i < 35; i++)
            {
                Color col = Main.hslToRgb(Main.rand.NextFloat(0f, 1f), 1f, 0.5f);


                Vector2 vel = projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(1.5f) *
                              Main.rand.NextFloat(10f, 30f);

                Dust p = Dust.NewDustPerfect(projectile.Center, ModContent.DustType<WindLine>(), vel, newColor: col,
                    Scale: Main.rand.NextFloat(0.5f, 0.65f) * 1.5f);
                p.customData = new WindLineBehavior(VelFadePower: 0.92f, TimeToStartShrink: 11, YScale: 0.5f);
            }

            FlashSystem.SetCAFlashEffect(0.3f, 25, 1f, 0.65f, true);

            Main.player[projectile.owner].GetModPlayer<ScreenShakePlayer>().ScreenShakePower = 20f;

            combinedLaserStartBoostPower = 1f;

            //Sound
            SoundStyle style = new SoundStyle("CalamityMod/Sounds/NPCKilled/CeaselessVoidDeath") with
            {
                Volume = .1f, Pitch = 0.2f, MaxInstances = -1
            };
            SoundEngine.PlaySound(style, projectile.Center);

            SoundStyle style2 = new SoundStyle("VFXPlus/Sounds/Effects/water_blast_projectile_spell_03") with
            {
                Volume = .25f, Pitch = .4f, MaxInstances = -1
            };
            SoundEngine.PlaySound(style2, projectile.Center);

            SoundStyle style4 = new SoundStyle("VFXPlus/Sounds/Effects/laser_fire") with
            {
                Volume = .1f, Pitch = .1f, MaxInstances = -1
            };
            SoundEngine.PlaySound(style4, projectile.Center);

            SoundStyle style5 = new SoundStyle("Terraria/Sounds/Item_163") with
            {
                Volume = 0.2f, Pitch = .2f, MaxInstances = -1
            };
            SoundEngine.PlaySound(style5, projectile.Center);

            SoundStyle style6 = new SoundStyle("CalamityMod/Sounds/Custom/AbilitySounds/RageActivate") with
            {
                Volume = 0.2f, Pitch = .7f, MaxInstances = -1
            };
            SoundEngine.PlaySound(style6, projectile.Center);

            SoundStyle style7 = new SoundStyle("VFXPlus/Sounds/Effects/Fire/HeatRayShot") with
            {
                Volume = 0.6f, Pitch = .4f, MaxInstances = -1
            };
            SoundEngine.PlaySound(style7, projectile.Center);

            SoundStyle style8 = new SoundStyle("VFXPlus/Sounds/Effects/Fire/SireFire") with
            {
                Volume = 0.66f, Pitch = 0.5f, MaxInstances = -1
            };
            SoundEngine.PlaySound(style8, projectile.Center);
        }

        /*
        if (projectile.ai[0] > 180 && projectile.ai[0] < 195)
        {
            float pow = Main.player[projectile.owner].GetModPlayer<ScreenShakePlayer>().ScreenShakePower;
            Main.player[projectile.owner].GetModPlayer<ScreenShakePlayer>().ScreenShakePower =
                Math.Clamp(pow, 15f, 60f);
        }
        */
        
        if (timer % 1 == 0)
        {
            float dist = projectile.ai[2];

            for (int i = 0; i < dist * 0.75f; i += 125)
            {
                Vector2 pos = projectile.Center + new Vector2(i, 0f).RotatedBy(projectile.velocity.ToRotation());
                float rot = projectile.velocity.ToRotation();
                
                int dustColsLength = lpci.dustColors.Count;
                Color color = lpci.dustColors[Main.rand.Next(0, dustColsLength)];

                if (Main.rand.NextBool(3))
                {
                    Vector2 offset = rot.ToRotationVector2().RotatedBy(MathHelper.PiOver2) *
                                     Main.rand.NextFloat(-35, 35);
                    Vector2 vel = rot.ToRotationVector2().RotatedByRandom(0.05f) * Main.rand.NextFloat(2, 7) *
                                  1.4f; //1f

                    if (!Main.rand.NextBool(3))
                    {
                        Dust d = Dust.NewDustPerfect(pos + offset, ModContent.DustType<GlowFlare>(), vel * 2f,
                            newColor: color * 1f, Scale: Main.rand.NextFloat(0.5f, 1.5f) * 0.5f);
                        d.noLight = false;
                        d.customData = new GlowFlareBehavior(0.4f, 2.5f);
                    }
                    else
                    {
                        Dust d = Dust.NewDustPerfect(pos + offset, ModContent.DustType<GlowPixelCross>(),
                            vel * 3.5f, newColor: color * 1f, Scale: Main.rand.NextFloat(0.5f, 1.5f) * 0.35f);
                        d.noLight = false;
                        d.customData = DustBehaviorUtil.AssignBehavior_GPCBase(rotPower: 0.17f,
                            postSlowPower: 0.84f, velToBeginShrink: 5f, fadePower: 0.9f, shouldFadeColor: false);
                    }
                }
            }

            //End point dust
            Vector2 dustPos = projectile.Center +
                              new Vector2(projectile.ai[2], 0f).RotatedBy(projectile.velocity.ToRotation());
            for (int i = 0; i < 3 + Main.rand.Next(0, 2); i++) //4 //2,2
            {
                Vector2 vel = Main.rand.NextVector2Circular(22f, 22f);

                int dustColsLength = lpci.dustColors.Count;
                Color color = lpci.dustColors[Main.rand.Next(0, dustColsLength)];

                Dust p = Dust.NewDustPerfect(dustPos, ModContent.DustType<WindLine>(), vel,
                    newColor: color, Scale: Main.rand.NextFloat(0.5f, 0.65f) * 1f);
                p.customData = new WindLineBehavior(VelFadePower: 0.92f, TimeToStartShrink: 5, YScale: 0.5f);

                if (i == 0)
                {
                    Dust p2 = Dust.NewDustPerfect(dustPos + vel * 3f, ModContent.DustType<SoftGlowDust>(), vel * 2f,
                        newColor: color * 0.85f, Scale: Main.rand.NextFloat(0.5f, 0.65f) * 0.35f);
                    p2.customData = DustBehaviorUtil.AssignBehavior_SGDBase(overallAlpha: 0.1f);
                }
            }

            //Light at end of laser
            Vector2 lightPos = projectile.Center +
                               new Vector2(projectile.ai[2], 0f).RotatedBy(projectile.velocity.ToRotation());
            Lighting.AddLight(lightPos, Color.White.ToVector3() * 1.1f);

            endRot += 1.15f * (projectile.velocity.X > 0 ? 1f : -1f); //0.85f

        }

        combinedLaserStartBoostPower = Math.Clamp(MathHelper.Lerp(combinedLaserStartBoostPower, -0.5f, 0.06f), 0f, 10f);

        timer++;

        return base.PreAI(projectile);
    }


    float combinedLaserStartBoostPower = 0f;
    float combinedLaserScale = 1f;
    bool drawSigil = false;

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        ModContent.GetInstance<PixelationSystem>().QueueRenderAction(RenderLayer.Dusts, () =>
        {
            DarkSigil(projectile);
        });


        Texture2D vanillaTex = TextureAssets.Projectile[projectile.type].Value;

        Vector2 drawPos = projectile.Center - Main.screenPosition +
                          new Vector2(0f, 1f * Main.player[projectile.owner].gfxOffY);
        Rectangle sourceRectangle = vanillaTex.Frame(1, Main.projFrames[projectile.type], frameY: projectile.frame);
        Vector2 TexOrigin = sourceRectangle.Size() / 2f;

        SpriteEffects SE = projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

        //Border
        Color[] rainbow = { Color.Red, Color.Orange, Color.Yellow, Color.Green, Color.Lime, Color.DodgerBlue, Color.Violet };
        for (int i = 0; i < 7; i++)
        {
            float rot = i * (MathHelper.TwoPi / 6f);
            
            const float ExpandTime = 60f * 10f;
            float prog = MathHelper.Clamp(projectile.ai[0] / ExpandTime, 0f, 1f);
            float dis = MathHelper.Lerp(0f, 2f, Easings.easeOutQuad(prog)); //10f
            float scale = MathHelper.Lerp(2f, 1f, prog);

            Vector2 off = new Vector2(dis, 0f).RotatedBy(rot + (float)Main.timeForVisualEffects * 0.15f);
            Main.EntitySpriteDraw(vanillaTex, drawPos + off + new Vector2(0f, 0f), sourceRectangle,
                rainbow[i] with { A = 0 } * 0.35f * Easings.easeInSine(prog), projectile.rotation, TexOrigin,
                projectile.scale * 1.07f * scale, SE);
        }

        Main.EntitySpriteDraw(vanillaTex, drawPos + Main.rand.NextVector2CircularEdge(2f, 2f), sourceRectangle,
            Color.White with { A = 0 } * 0.1f, projectile.rotation, TexOrigin, projectile.scale * 1.1f, SE);
        Main.EntitySpriteDraw(vanillaTex, drawPos, sourceRectangle, Color.White with { A = 180 }, projectile.rotation,
            TexOrigin, projectile.scale, SE);

        //Not returning true and drawing manually because the last prism apparently doesn't respect gfxOffY 
        return false;
    }

    private float GetPrismBeamLength(Projectile prism)
    {
        Vector2 dir = prism.velocity.SafeNormalize(-Vector2.UnitY);
        Vector2 samplingPoint = prism.Center;
        Player p = Main.player[prism.owner];
        if (!Collision.CanHitLine(p.Center, 0, 0, prism.Center, 0, 0))
            samplingPoint = p.Center;

        float[] samples = new float[3];
        Collision.LaserScan(samplingPoint, dir, 1f, 2400f, samples);

        float avg = (samples[0] + samples[1] + samples[2]) / 3f;
        return avg;
    }

    Effect myEffect = null;
    Effect laserEffect = null;

    public void DarkSigil(Projectile projectile)
    {
        //TODO load all of these once
        Texture2D star = VFXPlusTextures.Simple_Lens_Flare_11.Value;
        Texture2D star2 = VFXPlusTextures.flare_16.Value;
        Texture2D sigil = VFXPlusTextures.whiteFireEyeA.Value;

        if (myEffect == null)
            myEffect = ModContent
                .Request<Effect>("CalamityVFXPlus/Effects/GradientSigil", AssetRequestMode.ImmediateLoad).Value;

        var gradParam = myEffect.Parameters["GradTex"];
        gradParam?.SetValue(VFXPlusTextures.DarkGrad.Value);
        myEffect.Parameters["rotation"].SetValue(endRot * 0.03f * 2.5f);
        myEffect.Parameters["intensity"].SetValue(1f);

        myEffect.Parameters["gradOffset"].SetValue(endRot * 0.01f * 2.5f);
        myEffect.Parameters["gradScale"]?.SetValue(1.0f);
        myEffect.Parameters["fadeStrength"].SetValue(1f);

        Vector2 drawPos = projectile.Center - Main.screenPosition +
                          projectile.velocity.SafeNormalize(Vector2.UnitX) * offsetDistance;
        float rot = projectile.velocity.ToRotation();
        float length = GetPrismBeamLength(projectile);

        float sin1 = MathF.Sin((float)Main.timeForVisualEffects * 0.04f);
        float sin2 = MathF.Cos((float)Main.timeForVisualEffects * 0.06f);
        float sin3 = -MathF.Cos(((float)Main.timeForVisualEffects * 0.08f) / 2f) + 1f;

        Vector2 sigilScale1 = new Vector2(0.2f, 1f) * 0.55f * combinedLaserScale * 1.4f;
        Vector2 sigilScale2 = sigilScale1 * (1.75f + (0.25f * sin1)) * 1.4f;

        //Main.spriteBatch.Draw(orb, endPoint + new Vector2(0f, 0f), null, Color.White with { A = 0 } * 0.3f, 0f, orb.Size() / 2f, 2f, 0, 0f);

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, myEffect, Main.GameViewMatrix.EffectMatrix);

        //Main sigil
        Main.spriteBatch.Draw(sigil, drawPos, null, Color.White, rot, sigil.Size() / 2, sigilScale1, 0, 0f);
        Main.spriteBatch.Draw(sigil, drawPos, null, Color.White, rot, sigil.Size() / 2, sigilScale1, 0, 0f);

        Main.spriteBatch.Draw(star, drawPos + new Vector2(1f, 0f).RotatedBy(rot) * (15f * sin3), null, Color.White, rot,
            star.Size() / 2, sigilScale2, 0, 0f);

        Main.spriteBatch.Draw(star2, drawPos, null, Color.White, rot, star2.Size() / 2, sigilScale1 * 1.6f, 0, 0f);
        Main.spriteBatch.Draw(star2, drawPos, null, Color.White, rot, star2.Size() / 2, sigilScale1 * 1.6f, 0, 0f);
        
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.EffectMatrix);
        Main.graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;

        //^ Reset with EffectMatrix because otherwise the wind lines draw fucked up for some reason
    }

    LastPrismColorInfo lpci = null;

    public void GetCombinedLaserInfo()
    {
        lpci = new LastPrismColorInfo(0.66f, 0.66f, 1.03f, 0.77f);
    }

    public class DarkSparkLaserOverride : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        {
            return entity.type == ModContent.ProjectileType<DarkSparkBeam>();
        }

        public override void SetDefaults(Projectile entity)
        {
            entity.hide = true;
            base.SetDefaults(entity);
        }

        public override void DrawBehind(Projectile projectile, int index, List<int> behindNPCsAndTiles,
            List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
            base.DrawBehind(projectile, index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers,
                overWiresUI);
        }

        int timer = 0;
        LastPrismColorInfo lpci = null;
        
        public override bool PreAI(Projectile projectile)
        {
            // init
            if (timer == 0)
                lpci = new LastPrismColorInfo(0.66f, 0.66f, 1.03f, 0.77f);
            Vector2? chargeUpCenter = null;

            if (projectile.velocity.HasNaNs() || projectile.velocity == Vector2.Zero)
                projectile.velocity = -Vector2.UnitY;

            int parentIndex = (int)projectile.ai[1];
            if (parentIndex < 0 || parentIndex >= Main.maxProjectiles ||
                !Main.projectile[parentIndex].active ||
                Main.projectile[parentIndex].type != ModContent.ProjectileType<DarkSparkPrism>())
            {
                projectile.Kill();
                return false;
            }

            Projectile parent = Main.projectile[parentIndex];

            float laserPosition = (int)projectile.ai[0] - 2.5f;
            Vector2 aimDirection = Vector2.Normalize(parent.velocity);

            // contentReference[oaicite:7]{index=7}
            float laserTimer;
            float yOffset;
            float laserRotationSpeed;
            float forwardOffset;

            projectile.Opacity = 1f;

            if (parent.ai[0] < 360f)
            {
                laserTimer = parent.ai[0] / 720f;                 // 0..0.5
                yOffset = 6f + parent.ai[0] / 360f * 7f;          // 6..13
                laserRotationSpeed = (parent.ai[0] < 240f)
                    ? 1.75f
                    : 3f + 5f * ((parent.ai[0] - 240f) / 120f);   // 3..8
                forwardOffset = -2f - parent.ai[0] / 360f * 5f;   // -2..-7
            }
            else
            {
                laserTimer = 0.5f;
                laserRotationSpeed = 10.875f;
                yOffset = 13f;
                forwardOffset = -7f;
            }

            // contentReference[oaicite:8]{index=8}
            float phase = (parent.ai[0] + laserPosition * laserRotationSpeed) / (laserRotationSpeed * 6f) * MathHelper.TwoPi;

            // contentReference[oaicite:9]{index=9}
            float rotOffset = Vector2.UnitY.RotatedBy(phase).Y * (MathHelper.Pi / 6f) * laserTimer * 0.33f;

            // contentReference[oaicite:10]{index=10}
            Vector2 radialOffset =
                (Vector2.UnitY.RotatedBy(phase) * new Vector2(4f, yOffset)).RotatedBy(parent.velocity.ToRotation());

            // 위치(원본) :contentReference[oaicite:11]{index=11}
            projectile.position = parent.Center + aimDirection * 16f - projectile.Size / 2f
                                + new Vector2(0f, -parent.gfxOffY);
            projectile.position += parent.velocity.ToRotation().ToRotationVector2() * forwardOffset;
            projectile.position += radialOffset;

            // 방향(원본) :contentReference[oaicite:12]{index=12}
            projectile.velocity = Vector2.Normalize(parent.velocity).RotatedBy(rotOffset);

            // ✅ 두께/스케일(원본): 2.25 -> 1.5로 감소 후 고정 :contentReference[oaicite:13]{index=13}
            projectile.scale = 1.5f * (1.5f - laserTimer);

            // ✅ 데미지(원본): 0.25x -> 2.2x :contentReference[oaicite:14]{index=14}
            float dmgT = parent.ai[0] / 600f;
            if (dmgT > 1f) dmgT = 1f;
            projectile.damage = (int)(parent.damage * MathHelper.Lerp(0.25f, 2.2f, dmgT));

            // 충전 이후 스캔 중심(원본) :contentReference[oaicite:15]{index=15}
            if (parent.ai[0] >= 360f)
                chargeUpCenter = parent.Center;

            if (!Collision.CanHitLine(Main.player[projectile.owner].Center, 0, 0, parent.Center, 0, 0))
                chargeUpCenter = Main.player[projectile.owner].Center;

            if (projectile.velocity.HasNaNs() || projectile.velocity == Vector2.Zero)
                projectile.velocity = -Vector2.UnitY;

            // 회전/정규화(원본) :contentReference[oaicite:16]{index=16}
            float laserRot = projectile.velocity.ToRotation();
            projectile.rotation = laserRot - MathHelper.PiOver2;
            projectile.velocity = laserRot.ToRotationVector2();

            // 길이 스캔(원본) :contentReference[oaicite:17]{index=17}
            Vector2 samplingPoint = chargeUpCenter ?? projectile.Center;
            float[] samples = new float[2];
            Collision.LaserScan(samplingPoint, projectile.velocity, 0f * projectile.scale, 2400f, samples);

            float beamLen = (samples[0] + samples[1]) * 0.5f;
            projectile.localAI[1] = MathHelper.Lerp(projectile.localAI[1], beamLen, 0.75f); // 원본 0.75 :contentReference[oaicite:18]{index=18}

            // ❗ 원본은 friendly 토글 안 함. 네 코드의 parent.ai[0] > 30f 같은거 제거.
            // projectile.friendly = true; // defaults가 true면 굳이 안 넣어도 됨.

            timer++;
            return false; // 원본 AI 실행 방지(우리가 다 처리)
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            Projectile parent = Main.projectile[(int)projectile.ai[1]];

            ModContent.GetInstance<PixelationSystem>().QueueRenderAction(RenderLayer.UnderProjectiles, () =>
            {
                if (parent.ai[0] < 3600f)
                    DrawVertexTrailDark(projectile, false);
            });

            return false;

        }

        Effect myEffect = null;

        float randomTimeOffset = Main.rand.NextFloat(0f, 0.15f);
        
        private static readonly Color[] BeamRainbow =
        {
            Color.Red, Color.Orange, Color.DodgerBlue,
            Color.Green, Color.Yellow, Color.Violet,
            Color.Aqua, Color.Green, Color.Yellow
        };

        private readonly Vector2[] _posArr = new Vector2[2];
        private readonly float[] _rotArr = new float[2];

        private Color _currentBeamColor;

        private static Color GetBeamBaseColor(float chargeTicks, int beamSlot)
        {
            Color grayColor = new Color(0.55f, 0.55f, 0.55f, 1);
            Color col = Color.Yellow;
            // 360 ~ 720 : 검정 → 흰색
            if (chargeTicks < 360)
            {
                float t = MathHelper.Clamp(chargeTicks / 360f, 0f, 1f);
                return Color.Lerp(Color.Black, grayColor, t);
            }

            if ((uint)beamSlot < (uint)BeamRainbow.Length)
                col = BeamRainbow[beamSlot];
            col *= 0.7f;
            col.A = (byte)0.4f;

            // 720 ~ 960 : 흰색 → 무지개 슬롯 색
            if (chargeTicks < 600)
            {
                float t = MathHelper.Clamp((chargeTicks - 360) / 240f, 0f, 1f);
                return Color.Lerp(grayColor, col, t);
            }

            // 960 이후 : 슬롯 고정색
            return col;
        }

        // VertexStrip 콜백
        private float[] endRots = new float[10];
        private float _stripAlpha = 1f;
        private Color StripColor(float progress) => new Color(255, 255, 255, (byte)(255f * MathHelper.Clamp(_stripAlpha, 0f, 1f)));
        private float StripWidth(float progress) => 80f;

        private Effect _coreFx;
        private Effect _glowFx;
        
        // ===============================
        // DrawVertexTrailDark (core+glow two-pass)
        // - Core: AlphaBlend, coreSolid=1, powMult=0 (검정 실루엣 또렷 + 끊김 제거)
        // - Glow: Additive, glowFade로 360~720 서서히 ON
        // ===============================
        public void DrawVertexTrailDark(Projectile projectile, bool giveUp)
        {
            if (giveUp)
                return;

            int parentIndex = (int)projectile.ai[1];
            if ((uint)parentIndex >= (uint)Main.maxProjectiles)
                return;

            Projectile parent = Main.projectile[parentIndex];
            if (!parent.active)
                return;

            float chargeTicks = parent.ai[0];
            bool preCharge = chargeTicks < 360f;

            int beamSlot = (int)projectile.ai[0];
            Color beamColor = GetBeamBaseColor(chargeTicks, beamSlot);

            // 360~720 glow 페이드 (초반 튐 방지: t^4)
            float glowT = MathHelper.Clamp((chargeTicks - 360f) / 360f, 0f, 1f);
            float glowFade = glowT * glowT;
            glowFade *= glowFade;
            if (preCharge)
                glowFade = 0f;

            // Geometry
            float len = projectile.localAI[1];
            Vector2 startPoint = projectile.Center + new Vector2(0f, Main.player[projectile.owner].gfxOffY);
            Vector2 endPointWorld = startPoint + projectile.velocity * len;

            Vector2 endPoint = startPoint + projectile.velocity * len;
            float dist = Vector2.Distance(startPoint, endPoint);

            _posArr[0] = startPoint;
            _posArr[1] = endPoint;

            float rot = projectile.velocity.ToRotation();
            _rotArr[0] = rot;
            _rotArr[1] = rot;

            VertexStrip strip = new VertexStrip();
            _stripAlpha = projectile.Opacity * 1.0f;
            strip.PrepareStrip(_posArr, _rotArr, StripColor, StripWidth, -Main.screenPosition, includeBacksides: true);

            if (_coreFx == null)
                _coreFx = ModContent.Request<Effect>("CalamityVFXPlus/Effects/DarkSparkBeam_Core", AssetRequestMode.ImmediateLoad).Value;

            if (_glowFx == null)
                _glowFx = ModContent.Request<Effect>("CalamityVFXPlus/Effects/DarkSparkBeam_Glow", AssetRequestMode.ImmediateLoad).Value;
            
            if (myEffect == null)
                myEffect = ModContent.Request<Effect>("CalamityVFXPlus/Effects/GradientSigil", AssetRequestMode.ImmediateLoad).Value;
            
            void ApplyCommonParams(Effect fx, Color baseCol, float multScale, float sat)
            {
                fx.Parameters["WorldViewProjection"]?.SetValue(Main.GameViewMatrix.NormalizedTransformationmatrix);

                fx.Parameters["onTex"]?.SetValue(ModContent.Request<Texture2D>("VFXPlus/Assets/Trails/TextureLaser").Value);
                fx.Parameters["gradientTex"]?.SetValue(VFXPlusTextures.DarkGrad.Value);

                fx.Parameters["baseColor"]?.SetValue(baseCol.ToVector3());
                fx.Parameters["satPower"]?.SetValue(sat);

                fx.Parameters["sampleTexture1"]?.SetValue(VFXPlusTextures.ThinGlowLine.Value);
                fx.Parameters["sampleTexture2"]?.SetValue(VFXPlusTextures.spark_06.Value);
                fx.Parameters["sampleTexture3"]?.SetValue(VFXPlusTextures.Extra_196_Black.Value);
                fx.Parameters["sampleTexture4"]?.SetValue(VFXPlusTextures.Trail5Loop.Value);

                fx.Parameters["grad1Speed"]?.SetValue(lpci.grad1Speed);
                fx.Parameters["grad2Speed"]?.SetValue(lpci.grad2Speed);
                fx.Parameters["grad3Speed"]?.SetValue(lpci.grad3Speed);
                fx.Parameters["grad4Speed"]?.SetValue(lpci.grad4Speed);

                float easedAlpha = Easings.easeOutQuad(projectile.Opacity);

                fx.Parameters["tex1Mult"]?.SetValue(2f * easedAlpha * multScale);
                fx.Parameters["tex2Mult"]?.SetValue(1f * easedAlpha * multScale);
                fx.Parameters["tex3Mult"]?.SetValue(1.15f * easedAlpha * multScale);
                fx.Parameters["tex4Mult"]?.SetValue(2.5f * easedAlpha * multScale);

                fx.Parameters["totalMult"]?.SetValue(1f * multScale);

                float repValue = dist / 500f;
                fx.Parameters["gradientReps"]?.SetValue(0.35f * repValue);
                fx.Parameters["tex1reps"]?.SetValue(1f * repValue);
                fx.Parameters["tex2reps"]?.SetValue(0.3f * repValue);
                fx.Parameters["tex3reps"]?.SetValue(1f * repValue);
                fx.Parameters["tex4reps"]?.SetValue(0.25f * repValue);

                fx.Parameters["uTime"]?.SetValue(((float)Main.timeForVisualEffects * -0.025f) + randomTimeOffset);
            }

            // -------------------------
            // PASS 1: CORE (AlphaBlend)
            // -------------------------
            // preCharge(0~360): 완전 검정 시작
            Color coreBase = beamColor;

            ApplyCommonParams(_coreFx, coreBase, multScale: 0.85f, sat: 1.0f);

            _coreFx.Parameters["baseColorMult"]?.SetValue(1.8f); // 코어 또렷
            _coreFx.Parameters["powMult"]?.SetValue(0f);         // ✅ pow 발광 제거
            _coreFx.Parameters["coreSolid"]?.SetValue(1f);       // ✅ 끊김 제거

            // ✅ 코어는 투명하면 안 되므로 1.0 기준
            // 코어는 항상 존재해야 함 (투명도는 projectile.Opacity 기준)

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, _coreFx, Main.GameViewMatrix.EffectMatrix);

            _coreFx.CurrentTechnique.Passes["MainPS"].Apply();
            strip.DrawTrail();
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.End();

            // -------------------------
            // PASS 2: GLOW (Additive)
            // -------------------------
            if (glowFade > 0f)
            {
                ApplyCommonParams(_glowFx, beamColor, multScale: 1.0f, sat: 1.0f);

                _glowFx.Parameters["baseColorMult"]?.SetValue(0.5f);
                _glowFx.Parameters["powMult"]?.SetValue(0f);
                _glowFx.Parameters["glowFade"]?.SetValue(glowFade * 0.2f);

                _stripAlpha = projectile.Opacity * 0.4f * glowFade;
                strip.PrepareStrip(_posArr, _rotArr, StripColor, StripWidth, -Main.screenPosition, includeBacksides: true);

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullCounterClockwise, _glowFx, Main.GameViewMatrix.EffectMatrix);
                _glowFx.CurrentTechnique.Passes["MainPS"].Apply();
                strip.DrawTrail();
                if (len >= 8f)
                {
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                        DepthStencilState.None, RasterizerState.CullCounterClockwise, myEffect, Main.GameViewMatrix.EffectMatrix);
                    DrawBeamEndFlare(projectile, endPointWorld);
                }
                Main.pixelShader.CurrentTechnique.Passes[0].Apply();
                Main.spriteBatch.End();
            }

            // Restore
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.EffectMatrix);
        }
        
        private void DrawBeamEndFlare(Projectile beam, Vector2 endWorldPos)
        {
            // 텍스처들
            Texture2D orbGlow = VFXPlusTextures.circle_05.Value;
            Texture2D star = VFXPlusTextures.Simple_Lens_Flare_11.Value;
            Texture2D star2 = VFXPlusTextures.flare_16.Value;

            // 회전: 빔마다 조금 다르게(같으면 겹쳐서 한 덩어리처럼 보임)
            // ai[0] 슬롯을 섞어주면 빔별로 분산됨
            float slot = beam.ai[0];
            float rot = (float)Main.timeForVisualEffects * 0.08f + slot * 0.9f;
            //float rot = endRots[(int)slot] += (slot * 5) + 1.15f * (beam.velocity.X > 0 ? 1f : -1f); //0.85f
            // 스케일: 빔 차징에 따라(원하면 parent.ai[0] 기반)
            float endScale = 0.7f; // 기본
            // endScale = 0.7f + (combinedLaserStartBoostPower * 0.5f); // 부모 값을 쓰고 싶으면 전달/참조 필요

            Vector2 drawPos = endWorldPos - Main.screenPosition;

            // Additive로 그려야 “끝점 번쩍” 느낌 남
            Main.spriteBatch.Draw(orbGlow, drawPos, null, Color.DarkGray * 0.4f, rot * 0.55f, orbGlow.Size() / 2f, 0.7f, 0, 1f);
            Main.spriteBatch.Draw(star,    drawPos, null, Color.Gray, rot * 0.35f, star.Size() / 2f, endScale * 1f, 0, 1f);
            Main.spriteBatch.Draw(star2,   drawPos, null, Color.Gray, rot * 0.2f, star2.Size() / 2f, endScale * 0.7f, 0, 1f);
        }
    }
}