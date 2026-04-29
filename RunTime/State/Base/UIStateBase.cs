namespace NiumaUI.State.Base
{
     /// <summary>
    /// UI 状态抽象基类 —— 纯契约，零依赖
    /// </summary>
    public abstract class UIStateBase
    {
        public UIStateMachine Owner { get; set; }

        public abstract void Enter();
        public abstract void LogicUpdate();
        public abstract void Exit();
    }
}
