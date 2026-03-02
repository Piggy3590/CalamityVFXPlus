using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

internal static class VFXPlusTextures
{
    private const string Base = "VFXPlus/Assets/";

    // ===== Orbs =====
    public static Asset<Texture2D> circle_05;
    public static Asset<Texture2D> feather_circle128PMA;
    public static Asset<Texture2D> flare_12;
    public static Asset<Texture2D> GlowCircleFlare;
    public static Asset<Texture2D> SoftGlow;
    public static Asset<Texture2D> SoftGlow64;
    public static Asset<Texture2D> SolidBloom;

    // ===== Pixel =====
    public static Asset<Texture2D> AnotherLineGlow;
    public static Asset<Texture2D> CrispStarPMA;
    public static Asset<Texture2D> DiamondGlowPMA;
    public static Asset<Texture2D> Extra_89;
    public static Asset<Texture2D> Extra_91;
    public static Asset<Texture2D> FireBallBlur;
    public static Asset<Texture2D> Flare;
    public static Asset<Texture2D> FlareLineHalf;
    public static Asset<Texture2D> GlowingFlare;
    public static Asset<Texture2D> GlowingStar;
    public static Asset<Texture2D> Medusa_Gray;
    public static Asset<Texture2D> Nightglow;
    public static Asset<Texture2D> PartiGlowPMA;
    public static Asset<Texture2D> PixelSwirl;
    public static Asset<Texture2D> Projectile_540;
    public static Asset<Texture2D> RainbowRod;
    public static Asset<Texture2D> Starlight;
    public static Asset<Texture2D> Twinkle;
    public static Asset<Texture2D> SoulSpike;

    // ===== Trails =====
    public static Asset<Texture2D> EnergyTex;
    public static Asset<Texture2D> Extra_196_Black;
    public static Asset<Texture2D> FireTrailGamma;
    public static Asset<Texture2D> FlamesTextureButBlack;
    public static Asset<Texture2D> FlameTrail;
    public static Asset<Texture2D> FlashLightBeamBlack;
    public static Asset<Texture2D> GlowTrail;
    public static Asset<Texture2D> Laser1;
    public static Asset<Texture2D> LavaTrailV1;
    public static Asset<Texture2D> LintyTrail;
    public static Asset<Texture2D> s06sBloom;
    public static Asset<Texture2D> spark_06;
    public static Asset<Texture2D> spark_07_Black;
    public static Asset<Texture2D> TextureLaser;
    public static Asset<Texture2D> ThinGlowLine;
    public static Asset<Texture2D> ThinnerGlowTrail;
    public static Asset<Texture2D> Trail5Loop;
    public static Asset<Texture2D> Trail7;
    
    //LastPrism
    public static Asset<Texture2D> Simple_Lens_Flare_11;
    public static Asset<Texture2D> flare_16;
    public static Asset<Texture2D> whiteFireEyeA;
    public static Asset<Texture2D> PartiGlow;
    public static Asset<Texture2D> magicCirc;
    public static Asset<Texture2D> Yharim
        ;
    public static Asset<Texture2D> BlackWall;
    
    public static Asset<Texture2D> YharimGrad;
    public static Asset<Texture2D> DarkGrad;
    public static Asset<Texture2D> RainbowGrad1;

