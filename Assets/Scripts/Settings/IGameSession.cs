using LearnLanguages.Save;

namespace LearnLanguages.Settings
{
    /// <summary>
    /// Carries the chosen settings (or a save to restore) from the main menu into the game scene.
    /// </summary>
    public interface IGameSession
    {
        GameSettings Settings { get; }
        SaveData PendingSave { get; }
        bool HasSettings { get; }
        void StartNew(GameSettings settings);
        void StartFromSave(SaveData save);
        void ConsumePendingSave();
    }

    public sealed class GameSession : IGameSession
    {
        public GameSettings Settings { get; private set; }
        public SaveData PendingSave { get; private set; }
        public bool HasSettings => Settings != null;

        public void StartNew(GameSettings settings)
        {
            Settings = settings;
            PendingSave = null;
        }

        public void StartFromSave(SaveData save)
        {
            Settings = save.Settings;
            PendingSave = save;
        }

        public void ConsumePendingSave() => PendingSave = null;
    }
}
