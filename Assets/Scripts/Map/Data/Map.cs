using LearnLanguages.Battle.Data;
using UnityEngine;

namespace LearnLanguages.Map.Data
{
    [CreateAssetMenu(menuName = "LearnLanguages/Map/Map", fileName = "Map")]
    public sealed class Map : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _name;
        [Tooltip("Generated from the visual landscape in the MapCreator scene.")]
        [SerializeField] private Texture2D _icon;
        [SerializeField] private Texture2D _visualLandscape;
        [SerializeField] private Texture2D _provinces;
        [SerializeField] private Texture2D _heightMap;
        [SerializeField] private ProvincesData _provincesData;
        [SerializeField] private BattlefieldMapsConfig _battlefieldMapsConfig;
        [Tooltip("World units per map pixel.")]
        [SerializeField] private float _worldUnitsPerPixel = 0.1f;
        [SerializeField] private float _maxHeight = 16f;
        [Tooltip("Normalized height of the water surface. Terrain below it is a flat seabed.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float _waterLevel = 0.012f;
        [Tooltip("Mirrored maps get twin provinces with identical gameplay data and paired spawn points.")]
        [SerializeField] private MapSymmetry _symmetry;

        public string Id => _id;
        public string Name => _name;
        public Texture2D Icon => _icon;
        public Texture2D VisualLandscape => _visualLandscape;
        public Texture2D Provinces => _provinces;
        public Texture2D HeightMap => _heightMap;
        public ProvincesData ProvincesData => _provincesData;
        public BattlefieldMapsConfig BattlefieldMapsConfig => _battlefieldMapsConfig;
        public float WorldUnitsPerPixel => _worldUnitsPerPixel;
        public float MaxHeight => _maxHeight;
        public float WaterLevel => _waterLevel;
        public float WaterHeight => _waterLevel * _maxHeight;
        public MapSymmetry Symmetry => _symmetry;
        public Vector2Int PixelSize => _provinces != null ? new Vector2Int(_provinces.width, _provinces.height) : Vector2Int.one;
        public Vector2 WorldSize => (Vector2)PixelSize * _worldUnitsPerPixel;

        public void SetIcon(Texture2D icon) => _icon = icon;
    }
}
