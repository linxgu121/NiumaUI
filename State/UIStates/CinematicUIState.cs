using System;
using NiumaUI.Core.Interface;
using NiumaUI.Enum;
using NiumaUI.State.Base;

namespace NiumaUI.State.UIStates
{
    public class CinematicUIState : UIStateBase
    {
        private readonly IGameplayInputBlocker _inputBlocker;
        private readonly Action _closeAllGameViews;

        public CinematicUIState(IGameplayInputBlocker inputBlocker, Action closeAllGameViews)
        {
            _inputBlocker = inputBlocker;
            _closeAllGameViews = closeAllGameViews;
        }

        public override void Enter()
        {
            _inputBlocker?.SetBlocked(true, UIMode.Cinematic);
            _closeAllGameViews?.Invoke();
        }

        public override void LogicUpdate() { }
        public override void Exit() { }
    }
}
