using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace CalamityVFXPlus.Common.Systems
{
    public struct VividBeamVisualData
    {
        public Vector2 Start;
        public Vector2 End;
        public float Seed;
        public float BaseOuterWidth;
        public float BaseCoreWidth;
        public int MaxLifetime;
    }

    public static class VividBeamVisualStorage
    {
        public static readonly Dictionary<int, VividBeamVisualData> Data = new();
    }
}