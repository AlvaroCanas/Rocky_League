using RockyLeague.Core.StateMachines;
using UnityEngine.SceneManagement;

namespace RockyLeague.Core.Game
{
    /// <summary>
    /// Application-level state whose only job is to have a given scene loaded
    /// (main menu, match, training). The same class is reused for every top-level
    /// state while none of them needs extra behaviour (KISS / YAGNI): when the match
    /// state needs to receive a match configuration, it gets its own class.
    /// </summary>
    public sealed class SceneState : IState
    {
        public string SceneName { get; }

        public SceneState(string sceneName)
        {
            SceneName = sceneName;
        }

        public void Enter()
        {
            // The scene that hosts the GameManager is already loaded on startup.
            if (SceneManager.GetActiveScene().name == SceneName)
            {
                return;
            }

            SceneManager.LoadSceneAsync(SceneName);
        }

        public void Tick(float deltaTime)
        {
        }

        public void Exit()
        {
        }
    }
}
