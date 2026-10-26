using System.Collections.Generic;
using LearnLanguages.Map.Data;
using UnityEngine;

namespace LearnLanguages.Map.Logic
{
    /// <summary>
    /// Maps every pixel of the provinces image to a province id.
    /// </summary>
    public sealed class ProvinceLookup
    {
        private int[] _pixelProvinces;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public IReadOnlyList<int> PixelProvinces => _pixelProvinces;

        public static int Pack(Color32 c) => (c.r << 16) | (c.g << 8) | c.b;

        public void Build(Texture2D provincesTexture, ProvincesData provinces)
        {
            Width = provincesTexture.width;
            Height = provincesTexture.height;

            var colorToId = new Dictionary<int, int>();
            for (var i = 0; i < provinces.Count; i++) colorToId[Pack(provinces.Get(i).MapColor)] = i;
            foreach (var alias in provinces.ColorAliases) colorToId[Pack(alias.Color)] = alias.Province;

            var pixels = provincesTexture.GetPixels32();
            _pixelProvinces = new int[pixels.Length];
            for (var i = 0; i < pixels.Length; i++)
                _pixelProvinces[i] = colorToId.TryGetValue(Pack(pixels[i]), out var id) ? id : -1;
        }

        public int GetProvince(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return -1;
            return _pixelProvinces[y * Width + x];
        }

        public int GetProvince(Vector2 pixel) => GetProvince(Mathf.FloorToInt(pixel.x), Mathf.FloorToInt(pixel.y));
    }
}
