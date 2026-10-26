using System.Collections.Generic;
using LearnLanguages.Map.Logic;
using UnityEngine;

namespace LearnLanguages.Map.Visual
{
    public enum ProvinceHighlight
    {
        None = 0,
        Selected = 1,
        MoveTarget = 2
    }

    /// <summary>
    /// Bakes the visible map texture: landscape tinted by owner colors, province borders and highlights.
    /// Only the pixels of changed provinces are repainted.
    /// </summary>
    public sealed class ProvinceOverlayPainter
    {
        private const float OwnerTintStrength = 0.38f;
        private const float BorderDarkening = 0.55f;

        private readonly Color32[] _landscape;
        private readonly Color32[] _output;
        private readonly bool[] _border;
        private readonly int[][] _provincePixels;
        private readonly Color[] _tints;
        private readonly bool[] _hasTint;
        private readonly ProvinceHighlight[] _highlights;

        public ProvinceOverlayPainter(Texture2D landscape, ProvinceLookup lookup, int provincesCount, Texture2D target)
        {
            Texture = target;
            _landscape = ResampleLandscape(landscape, lookup.Width, lookup.Height);
            _output = new Color32[_landscape.Length];
            _border = new bool[_landscape.Length];
            _tints = new Color[provincesCount];
            _hasTint = new bool[provincesCount];
            _highlights = new ProvinceHighlight[provincesCount];

            var pixels = lookup.PixelProvinces;
            var width = lookup.Width;
            var height = lookup.Height;
            var counts = new int[provincesCount];
            for (var i = 0; i < pixels.Count; i++)
            {
                var id = pixels[i];
                if (id >= 0) counts[id]++;
                var x = i % width;
                var y = i / width;
                _border[i] = (x + 1 < width && pixels[i + 1] != id) || (y + 1 < height && pixels[i + width] != id) ||
                             (x > 0 && pixels[i - 1] != id) || (y > 0 && pixels[i - width] != id);
            }

            _provincePixels = new int[provincesCount][];
            var fill = new int[provincesCount];
            for (var p = 0; p < provincesCount; p++) _provincePixels[p] = new int[counts[p]];
            for (var i = 0; i < pixels.Count; i++)
            {
                var id = pixels[i];
                if (id >= 0) _provincePixels[id][fill[id]++] = i;
            }

            for (var i = 0; i < _landscape.Length; i++)
                _output[i] = _border[i] ? Darken(_landscape[i]) : _landscape[i];
            Apply();
        }

        public Texture2D Texture { get; }

        public void SetTint(int province, Color color, bool hasTint)
        {
            _tints[province] = color;
            _hasTint[province] = hasTint;
            Repaint(province);
        }

        public void SetHighlight(int province, ProvinceHighlight highlight)
        {
            if (province < 0 || _highlights[province] == highlight) return;
            _highlights[province] = highlight;
            Repaint(province);
        }

        public void Apply()
        {
            Texture.SetPixels32(_output);
            Texture.Apply(true);
        }

        private void Repaint(int province)
        {
            var tint = _tints[province];
            var tinted = _hasTint[province];
            var highlight = _highlights[province];
            foreach (var i in _provincePixels[province])
            {
                Color color = _landscape[i];
                if (tinted) color = Color.Lerp(color, tint, OwnerTintStrength);
                switch (highlight)
                {
                    case ProvinceHighlight.Selected:
                        color = Color.Lerp(color, Color.white, 0.35f);
                        break;
                    case ProvinceHighlight.MoveTarget:
                        color = Color.Lerp(color, new Color(1f, 0.85f, 0.2f), 0.3f);
                        break;
                }
                if (_border[i]) color = tinted ? Color.Lerp(tint, Color.black, 0.35f) : color * BorderDarkening;
                color.a = 1f;
                _output[i] = color;
            }
        }

        private static Color32 Darken(Color32 color) => (Color)color * BorderDarkening;

        private static Color32[] ResampleLandscape(Texture2D landscape, int width, int height)
        {
            if (landscape.width == width && landscape.height == height) return landscape.GetPixels32();
            var result = new Color32[width * height];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                result[y * width + x] = landscape.GetPixelBilinear((x + 0.5f) / width, (y + 0.5f) / height);
            return result;
        }
    }
}