    public static void Load()
    {
        if (!ModLoader.TryGetMod("VFXPlus", out _))
            return;

        BlackWall = Req("BlackWall");
        
        Simple_Lens_Flare_11 = Req("Flare/Simple Lens Flare_11");
        flare_16 = Req("Flare/flare_16");
        
        // ===== Orbs =====
        circle_05 = Req("Orbs/circle_05");
        whiteFireEyeA = Req("Orbs/whiteFireEyeA");
        feather_circle128PMA = Req("Orbs/feather_circle128PMA");
        flare_12 = Req("Orbs/flare_12");
        GlowCircleFlare = Req("Orbs/GlowCircleFlare");
        SoftGlow = Req("Orbs/SoftGlow");
        SoftGlow64 = Req("Orbs/SoftGlow64");
        SolidBloom = Req("Orbs/SolidBloom");

        // ===== Pixel =====
        PartiGlow = Req("Pixel/PartiGlow");
        AnotherLineGlow = Req("Pixel/AnotherLineGlow");
        CrispStarPMA = Req("Pixel/CrispStarPMA");
        DiamondGlowPMA = Req("Pixel/DiamondGlowPMA");
        Extra_89 = Req("Pixel/Extra_89");
        Extra_91 = Req("Pixel/Extra_91");
        FireBallBlur = Req("Pixel/FireBallBlur");
        Flare = Req("Pixel/Flare");
        FlareLineHalf = Req("Pixel/FlareLineHalf");
        GlowingFlare = Req("Pixel/GlowingFlare");
        GlowingStar = Req("Pixel/GlowingStar");
        Medusa_Gray = Req("Pixel/Medusa_Gray");
        Nightglow = Req("Pixel/Nightglow");
        PartiGlowPMA = Req("Pixel/PartiGlowPMA");
        PixelSwirl = Req("Pixel/PixelSwirl");
        Projectile_540 = Req("Pixel/Projectile_540");
        RainbowRod = Req("Pixel/RainbowRod");
        Starlight = Req("Pixel/Starlight");
        Twinkle = Req("Pixel/Twinkle");
        SoulSpike = Req("Pixel/SoulSpike");

        // ===== Trails =====
        EnergyTex = Req("Trails/EnergyTex");
        Extra_196_Black = Req("Trails/Extra_196_Black");
        FireTrailGamma = Req("Trails/FireTrailGamma");
        FlamesTextureButBlack = Req("Trails/FlamesTextureButBlack");
        FlameTrail = Req("Trails/FlameTrail");
        FlashLightBeamBlack = Req("Trails/FlashLightBeamBlack");
        GlowTrail = Req("Trails/GlowTrail");
        Laser1 = Req("Trails/Laser1");
        LavaTrailV1 = Req("Trails/LavaTrailV1");
        LintyTrail = Req("Trails/LintyTrail");
        s06sBloom = Req("Trails/s06sBloom");
        spark_06 = Req("Trails/spark_06");
        spark_07_Black = Req("Trails/spark_07_Black");
        TextureLaser = Req("Trails/TextureLaser");
        ThinGlowLine = Req("Trails/ThinGlowLine");
        ThinnerGlowTrail = Req("Trails/ThinnerGlowTrail");
        Trail5Loop = Req("Trails/Trail5Loop");
        Trail7 = Req("Trails/Trail7");
        
        
        RainbowGrad1 = Req("Gradients/RainbowGrad1");
        YharimGrad = ModContent.Request<Texture2D>("CalamityVFXPlus/Assets/Gradient/YharimsCrystalGrad");
        DarkGrad = ModContent.Request<Texture2D>("CalamityVFXPlus/Assets/Gradient/DarkSpark");
        magicCirc = ModContent.Request<Texture2D>("CalamityVFXPlus/Assets/magicCirc");
        Yharim = ModContent.Request<Texture2D>("CalamityVFXPlus/Assets/Yharim");
    }

    private static Asset<Texture2D> Req(string relativePath)
    {
        return ModContent.Request<Texture2D>(
            Base + relativePath,
            AssetRequestMode.ImmediateLoad);
    }

    public static void Unload()
    { 
        Simple_Lens_Flare_11 = null;
        flare_16 = null;
        whiteFireEyeA = null;
        circle_05 = null;
        feather_circle128PMA = null;
        flare_12 = null;
        GlowCircleFlare = null;
        SoftGlow = null;
        SoftGlow64 = null;
        SolidBloom = null;

        AnotherLineGlow = null;
        CrispStarPMA = null;
        DiamondGlowPMA = null;
        Extra_89 = null;
        Extra_91 = null;
        FireBallBlur = null;
        Flare = null;
        FlareLineHalf = null;
        GlowingFlare = null;
        GlowingStar = null;
        Medusa_Gray = null;
        Nightglow = null;
        PartiGlowPMA = null;
        PixelSwirl = null;
        Projectile_540 = null;
        RainbowRod = null;
        Starlight = null;
        Twinkle = null;
        SoulSpike = null;

        EnergyTex = null;
        Extra_196_Black = null;
        FireTrailGamma = null;
        FlamesTextureButBlack = null;
        FlameTrail = null;
        FlashLightBeamBlack = null;
        GlowTrail = null;
        Laser1 = null;
        LavaTrailV1 = null;
        LintyTrail = null;
        s06sBloom = null;
        spark_06 = null;
        spark_07_Black = null;
        TextureLaser = null;
        ThinGlowLine = null;
        ThinnerGlowTrail = null;
        Trail5Loop = null;
        Trail7 = null;
    }
}