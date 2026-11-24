using System;
using System.Collections.Generic;

namespace RockyLeague.Core.StateMachines
{
    /// <summary>
    /// Generic, reusable finite state machine (State pattern).
    /// Shared by the game flow, the match flow, the car and the AI, so the
    /// transition logic is written only once (DRY).
    ///
    /// Transitions are predicates (<see cref="Func{TResult}"/>) registered from outside,
    /// which keeps states decoupled from each other.
    /// It is a plain C# class (no MonoBehaviour): the owner decides whether to tick it
    /// from Update or FixedUpdate.
    /// </summary>
    public sealed class StateMachine
    {
        private readonly Dictionary<IState, List<Transition>> _transitions = new Dictionary<IState, List<Transition>>();
        private readonly List<Transition> _anyStateTransitions = new List<Transition>();

        public IState CurrentState { get; private set; }

        /// <summary>Raised after a state change with (previous, next). Previous is null on the first change.</summary>
        public event Action<IState, IState> StateChanged;

        public void AddTransition(IState from, IState to, Func<bool> condition)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));
            if (condition == null) throw new ArgumentNullException(nameof(condition));

            if (!_transitions.TryGetValue(from, out List<Transition> list))
            {
                list = new List<Transition>();
                _transitions.Add(from, list);
            }

            list.Add(new Transition(to, condition));
        }

        /// <summary>Transition that can fire from any state (e.g. "goal scored" interrupts everything).</summary>
        public void AddAnyStateTransition(IState to, Func<bool> condition)
        {
            if (to == null) throw new ArgumentNullException(nameof(to));
            if (condition == null) throw new ArgumentNullException(nameof(condition));

            _anyStateTransitions.Add(new Transition(to, condition));
        }

        public void ChangeState(IState next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            if (next == CurrentState) return;

            IState previous = CurrentState;
            previous?.Exit();
            CurrentState = next;
            CurrentState.Enter();

            StateChanged?.Invoke(previous, CurrentState);
        }

        /// <summary>Evaluates transitions and then ticks the current state.</summary>
        public void Tick(float deltaTime)
        {
            IState next = FindTriggeredTransition();
            if (next != null)
            {
                ChangeState(next);
            }

            CurrentState?.Tick(deltaTime);
        }

        // Plain loops instead of LINQ on purpose: this runs every frame and
        // LINQ with closures allocates garbage on each call.
        private IState FindTriggeredTransition()
        {
            foreach (Transition transition in _anyStateTransitions)
            {
                if (transition.To != CurrentState && transition.Condition())
                {
                    return transition.To;
                }
            }

            if (CurrentState != null && _transitions.TryGetValue(CurrentState, out List<Transition> fromCurrent))
            {
                foreach (Transition transition in fromCurrent)
                {
                    if (transition.Condition())
                    {
                        return transition.To;
                    }
                }
            }

            return null;
        }

        private readonly struct Transition
        {
            public IState To { get; }
            public Func<bool> Condition { get; }

            public Transition(IState to, Func<bool> condition)
            {
                To = to;
                Condition = condition;
            }
        }
    }
}
