using System;
using UnityEngine;

namespace LearnLanguages.Settings
{
    /// <summary>
    /// Settings chosen for one game session (match).
    /// </summary>
    [Serializable]
    public sealed class GameSettings
    {
        [SerializeField] private string _mapId;
        [SerializeField] private Language _targetLanguage;
        [SerializeField] private Language _interfaceLanguage;
        [SerializeField] private LanguageLevel _languageLevel;
        [SerializeField] private GameDifficulty _difficulty;
        [SerializeField] private int _aiPlayersCount;
        [SerializeField] private int _seed;

        // Used by the serializer.
        private GameSettings() { }

        public GameSettings(string mapId, Language targetLanguage, Language interfaceLanguage,
            LanguageLevel languageLevel, GameDifficulty difficulty, int aiPlayersCount, int seed)
        {
            if (targetLanguage == interfaceLanguage)
                throw new ArgumentException("Interface language must differ from the trained language.");

            _mapId = mapId;
            _targetLanguage = targetLanguage;
            _interfaceLanguage = interfaceLanguage;
            _languageLevel = languageLevel;
            _difficulty = difficulty;
            _aiPlayersCount = aiPlayersCount;
            _seed = seed;
        }

        public string MapId => _mapId;
        public Language TargetLanguage => _targetLanguage;
        public Language InterfaceLanguage => _interfaceLanguage;
        public LanguageLevel LanguageLevel => _languageLevel;
        public GameDifficulty Difficulty => _difficulty;
        public int AiPlayersCount => _aiPlayersCount;
        public int Seed => _seed;
    }
}
