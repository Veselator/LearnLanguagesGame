using UnityEngine;

namespace LearnLanguages.Settings
{
    public interface IGameOptions
    {
        /// <summary>Developer tools in the game, such as the view of the AI decisions.</summary>
        bool DebugMode { get; set; }
    }

    /// <summary>
    /// Options of the game kept between launches. Debug mode is on by default in the editor, off in builds.
    /// </summary>
    public sealed class GameOptions : IGameOptions
    {
        private const string DebugModeKey = "settings.debug_mode";

        public bool DebugMode
        {
            get => PlayerPrefs.GetInt(DebugModeKey, Application.isEditor ? 1 : 0) == 1;
            set
            {
                PlayerPrefs.SetInt(DebugModeKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
