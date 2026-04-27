using NiumaUI.Core.Interface;
using NiumaUI.Enum;
using NiumaUI.State.Base;

namespace NiumaUI.State.UIStates
{
    public class DialogueUIState : UIStateBase
    {
        private readonly IGameplayInputBlocker _inputBlocker;

        public DialogueUIState(IGameplayInputBlocker inputBlocker)
        {
            _inputBlocker = inputBlocker;
        }

        public override void Enter()
        {
            _inputBlocker?.SetBlocked(true, UIMode.Dialogue);
        }

        public override void LogicUpdate() { }
        public override void Exit() { }
    }
}
