using System;
using System.Collections.Generic;
using LearnLanguages.Gameplay.Economy;
using LearnLanguages.Settings;
using UnityEngine;

namespace LearnLanguages.Save
{
    /// <summary>
    /// Plain serializable snapshot of a match (JSON).
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public string SavedAt;
        public GameSettings Settings;
        public int Round;
        public int CurrentPlayerIndex;
        public int NextArmyId;
        public int TasksAnswered;
        public int TasksCorrect;
        public List<PlayerSave> Players = new List<PlayerSave>();
        public List<TileSave> Tiles = new List<TileSave>();
        public List<ArmySave> Armies = new List<ArmySave>();
    }

    [Serializable]
    public sealed class PlayerSave
    {
        public int Id;
        public string Name;
        public Color Color;
        public bool IsHuman;
        public GameDifficulty Difficulty;
        public int CapitalTileId;
        public bool IsAlive;
        public ResourceBundle Resources;
    }

    [Serializable]
    public sealed class TileSave
    {
        public int Id;
        public int OwnerId;
        public int BattlefieldIndex;
        public List<string> Upgrades = new List<string>();
    }

    [Serializable]
    public sealed class ArmySave
    {
        public int Id;
        public int OwnerId;
        public int TileId;
        public bool HasMoved;
        public List<CohortSave> Cohorts = new List<CohortSave>();
    }

    [Serializable]
    public sealed class CohortSave
    {
        public string UnitId;
        public int MaxSoldiers;
        public int Healthy;
        public int Wounded;
        public float Organization;
    }
}
