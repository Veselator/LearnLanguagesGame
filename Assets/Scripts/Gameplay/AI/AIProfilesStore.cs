using System;
using System.Collections.Generic;
using System.IO;
using LearnLanguages.Core.Randomness;
using LearnLanguages.Settings;
using UnityEngine;

namespace LearnLanguages.Gameplay.AI
{
    /// <summary>
    /// Where the edited models are kept.
    /// </summary>
    public interface IAIProfilesStorage
    {
        /// <summary>Null when nothing has been saved yet.</summary>
        string Read();
        void Write(string json);
    }

    public sealed class FileAIProfilesStorage : IAIProfilesStorage
    {
        private const string FileName = "ai_profiles.json";

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public string Read() => File.Exists(FilePath) ? File.ReadAllText(FilePath) : null;

        public void Write(string json)
        {
            var temporary = FilePath + ".tmp";
            File.WriteAllText(temporary, json);
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(temporary, FilePath);
        }
    }

    public interface IAIProfilesStore
    {
        IReadOnlyList<AIProfile> Profiles { get; }
        AIProfile GetById(string id);
        /// <summary>A random model of the difficulty; the closest one when the difficulty has none.</summary>
        AIProfile Pick(GameDifficulty difficulty, IRandomService random);
        AIProfile Add(string displayName);
        AIProfile Duplicate(AIProfile profile, string displayName);
        /// <summary>Applies a change to a model and saves it right away.</summary>
        void Edit(AIProfile profile, Action<AIProfile> change);
    }

    /// <summary>
    /// All AI models available to the game: the shipped ones plus what the player changed and added.
    /// Every change is written to the storage immediately.
    /// </summary>
    public sealed class AIProfilesStore : IAIProfilesStore
    {
        private const string NewProfileId = "custom";

        private readonly List<AIProfile> _profiles = new List<AIProfile>();
        private readonly IAIProfilesStorage _storage;

        public AIProfilesStore(AIProfilesConfig config, IAIProfilesStorage storage)
        {
            _storage = storage;
            LoadSaved();
            // Models that came with an update of the game join the saved ones.
            foreach (var shipped in config.Defaults)
                if (GetById(shipped.Id) == null) _profiles.Add(shipped.Clone());
        }

        public IReadOnlyList<AIProfile> Profiles => _profiles;

        public AIProfile GetById(string id)
        {
            foreach (var profile in _profiles)
                if (profile.Id == id) return profile;
            return null;
        }

        public AIProfile Pick(GameDifficulty difficulty, IRandomService random)
        {
            var matching = new List<AIProfile>();
            var closest = int.MaxValue;
            foreach (var profile in _profiles)
            {
                var distance = Math.Abs((int)profile.Difficulty - (int)difficulty);
                if (distance > closest) continue;
                if (distance < closest) matching.Clear();
                closest = distance;
                matching.Add(profile);
            }
            return matching.Count > 0 ? matching[random.Range(0, matching.Count)] : null;
        }

        public AIProfile Add(string displayName) => Register(new AIProfile(), displayName);

        public AIProfile Duplicate(AIProfile profile, string displayName) => Register(profile.Clone(), displayName);

        public void Edit(AIProfile profile, Action<AIProfile> change)
        {
            change(profile);
            Save();
        }

        private AIProfile Register(AIProfile profile, string displayName)
        {
            profile.SetId(GenerateId());
            profile.SetDisplayName(displayName);
            _profiles.Add(profile);
            Save();
            return profile;
        }

        private string GenerateId()
        {
            var number = _profiles.Count + 1;
            while (GetById(NewProfileId + number) != null) number++;
            return NewProfileId + number;
        }

        private void LoadSaved()
        {
            var json = _storage.Read();
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var saved = JsonUtility.FromJson<SavedProfiles>(json);
                if (saved?.Profiles == null) return;
                foreach (var profile in saved.Profiles)
                {
                    profile.EnsureValid();
                    _profiles.Add(profile);
                }
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning($"AI models file is corrupted: {exception.Message}");
            }
        }

        private void Save() => _storage.Write(JsonUtility.ToJson(new SavedProfiles { Profiles = _profiles }, true));

        [Serializable]
        private sealed class SavedProfiles
        {
            public List<AIProfile> Profiles;
        }
    }
}
