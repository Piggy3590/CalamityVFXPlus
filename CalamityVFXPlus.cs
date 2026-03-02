using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityVFXPlus;

public class CalamityVFXPlus : Mod
{
    
    internal static CalamityVFXPlus Instance { get; private set; }
    
    public override void Load()
    {
        VFXPlusTextures.Load();
    }

    public override void Unload()
    {
        VFXPlusTextures.Unload();
    }
}