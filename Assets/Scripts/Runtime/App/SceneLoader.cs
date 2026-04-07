using UnityEngine.SceneManagement;

namespace Pachimon.App
{
    public static class SceneLoader
    {
        public const string TopSceneName = "TopScene";
        public const string GameSceneName = "GameScene";

        public static void LoadTopScene()
        {
            SceneManager.LoadScene(TopSceneName);
        }

        public static void LoadGameScene()
        {
            SceneManager.LoadScene(GameSceneName);
        }
    }
}
