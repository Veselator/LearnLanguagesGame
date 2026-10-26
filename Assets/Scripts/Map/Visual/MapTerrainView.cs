using LearnLanguages.Map.Logic;
using UnityEngine;

namespace LearnLanguages.Map.Visual
{
    /// <summary>
    /// Unity terrain of the global map: heights from the height map, painted texture from <see cref="ProvinceOverlayPainter"/>.
    /// Also converts between map pixels and world positions.
    /// </summary>
    public sealed class MapTerrainView : MonoBehaviour
    {
        [SerializeField] private Terrain _terrain;
        [SerializeField] private TerrainCollider _collider;
        [Tooltip("Template copied at runtime; it must not be modified as an asset.")]
        [SerializeField] private TerrainData _terrainDataTemplate;
        [SerializeField] private TerrainLayer _layerTemplate;
        [SerializeField] private int _heightmapResolution = 1025;

        private Data.Map _map;
        private ProvinceLookup _lookup;
        private Texture2D _paintedTexture;

        public ProvinceOverlayPainter Painter { get; private set; }
        public ProvinceLookup Lookup => _lookup;
        public Vector3 Size => _terrain.terrainData.size;

        public void Build(Data.Map map, ProvinceLookup lookup)
        {
            _map = map;
            _lookup = lookup;
            var worldSize = map.WorldSize;

            var data = Instantiate(_terrainDataTemplate);
            data.heightmapResolution = _heightmapResolution;
            data.size = new Vector3(worldSize.x, map.MaxHeight, worldSize.y);
            data.SetHeights(0, 0, SampleHeights(map.HeightMap, data.heightmapResolution));

            _paintedTexture = new Texture2D(lookup.Width, lookup.Height, TextureFormat.RGBA32, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4
            };
            Painter = new ProvinceOverlayPainter(map.VisualLandscape, lookup, map.ProvincesData.Count, _paintedTexture);

            var layer = Instantiate(_layerTemplate);
            layer.diffuseTexture = _paintedTexture;
            layer.tileSize = worldSize;
            layer.tileOffset = Vector2.zero;
            data.terrainLayers = new[] { layer };

            _terrain.terrainData = data;
            _collider.terrainData = data;
            transform.position = Vector3.zero;
        }

        /// <summary>World position on the terrain surface for a pixel of the map images.</summary>
        public Vector3 PixelToWorld(Vector2 pixel)
        {
            var scale = _map.WorldUnitsPerPixel;
            var position = new Vector3(pixel.x * scale, 0f, pixel.y * scale) + transform.position;
            position.y = _terrain.SampleHeight(position) + transform.position.y;
            return position;
        }

        public Vector2 WorldToPixel(Vector3 world)
        {
            var local = world - transform.position;
            return new Vector2(local.x, local.z) / _map.WorldUnitsPerPixel;
        }

        public bool TryRaycastProvince(Ray ray, out int provinceId, out Vector3 point)
        {
            provinceId = -1;
            point = default;
            if (_collider == null || !_collider.Raycast(ray, out var hit, 5000f)) return false;
            point = hit.point;
            provinceId = _lookup.GetProvince(WorldToPixel(hit.point));
            return provinceId >= 0;
        }

        private static float[,] SampleHeights(Texture2D heightMap, int resolution)
        {
            var heights = new float[resolution, resolution];
            var step = 1f / (resolution - 1);
            for (var y = 0; y < resolution; y++)
            for (var x = 0; x < resolution; x++)
                heights[y, x] = heightMap.GetPixelBilinear(x * step, y * step).grayscale;
            return heights;
        }

        private void OnDestroy()
        {
            if (_paintedTexture != null) Destroy(_paintedTexture);
        }
    }
}
