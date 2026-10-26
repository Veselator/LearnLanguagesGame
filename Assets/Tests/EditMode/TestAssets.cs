using LearnLanguages.Battle.Data;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Learning.Data;
using LearnLanguages.Localization;
using UnityEditor;

namespace LearnLanguages.Tests
{
    /// <summary>
    /// Project configuration assets used by the tests.
    /// </summary>
    internal static class TestAssets
    {
        public static LanguageConfig LanguageConfig => Load<LanguageConfig>("Assets/Data/Language/LanguageConfig.asset");
        public static LocalizationTable Localization => Load<LocalizationTable>("Assets/Data/Localization/LocalizationTable.asset");
        public static UnitsConfig Units => Load<UnitsConfig>("Assets/Data/Units/UnitsConfig.asset");
        public static BattleConfig Battle => Load<BattleConfig>("Assets/Data/Battle/BattleConfig.asset");
        public static PlayersConfig Players => Load<PlayersConfig>("Assets/Data/PlayersConfig.asset");
        public static BattlefieldMapsConfig Battlefields => Load<BattlefieldMapsConfig>("Assets/Data/Battle/BattlefieldMapsConfig.asset");

        private static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);
    }
}
