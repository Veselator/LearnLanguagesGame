using LearnLanguages.Gameplay.Economy;
using LearnLanguages.Settings;
using UnityEngine;

namespace LearnLanguages.Gameplay.Players
{
    /// <summary>
    /// State of a participant: human, AI, or the neutral side that owns unclaimed provinces.
    /// </summary>
    public sealed class PlayerData
    {
        public const int NeutralId = -1;

        public PlayerData(int id, string name, Color color, bool isHuman, GameDifficulty aiDifficulty, int capitalTileId)
        {
            Id = id;
            Name = name;
            Color = color;
            IsHuman = isHuman;
            AiDifficulty = aiDifficulty;
            CapitalTileId = capitalTileId;
            IsAlive = true;
        }

        public int Id { get; }
        public string Name { get; }
        public Color Color { get; }
        public bool IsHuman { get; }
        public GameDifficulty AiDifficulty { get; }
        public int CapitalTileId { get; }
        public bool IsAlive { get; private set; }
        public ResourceBundle Resources { get; private set; }
        public bool IsNeutral => Id == NeutralId;

        public void SetResources(ResourceBundle resources) => Resources = resources;

        public void AddResources(ResourceBundle amount) => Resources += amount;

        public bool TrySpend(ResourceBundle cost)
        {
            if (!Resources.Covers(cost)) return false;
            Resources -= cost;
            return true;
        }

        public void Eliminate() => IsAlive = false;

        public void SetAlive(bool alive) => IsAlive = alive;
    }
}
