using NiumaUI.Core.Interface;
using NiumaUI.Enum;
using NiumaUI.State.Base;

namespace NiumaUI.State.UIStates
{
     public class GameplayUIState : UIStateBase
    {
        private readonly IGameplayInputBlocker _inputBlocker;

        public GameplayUIState(IGameplayInputBlocker inputBlocker)
        {
            _inputBlocker = inputBlocker;
        }

        public override void Enter()
        {
            _inputBlocker?.SetBlocked(false, UIMode.Gameplay);
        }

        public override void LogicUpdate() { }
        public override void Exit() { }
    }
}