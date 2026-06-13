using NiumaUI.State.Base;

namespace NiumaUI.State
{
     public class UIStateMachine
    {
        public UIStateBase CurrentState { get; private set; }

        public void Initialize(UIStateBase startingState)
        {
            CurrentState = startingState;
            CurrentState.Owner = this;
            CurrentState.Enter();
        }

        public void ChangeState(UIStateBase newState)
        {
            CurrentState?.Exit();
            CurrentState = newState;
            CurrentState.Owner = this;
            CurrentState.Enter();
        }
    }
}
