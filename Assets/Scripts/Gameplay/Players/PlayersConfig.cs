using System;
using UnityEngine;

namespace LearnLanguages.Gameplay.Players
{
    [CreateAssetMenu(menuName = "LearnLanguages/Players Config", fileName = "PlayersConfig")]
    public sealed class PlayersConfig : ScriptableObject
    {
        [SerializeField] private Color[] _playerColors =
        {
            new Color(0.15f, 0.35f, 0.95f), new Color(0.85f, 0.15f, 0.15f), new Color(0.6f, 0.2f, 0.8f), new Color(0.95f, 0.5f, 0.1f)
        };

        [SerializeField] private string[] _aiNameKeys = { "player.ai_1", "player.ai_2", "player.ai_3" };
        [SerializeField] private Color _neutralColor = new Color(0.55f, 0.55f, 0.55f);
        [Tooltip("Probability for an AI cohort to win a language duel, by AI difficulty (Easy, Medium, Hard).")]
        [SerializeField] private float[] _aiDuelSuccess = { 0.4f, 0.55f, 0.7f };
        [SerializeField] private int _maxAiPlayers = 3;

        public Color NeutralColor => _neutralColor;
        public int MaxAiPlayers => _maxAiPlayers;

        public Color GetColor(int playerIndex) => _playerColors[playerIndex % _playerColors.Length];

        public string GetAiNameKey(int aiIndex) =>
            _aiNameKeys.Length == 0 ? "player.ai_1" : _aiNameKeys[aiIndex % _aiNameKeys.Length];

        public float GetAiDuelSuccess(Settings.GameDifficulty difficulty) =>
            _aiDuelSuccess[Math.Min((int)difficulty, _aiDuelSuccess.Length - 1)];
    }
}
