using NiumaUI.Core.Interface;
using NiumaUI.Enum;
using NiumaUI.State.Base;

namespace NiumaUI.State.UIStates
{
     public class MenuUIState : UIStateBase
    {
        private readonly IGameplayInputBlocker _inputBlocker;

        public MenuUIState(IGameplayInputBlocker inputBlocker)
        {
            _inputBlocker = inputBlocker;
        }

        public override void Enter()
        {
            _inputBlocker?.SetBlocked(true, UIMode.Menu);
        }

        public override void LogicUpdate() { }
        public override void Exit() { }
    }
}
