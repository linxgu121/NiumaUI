namespace NiumaUI.Enum
{
    public enum UIMode
    {
        /// <summary>
        /// 自由玩法：WASD移动、鼠标视角、交互键可用、HUD显示
        /// </summary>
        Gameplay,

       /// <summary>
       /// 对话模式：完全阻塞游戏操作（禁止移动/视角/交互）
       /// 仅对话输入有效（Advance/Skip/Auto/Menu）
       /// </summary>
       Dialogue,

       /// <summary>
       /// 菜单模式：完全阻塞游戏操作，游戏逻辑暂停（Time.timeScale=0可选）
       /// 仅菜单导航有效
       /// </summary>
       Menu,

       /// <summary>
       /// 剧情演出：完全阻塞游戏操作，隐藏HUD，仅保留字幕/黑边
       /// </summary>
       Cinematic
    }
}
