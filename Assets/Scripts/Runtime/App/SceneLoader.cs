using UnityEngine.SceneManagement;

namespace Pachimon.App
{
    public static class SceneLoader
    {
        public const string TitleSceneName = "TitleScene";
        public const string GameSceneName = "GameScene";

        public static void LoadTitleScene()
        {
            SceneManager.LoadScene(TitleSceneName);
        }

        public static void LoadGameScene()
        {
            SceneManager.LoadScene(GameSceneName);
        }
    }
}
