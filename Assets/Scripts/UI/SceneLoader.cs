using UnityEngine.SceneManagement;

namespace LearnLanguages.UI
{
    public interface ISceneLoader
    {
        void LoadMainMenu();
        void LoadGame();
        void Quit();
    }

    public sealed class SceneLoader : ISceneLoader
    {
        public const string MainMenuScene = "MainMenu";
        public const string GameScene = "Game";

        public void LoadMainMenu() => SceneManager.LoadScene(MainMenuScene);

        public void LoadGame() => SceneManager.LoadScene(GameScene);

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}
