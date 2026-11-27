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
        private bool _hasBeenEntered;

        public string SceneName { get; }

        public SceneState(string sceneName)
        {
            SceneName = sceneName;
        }

        public void Enter()
        {
            // The scene that hosts the GameManager is already loaded when the game starts, so the
            // very first time it is not loaded again. From then on, entering always loads the
            // scene, which is what lets a match be restarted by entering its state again.
            bool alreadyOnScreen = SceneManager.GetActiveScene().name == SceneName;
            if (!_hasBeenEntered && alreadyOnScreen)
            {
                _hasBeenEntered = true;
                return;
            }

            _hasBeenEntered = true;
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
