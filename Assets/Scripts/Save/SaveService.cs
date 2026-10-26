using System;
using System.IO;
using UnityEngine;

namespace LearnLanguages.Save
{
    public interface ISaveService
    {
        bool HasSave { get; }
        void Save(SaveData data);
        SaveData Load();
        void Delete();
    }

    public sealed class FileSaveService : ISaveService
    {
        private const string FileName = "savegame.json";

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public bool HasSave => File.Exists(FilePath);

        public void Save(SaveData data)
        {
            data.SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            var temporary = FilePath + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(data));
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(temporary, FilePath);
        }

        public SaveData Load()
        {
            if (!HasSave) return null;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                return data != null && data.Version == SaveData.CurrentVersion && data.Settings != null ? data : null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Save file is corrupted: {exception.Message}");
                return null;
            }
        }

        public void Delete()
        {
            if (HasSave) File.Delete(FilePath);
        }
    }
}
