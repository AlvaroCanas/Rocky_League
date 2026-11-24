using RockyLeague.Core.StateMachines;
using UnityEngine;

namespace RockyLeague.Core.Game
{
    /// <summary>
    /// Application-level flow: switches between the main menu, a match and the
    /// training mode. It owns a <see cref="StateMachine"/> (State pattern) where each
    /// state is a scene.
    ///
    /// It is a Singleton on purpose: it is the one object that must survive scene
    /// changes and be reachable from any of them. To keep it from becoming a God Object
    /// it only decides WHICH scene is active; what happens inside a match belongs
    /// to a separate match state machine.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        // Scene names must be in File > Build Profiles > Scene List.
        [SerializeField] private string _mainMenuScene = "MainMenu";
        [SerializeField] private string _matchScene = "Match";
        [SerializeField] private string _trainingScene = "Training";

        private StateMachine _stateMachine;
        private SceneState _mainMenuState;
        private SceneState _matchState;
        private SceneState _trainingState;

        public static GameManager Instance { get; private set; }

        public void GoToMainMenu() => GoTo(_mainMenuState);

        public void StartMatch() => GoTo(_matchState);

        public void StartTraining() => GoTo(_trainingState);

        private void Awake()
        {
            // The manager lives in the first scene; when we come back to that scene
            // a second copy is created and must be discarded.
            if (Instance != null && Instance != this)
            {
                enabled = false;
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _mainMenuState = new SceneState(_mainMenuScene);
            _matchState = new SceneState(_matchScene);
            _trainingState = new SceneState(_trainingScene);

            _stateMachine = new StateMachine();
        }

        private void Start()
        {
            _stateMachine.ChangeState(_mainMenuState);
        }

        private void Update()
        {
            _stateMachine.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void GoTo(SceneState state)
        {
            if (!Application.CanStreamedLevelBeLoaded(state.SceneName))
            {
                Debug.LogError($"Scene '{state.SceneName}' is not in the build settings.", this);
                return;
            }

            _stateMachine.ChangeState(state);
        }
    }
}
