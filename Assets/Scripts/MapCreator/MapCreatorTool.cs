using UnityEngine;

namespace LearnLanguages.MapCreator
{
    public enum MapPreviewMode
    {
        Landscape = 0,
        Provinces = 1,
        HeightMap = 2
    }

    /// <summary>
    /// Scene component of the MapCreator scene. Shows the edited map on a plane laid out exactly like the game terrain
    /// (x = pixel.x * scale, z = pixel.y * scale). All editing happens in its custom inspector.
    /// </summary>
    [ExecuteAlways]
    public sealed class MapCreatorTool : MonoBehaviour
    {
        [SerializeField] private Map.Data.Map _map;
        [SerializeField] private MeshRenderer _preview;
        [SerializeField] private MapPreviewMode _previewMode;
        [SerializeField] private int _selectedProvince = -1;
        [SerializeField] private bool _showNames = true;
        [SerializeField] private bool _showNeighbours = true;

        public Map.Data.Map Map => _map;
        public MeshRenderer Preview => _preview;
        public MapPreviewMode PreviewMode => _previewMode;
        public int SelectedProvince => _selectedProvince;
        public bool ShowNames => _showNames;
        public bool ShowNeighbours => _showNeighbours;

        public void Select(int province) => _selectedProvince = province;

        public void SetPreviewMode(MapPreviewMode mode)
        {
            _previewMode = mode;
            RefreshPreview();
        }

        public Vector3 PixelToWorld(Vector2 pixel) =>
            transform.position + new Vector3(pixel.x, 0f, pixel.y) * _map.WorldUnitsPerPixel;

        public Vector2 WorldToPixel(Vector3 world)
        {
            var local = world - transform.position;
            return new Vector2(local.x, local.z) / _map.WorldUnitsPerPixel;
        }

        public void RefreshPreview()
        {
            if (_map == null || _preview == null) return;
            var size = _map.WorldSize;
            _preview.transform.localPosition = new Vector3(size.x * 0.5f, 0f, size.y * 0.5f);
            _preview.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _preview.transform.localScale = new Vector3(size.x, size.y, 1f);

            Texture texture;
            switch (_previewMode)
            {
                case MapPreviewMode.Provinces: texture = _map.Provinces; break;
                case MapPreviewMode.HeightMap: texture = _map.HeightMap; break;
                default: texture = _map.VisualLandscape; break;
            }
            if (_preview.sharedMaterial != null) _preview.sharedMaterial.mainTexture = texture;
        }

        private void OnValidate() => RefreshPreview();
    }
}
