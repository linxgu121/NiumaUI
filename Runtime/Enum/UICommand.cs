namespace NiumaUI.Enum
{
    /// <summary>
    /// UI 系统全局输入命令
    /// 与对话无关，由 UIInputPipeline 或 EventSystem 生成
    /// </summary>
    public enum UICommand
    {
        Confirm,        // 确认/点击（UI 导航用）
        Back,           // 返回/关闭
        NavigateUp,     // 导航向上
        NavigateDown,   // 导航向下
        NavigateLeft,   // 导航向左
        NavigateRight,  // 导航向右
        OpenMenu,       // 打开菜单（Esc 等）
        HideUI,         // 隐藏/显示游戏 UI
        Save,           // 存档
        Load,           // 读档
        OpenLog         // 历史记录
    }
}
