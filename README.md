# NiumaUI

## 模块定位
NiumaUI 是通用 UI 管理与 View 层模块，负责窗口生命周期、Binding 代理、基础视图组件和模块 UI 桥接的承载。它不直接理解任务、背包、剧情等业务规则。

## 框架设计思路
- ViewBase 使用纯 C# 类表达 UI 逻辑，避免直接依赖 GameObject 生命周期。
- Unity 组件细节下沉到 Binding，View 只通过 Binding 调用显示、刷新和关闭。
- UIManager 负责窗口打开、关闭、缓存和统一入口。
- 各业务模块通过自己的 UIViewBridge 转换 ViewData，再推给具体 UI Receiver。

## 核心流程
1. 业务模块 Service 改变运行时数据并递增 Revision。
2. UIViewBridge 检测 Revision，构建表现数据 ViewData。
3. UIManager 打开或取得对应 View。
4. View 将数据传给 Binding。
5. Binding 操作 Text、Button、Image、Panel 等 Unity UI 对象。

## 模块用法
- 新 UI 窗口优先创建 ViewBase + Binding，而不是让业务模块直接持有 UI GameObject。
- Button 回调只调用桥接层或 Controller 暴露的命令接口，不直接改 Service 内部状态。
- 需要自定义预制体时，把 GameObject 绑定给 Binding 字段即可。
- 对话选项按钮由 `DialogueWindowBinding.choiceSlots` 显式绑定，不再自动创建保底按钮。

## 场景使用方法
推荐放置方式：`UIRoot` 一个 UI 根物体管理 Canvas、EventSystem、UIManager 和窗口预制体。

- `UIRoot`：挂 `Canvas`、`GraphicRaycaster`，建议作为全局或当前场景唯一 UI 根。
- `UIRoot/EventSystem`：放 Unity `EventSystem`。按钮无响应时先检查它是否存在。
- `UIRoot/UIManager`：挂 `UIManager` 和 `DefaultViewFactory`，统一打开、关闭和创建 View。
- `UIRoot/Windows`：放对话框、背包、任务、商店等窗口预制体实例或模板。
- `UIRoot/PromptLayer`：放交互提示、Toast、Loading 等轻量面板。
- 各业务模块的 `XXXUIViewBridge` 可以挂在对应模块根物体，也可以统一放在 `UIRoot/Bridges`，但不要把业务 Service 直接挂到 UI 按钮上。
- 每个窗口建议一个 Binding 组件管理 Text/Button/Image 引用；ViewBase 保持纯 C# 逻辑。

### 对话框场景绑定
推荐层级：

```text
UIRoot
└── Windows
    └── DialogueWindow
        ├── SpeakerText
        ├── BodyText
        ├── ContinueHint
        └── ChoiceRoot
            ├── ChoiceButton_01
            ├── ChoiceButton_02
            ├── ChoiceButton_03
            └── ChoiceButton_04
```

`DialogueWindow` 根物体：

1. 挂 `DialogueWindowBinding`。
2. `Speaker Text` 绑定说话人 TMP_Text。
3. `Body Text` 绑定正文 TMP_Text。
4. `Continue Hint` 绑定继续提示物体。
5. `Choice Root` 绑定选项按钮父节点。
6. `Choice Slots` 数组大小设置为最多显示的选项数量。

每个 `Choice Slots` 元素建议这样绑定：

- `Slot Root`：绑定 `ChoiceButton_xx` 根物体。
- `Button`：绑定该选项按钮的 `Button` 组件。
- `Label Text`：绑定该按钮内的 TMP_Text。
- `Available Root`：可选，绑定“可点击状态”表现物体。
- `Disabled Root`：可选，绑定“不可点击状态”表现物体。
- `On Choice Bound`：可选，选项刷新到按钮时触发，适合播放刷新动画。
- `On Choice Clicked`：可选，玩家点击选项时触发，适合播放音效、动画、埋点。

注意：`On Choice Clicked` 不建议直接写业务逻辑。真正的对话分支会自动通过 `ChoiceId` 回到 Gal 对话系统，保证任务、剧情、存档都能知道玩家选择了什么。

### 普通按钮绑定业务事件
普通 UI 按钮可以绑定业务桥接脚本或控制器的 `void` 方法。推荐规则：

- 打开/关闭 UI：绑定 UI 桥接层或窗口控制脚本。
- 购买、使用、领取、提交：绑定对应模块 Controller 或 Bridge 的公开命令方法。
- 场景跳转：绑定 `NiumaScene.UIBridge.SceneButtonAction`，不要直接调用 `SceneManager.LoadScene`。
- 对话选项：优先用 `ChoiceId` 驱动业务，不要在按钮事件里直接改任务、剧情、存档。

按钮绑定步骤：

1. 选中 Button 物体。
2. 找到 `Button` 组件的 `OnClick()`。
3. 点击 `+` 添加事件。
4. 拖入目标组件所在物体，例如挂了 `SceneButtonAction` 的按钮物体。
5. 在函数下拉框选择公开 `void` 方法，例如 `SceneButtonAction.LoadConfiguredScene()`。

### 对话选项跳转场景
如果对话中有“进入你画我猜 / 下次再说”两个选项：

1. 在 `DialogueAsset` 中配置两个选项：
   - `enter_draw_guess`
   - `maybe_next_time`
2. `DialogueWindowBinding.choiceSlots` 只负责显示这两个按钮。
3. `On Choice Clicked` 可以播放按钮音效，但不要直接跳场景。
4. Gal / Story / Interact 桥接层收到 `enter_draw_guess` 后，再调用 `NiumaSceneController` 进入 MiniGame 场景。
5. `maybe_next_time` 只结束对话或返回普通对话流程。

这样做的好处是：UI 策划能自由摆按钮和做表现，对话系统仍然能稳定记录玩家选择，后续任务、剧情、存档不会断链。

## 协作边界
NiumaUI 只做表现通道，不做业务判断。任务是否可接、物品是否能用、技能是否能放，都由对应业务模块输出 ViewData 或 CanXXX 结果。


