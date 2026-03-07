using CalamityMod.Items.Weapons.Magic;
using CalamityMod.Projectiles.Magic;
using CalamityVFXPlus.Common.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using VFXPlus.Common.Drawing;
using VFXPlus.Content.Dusts;

namespace CalamityVFXPlus.Projectiles.Magic
{
    public sealed class VividBeamVFXGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        
        private bool initialized;
        private bool visualSpawned;
        private float seed;
        private Vector2 spawnCenter;
        private Vector2 spawnVelocity;

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        {
            return entity.type == ModContent.ProjectileType<VividBeam>();
        }

        public override bool PreAI(Projectile projectile)
        {
            if (!initialized)
            {
                initialized = true;
                seed = Main.rand.NextFloat(MathHelper.TwoPi);

                spawnCenter = projectile.Center;
                spawnVelocity = projectile.velocity;

                SoundEngine.PlaySound(VividClarity.BeamSound, projectile.Center);
            }

            if (!visualSpawned)
            {
                visualSpawned = true;
                SpawnVisualProjectile(projectile, spawnCenter, spawnVelocity);
            }

            if (projectile.velocity.LengthSquared() > 0.001f)
                projectile.rotation = projectile.velocity.ToRotation();

            return false;
        }
        
        public void ShootFX(Projectile projectile, Vector2 beamStart, Vector2 beamEnd, float seed = 0f)
        {
            Vector2 dir = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 offsetPos = dir * 5f + beamStart;

            float progress = GetBeamProgress(beamStart, beamEnd, offsetPos);
            Color ringCol = GetBeamColor(progress, seed);

            Vector2 vel = dir * 2f;
            Dust d = Dust.NewDustPerfect(
                beamStart - dir * 5f,
                ModContent.DustType<CirclePulse>(),
                vel,
                newColor: ringCol * 0.5f);

            d.scale = 0.03f;

            CirclePulseBehavior b = new CirclePulseBehavior(0.25f, true, 3, 0.4f, 0.8f);
            b.drawLayer = "Dusts";
            d.customData = b;
        }
        
        public static float GetBeamProgress(Vector2 beamStart, Vector2 beamEnd, Vector2 samplePos)
        {
            Vector2 beam = beamEnd - beamStart;
            float beamLenSq = beam.LengthSquared();

            if (beamLenSq <= 0.0001f)
                return 0f;

            float t = Vector2.Dot(samplePos - beamStart, beam) / beamLenSq;
            return MathHelper.Clamp(t, 0f, 1f);
        }
        
        public static Color GetBeamColor(float beamProgress, float seed = 0f, float timeScroll = -0.024f, float saturation = 1f, float lightness = 0.65f)
        {
            float hue = seed + beamProgress + (float)Main.timeForVisualEffects * timeScroll;
            hue %= 1f;
            if (hue < 0f)
                hue += 1f;

            return Main.hslToRgb(hue, saturation, lightness);
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            return false;
        }

        private void SpawnVisualProjectile(Projectile source, Vector2 start, Vector2 velocityAtSpawn)
        {
            if (velocityAtSpawn.LengthSquared() <= 0.001f)
                return;

            Vector2 unit = velocityAtSpawn.SafeNormalize(Vector2.UnitX);

            float maxVisualLength = 2000f;
            float sampleWidth = 14f;
            float[] samples = new float[3];

            Collision.LaserScan(start, unit, sampleWidth, maxVisualLength, samples);

            float length = 0f;
            for (int i = 0; i < samples.Length; i++)
                length += samples[i];
            length /= samples.Length;

            length = MathHelper.Clamp(length, 80f, maxVisualLength);

            Vector2 end = start + unit * length;

            int lifetime = 10;
            
            ModContent.GetInstance<PixelationSystem>().QueueRenderAction(RenderLayer.UnderProjectiles, () =>
            {
                ShootFX(source, start, end, Main.rand.NextFloat());
            });

            int idx = Projectile.NewProjectile(
                source.GetSource_FromThis(),
                start,
                Vector2.Zero,
                ModContent.ProjectileType<VividBeamVisualProjectile>(),
                0,
                0f,
                source.owner);

            if (!Main.projectile.IndexInRange(idx))
                return;

            Main.projectile[idx].timeLeft = lifetime;

            VividBeamVisualStorage.Data[idx] = new VividBeamVisualData
            {
                Start = start,
                End = end,
                Seed = seed,
                BaseOuterWidth = 62f,
                BaseCoreWidth = 28f,
                MaxLifetime = lifetime
            };
        }
    }
}