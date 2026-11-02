using UnityEngine;

namespace LearnLanguages.Map.Visual
{
    /// <summary>
    /// Water plane of the global map at the map's water level. Bakes a small coast proximity texture from the
    /// height map so the water shader can draw shallows and surf along the shore.
    /// </summary>
    public sealed class MapWaterView : MonoBehaviour
    {
        private static readonly int CoastMapId = Shader.PropertyToID("_CoastMap");
        private static readonly int MapRectId = Shader.PropertyToID("_MapRect");

        [SerializeField] private MeshRenderer _renderer;
        [Tooltip("Width of the baked coast proximity texture; its height follows the map aspect.")]
        [SerializeField] private int _coastResolution = 512;
        [Tooltip("How far from the shore the shallows reach, in coast texture texels.")]
        [SerializeField] private int _shallowRadius = 7;
        [Tooltip("Size of the water plane, in world units: it reaches well past the map edges.")]
        [SerializeField] private float _planeSize = 3000f;

        private Texture2D _coastMap;
        private MaterialPropertyBlock _properties;

        public void Build(Data.Map map, Vector3 mapOrigin)
        {
            var worldSize = map.WorldSize;
            var center = mapOrigin + new Vector3(worldSize.x * 0.5f, map.WaterHeight, worldSize.y * 0.5f);
            transform.SetPositionAndRotation(center, Quaternion.Euler(90f, 0f, 0f));
            transform.localScale = new Vector3(_planeSize, _planeSize, 1f);

            if (_coastMap != null) Destroy(_coastMap);
            _coastMap = BakeCoastMap(map);

            _properties ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_properties);
            _properties.SetTexture(CoastMapId, _coastMap);
            _properties.SetVector(MapRectId, new Vector4(mapOrigin.x, mapOrigin.z, worldSize.x, worldSize.y));
            _renderer.SetPropertyBlock(_properties);
        }

        /// <summary>1 on land and the shoreline, fading to 0 in the open sea.</summary>
        private Texture2D BakeCoastMap(Data.Map map)
        {
            var heightMap = map.HeightMap;
            var width = Mathf.Max(16, _coastResolution);
            var height = Mathf.Max(16, Mathf.RoundToInt(width * (float)heightMap.height / heightMap.width));
            var values = new float[width * height];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var sample = heightMap.GetPixelBilinear((x + 0.5f) / width, (y + 0.5f) / height).r;
                values[y * width + x] = sample > map.WaterLevel ? 1f : 0f;
            }

            // Three box blurs approximate a gaussian falloff from the shore.
            var buffer = new float[values.Length];
            for (var pass = 0; pass < 3; pass++)
            {
                BoxBlur(values, buffer, width, height, _shallowRadius, 1, width);
                BoxBlur(buffer, values, height, width, _shallowRadius, width, 1);
            }

            var pixels = new Color32[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                var v = (byte)Mathf.RoundToInt(Mathf.Clamp01(values[i] * 2f) * 255f);
                pixels[i] = new Color32(v, v, v, 255);
            }
            var texture = new Texture2D(width, height, TextureFormat.R8, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "CoastMap"
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        /// <summary>Running-sum blur along one axis: <paramref name="step"/> walks a line, <paramref name="lineStride"/> jumps between lines.</summary>
        private static void BoxBlur(float[] source, float[] target, int length, int lines, int radius, int step, int lineStride)
        {
            var window = radius * 2 + 1;
            for (var line = 0; line < lines; line++)
            {
                var start = line * lineStride;
                var sum = 0f;
                for (var i = -radius; i <= radius; i++) sum += source[start + Mathf.Clamp(i, 0, length - 1) * step];
                for (var i = 0; i < length; i++)
                {
                    target[start + i * step] = sum / window;
                    sum += source[start + Mathf.Min(i + radius + 1, length - 1) * step];
                    sum -= source[start + Mathf.Max(i - radius, 0) * step];
                }
            }
        }

        private void OnDestroy()
        {
            if (_coastMap != null) Destroy(_coastMap);
        }
    }
}
