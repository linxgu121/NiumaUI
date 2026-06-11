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

## NiumaUI 2.0：UI Toolkit 阶段 1

2.0 新 UI 以 UI Toolkit 为主线。当前阶段只落地“协议与注册表”，不会自动替换旧 UGUI 面板，也不会迁移 MiniGame。旧 `UIManager / DefaultViewFactory / ViewBindingBase` 暂时保留为 Legacy。

### UIToolkitViewRegistrySO

创建方式：在 Project 面板右键 `Create / NiumaUI / Toolkit View Registry`。

建议放置位置：`Assets/Game/Moudle/NiumaUI/Config` 或项目统一 UI 配置目录。

| Inspector 字段 | 推荐填写 | 可留空 | 留空后果 | 作用 |
| --- | --- | --- | --- | --- |
| `View Id` | 稳定窗口 ID，例如 `DialogueWindow`、`InventoryPanel` | 不可以 | 外部模块无法通过 ViewId 打开该窗口 | 业务层、UIManager、注册表之间的唯一窗口标识。 |
| `Visual Tree Asset` | 拖 UI Builder 制作的 `.uxml` | 不可以 | 工厂无法创建 Toolkit 窗口 | 定义窗口结构。 |
| `Style Sheets` | 拖该窗口需要的 `.uss` | 可以 | 只使用默认样式 | 定义窗口外观。 |
| `Binding Provider Id` | 填程序提供的 Binding ID，例如 `Default`、`DialogueWindow` | 可以 | 空值会按 `Default` 处理 | 第二阶段工厂会用这个 ID 找到真正的 Binding 创建器。 |
| `Layer Id` | 填 `HUD`、`Prompt`、`Dialogue`、`Menu`、`Popup`、`Loading`、`Debug` 等 | 不建议 | 空值会按 `Default` 处理 | 决定窗口生成到哪个 UIDocument 层。 |
| `Cache Policy` | 常用窗口选 `HideAndCache`，一次性窗口选 `DestroyOnClose` | 不可以 | 默认隐藏缓存 | 决定关闭后保留还是销毁。 |
| `Modal Policy` | Popup、Confirm、Loading 选 `Modal`；HUD、Prompt 选 `None` | 不可以 | 默认非模态 | 决定是否阻塞下层 UI 点击。 |
| `Input Policy` | 对话、菜单、弹窗、加载遮罩选 `BlockGameplayInput`；HUD、提示选 `None` | 不可以 | 默认不阻塞玩法输入 | 决定打开窗口时是否冻结玩家输入。 |
| `Back Policy` | 菜单、弹窗选 `CloseOnBack`；HUD、Prompt、Toast 选 `None` | 不可以 | 默认进入返回栈 | 决定 ESC / 返回按钮是否能关闭该 View。 |
| `Default Focus Name` | 填 UXML 中默认焦点元素的 `name` | 可以 | 不自动设置焦点 | 用于键盘/手柄导航。 |

`Binding Provider Id` 不直接拖脚本，这是有意设计。Runtime 包不应依赖 Editor 专用的 `MonoScript` 类型；第二阶段会由 `UIToolkitViewFactory` 在 Inspector 中注册 `BindingProviderId -> BindingProvider` 的映射。

### UIToolkitLayerRoot

该结构会在第二阶段由 Toolkit 工厂使用，用来把 View 生成到对应 UIDocument 层。

| Inspector 字段 | 推荐填写 | 可留空 | 留空后果 | 作用 |
| --- | --- | --- | --- | --- |
| `Layer Id` | 与注册表条目的 `Layer Id` 完全一致 | 不可以 | 找不到对应层级 | 例如 `Dialogue` 的窗口会生成到 `Dialogue` 层。 |
| `Document` | 拖该层级的 `UIDocument` | 不可以 | 该层无法创建窗口 | 每个主要层级建议一个 UIDocument。 |
| `Root Element Name` | 填 UIDocument 中作为父节点的 VisualElement 名字 | 可以 | 使用 `document.rootVisualElement` | 用于把窗口挂到文档内部指定容器。 |

推荐核心场景层级：

```text
UIRoot
├── EventSystem
├── UIToolkitRoot
│   ├── UIDocument_HUD
│   ├── UIDocument_Prompt
│   ├── UIDocument_Dialogue
│   ├── UIDocument_Menu
│   ├── UIDocument_Popup
│   ├── UIDocument_Loading
│   └── UIDocument_Debug
├── UIManager
└── UIBridges
```

### ToolkitViewBindingBase

程序制作具体窗口 Binding 时继承 `ToolkitViewBindingBase`。

- 在 `OnInitialize()` 中用 `Query<T>("元素Name")` 缓存 VisualElement。
- 在 `OnRefresh(object viewData)` 中把 ViewData 写到 Label、Button、Image 等元素。
- 在 `OnOpen()` / `OnClose()` 中处理表现层动画或临时状态。
- 不要在 Binding 中直接修改任务、背包、商店、MiniGame 等业务 RuntimeState。
### UIToolkitViewFactory

建议挂载位置：`UIRoot/UIManager` 同物体，或 `UIRoot/UIToolkitRoot/ViewFactory` 子物体。

| Inspector 字段 | 推荐填写 | 可留空 | 留空后果 | 作用 |
| --- | --- | --- | --- | --- |
| `Registry` | 拖 `UIToolkitViewRegistrySO` | 不建议 | 无法通过 ViewId 创建 Toolkit View | 提供 ViewId 到 UXML/USS/策略的映射。 |
| `Layer Roots` | 按层配置 `LayerId + UIDocument + RootElementName` | 不建议 | 找不到对应 LayerId 时窗口创建失败 | 决定窗口挂到哪个 UIDocument 层。 |
| `Binding Provider Behaviours` | 拖实现 `IToolkitViewBindingProvider` 的组件 | 可以 | 未匹配时使用 Default Binding | 把 `BindingProviderId` 映射到具体 Binding 创建器。 |
| `Log Warnings` | 建议开启 | 可以 | 关闭后缺配置时不提示 | 方便排查注册表、层级和 Binding 问题。 |

`Layer Roots` 示例：

- `HUD` -> `UIDocument_HUD`
- `Prompt` -> `UIDocument_Prompt`
- `Dialogue` -> `UIDocument_Dialogue`
- `Menu` -> `UIDocument_Menu`
- `Popup` -> `UIDocument_Popup`
- `Loading` -> `UIDocument_Loading`
- `Debug` -> `UIDocument_Debug`

如果 `Root Element Name` 为空，View 会直接挂到该 `UIDocument.rootVisualElement` 下。如果填写了名字，工厂会在文档内查找同名 VisualElement 作为父节点；找不到时回退到 `rootVisualElement`。

### UIToolkitUIManager

建议挂载位置：`UIRoot/UIManager`。这是 UI Toolkit 2.0 的根控制器，负责通过 ViewId 打开、关闭、刷新 Toolkit View，并根据 `InputPolicy` 请求玩法输入阻塞。

| Inspector 字段 | 推荐填写 | 可留空 | 留空后果 | 作用 |
| --- | --- | --- | --- | --- |
| `View Factory` | 拖 `UIToolkitViewFactory` | 可以 | 如果 `Auto Resolve Factory` 开启，会自动查找同物体/子物体；仍找不到则无法打开 View | Toolkit View 创建入口。 |
| `Input Blocker Provider` | 使用 TPC 时拖 `PlayerRoot/UIBridge` 上的 `TPCGameplayInputBlocker` | 可以 | `BlockGameplayInput` 不会实际冻结玩家 | Toolkit View 打开/关闭时阻塞或释放玩法输入。 |
| `Toast View Id` | 填注册表中的 Toast ViewId，默认 `Toast` | 可以 | `ShowToast` 找不到对应 View 时返回 false | 短提示统一入口。 |
| `Confirm View Id` | 填注册表中的 Confirm ViewId，默认 `Confirm` | 可以 | `ShowConfirm` 找不到对应 View 时返回 false | 确认/取消弹窗统一入口。 |
| `Loading View Id` | 填注册表中的 Loading ViewId，默认 `Loading` | 可以 | `ShowLoading` 找不到对应 View 时返回 false | 加载遮罩统一入口。 |
| `Enable Keyboard Back` | 建议开启 | 可以 | 关闭后 ESC 不会自动返回，需要外部调用 `TryGoBack()` | 控制是否监听返回键。 |
| `Back Key` | 默认 `Escape` | 可以 | 使用默认 ESC | 返回栈触发按键。 |
| `Drive Tick In Update` | 独立使用时开启；外部统一 Tick 时关闭 | 可以 | 关闭后 Binding.Tick 不会自动执行 | 驱动已打开 Toolkit View 的 Tick。 |
| `Auto Resolve Factory` | 建议开启 | 可以 | 关闭后必须手动拖 `View Factory` | 降低场景配置成本。 |
| `Log Warnings` | 建议开启 | 可以 | 关闭后缺配置时不提示 | 方便排查。 |

常用方法：

- `OpenView(string viewId)`：打开窗口。
- `OpenView(string viewId, object viewData)`：打开并立即刷新表现数据。
- `RefreshView(string viewId, object viewData)`：刷新已打开窗口。
- `CloseView(string viewId)`：关闭指定窗口。
- `CloseTopView()`：关闭当前顶部窗口。
- `CloseAllViews()`：关闭所有 Toolkit 窗口。
- `TryGetBinding<T>()`：获取具体 Binding，供调试或少数强交互面板使用。

### 阶段 3：Toast / Confirm / Loading / BackStack

第三阶段只提供通用入口和数据协议，不自带固定样式。策划仍需要制作对应 UXML / USS，并在 `UIToolkitViewRegistrySO` 中注册。

推荐注册：

| ViewId | LayerId | Modal Policy | Input Policy | Back Policy | 用途 |
| --- | --- | --- | --- | --- | --- |
| `Toast` | `Prompt` | `None` | `None` | `None` | 右上角或底部短提示，自动消失。 |
| `Confirm` | `Popup` | `Modal` | `BlockGameplayInput` | `CloseOnBack` | 确认/取消弹窗。 |
| `Loading` | `Loading` | `Modal` | `BlockGameplayInput` | `None` 或 `CloseOnBack` | 场景加载、网络等待、存档等待遮罩。 |

UXML Binding 写法：

- Toast Binding 继承 `ToolkitViewBindingBase` 并实现 `IToolkitToastBinding`。
- Confirm Binding 继承 `ToolkitViewBindingBase` 并实现 `IToolkitConfirmBinding`。
- Loading Binding 继承 `ToolkitViewBindingBase` 并实现 `IToolkitLoadingBinding`。

调用入口：

```csharp
uiToolkitManager.ShowToast("背包已满", 2f, "warning");
uiToolkitManager.ShowConfirm(new UIToolkitConfirmViewData
{
    Title = "退出房间",
    Message = "确定要离开当前房间吗？",
    ConfirmText = "离开",
    CancelText = "取消",
    Callback = confirmed => { if (confirmed) LeaveRoom(); }
});
uiToolkitManager.ShowLoading("正在切换场景...", -1f, true);
uiToolkitManager.HideLoading();
```

返回栈规则：

- 只有 `Back Policy = CloseOnBack` 的 View 会进入返回栈。
- `Enable Keyboard Back` 开启时，按 `Back Key` 会关闭返回栈顶部 View。
- HUD、交互提示、Toast 通常不进入返回栈。
- 菜单、设置、Confirm、Popup 通常进入返回栈。

模态遮罩规则：

- `Modal Policy = Modal` 时，工厂会自动在同层级下创建一个遮罩节点。
- 遮罩节点类名是 `niuma-modal-blocker`，在 USS 中可配置背景色、透明度和过渡动画。
- 遮罩只阻止同 UIDocument 层级下的下层 UI 点击，不负责业务输入；玩法输入由 `Input Policy` 处理。
### BindingProvider 制作方式

程序给具体窗口写 BindingProvider 时，推荐继承 `ToolkitViewBindingProviderBase`：

```csharp
public sealed class DialogueToolkitBindingProvider : ToolkitViewBindingProviderBase
{
    public override IToolkitViewBinding CreateBinding()
    {
        return new DialogueToolkitBinding();
    }
}
```

然后：

1. 把 Provider 组件挂到 `UIRoot/UIToolkitRoot/BindingProviders`。
2. 在 Provider 的 `Provider Id` 中填写稳定 ID，例如 `DialogueWindow`。
3. 把该组件拖入 `UIToolkitViewFactory.Binding Provider Behaviours`。
4. 在 `UIToolkitViewRegistrySO` 对应条目的 `Binding Provider Id` 填同一个 ID。

如果只是测试纯静态 UXML，可以不配置 Provider，系统会使用 `DefaultToolkitViewBinding`。
## 场景挂载与 Inspector 配置
### UIManager

建议挂在核心场景的 `UIRoot` 或 `UIRoot/UIManager` 物体上。它是 UI 模块的服务入口，负责注册 `IUIService`、接收打开/关闭窗口请求、调用 View Factory 创建窗口。

| Inspector 字段 | 推荐填写 | 可留空 | 留空后果 | 作用 |
| --- | --- | --- | --- | --- |
| `View Factory Provider` | 拖同物体或子物体上的 `DefaultViewFactory` | 可以 | 会尝试使用 `Default View Factory`；仍为空时自动查找或创建 `DefaultViewFactory` | 指定真正负责生成窗口的工厂脚本。 |
| `Default View Factory` | 拖同物体上的 `DefaultViewFactory` | 可以 | 如果两个 Factory 字段都为空，运行时会尝试自动补一个默认工厂 | UIManager 的默认工厂兜底引用。 |
| `Input Blocker Provider` | 使用 TPC 时拖 `PlayerRoot/UIBridge` 上的 `TPCGameplayInputBlocker` | 可以 | UI 打开时不会冻结玩法输入 | UI 面板打开后阻塞玩家移动/交互输入。 |

`View Registry`、窗口生成父节点、Layer Roots 不在 `UIManager` 上填写，它们在 `DefaultViewFactory` 上配置。

### DefaultViewFactory

建议挂在 `UIRoot/UIManager` 同物体，或挂在 `UIRoot/ViewFactory` 子物体上，并把它拖给 `UIManager.View Factory Provider`。它负责把 `ViewId` 翻译成具体 UI 预制体，并决定实例化到哪个 UI 层级父节点下。

| Inspector 字段 | 推荐填写 | 可留空 | 留空后果 | 作用 |
| --- | --- | --- | --- | --- |
| `Registry` | 拖 `UIViewRegistrySO` 资产 | 开发期可以 | 没有注册表时，只能依赖内置兜底窗口；正式 UI 的 ViewId 可能打不开 | 配置 `ViewId -> Binding 预制体` 的映射。 |
| `Default Root` | 拖 Canvas 下默认窗口父节点，例如 `Canvas/Windows/DefaultLayer` | 可以 | 若 `Auto Create Runtime Canvas Root` 开启，会自动创建临时 Canvas；否则窗口没有明确父节点 | 没有指定 LayerId 或找不到 LayerId 时，窗口生成到这里。 |
| `Layer Roots` | 按需添加 `Default`、`Dialogue`、`Popup`、`Toast` 等层级 | 可以 | 所有窗口都走 `Default Root` | 让不同窗口生成到不同父节点，方便控制层级。 |
| `Enable Built In Dialogue Window` | 开发期可勾选，正式 UI 配好后建议关闭 | 可以 | 关闭后，`DialogueWindow` 没注册就不会自动生成保底窗口 | 未配置对话窗口预制体时，是否创建内置测试窗口。 |
| `Auto Create Runtime Canvas Root` | 开发期可勾选，正式核心场景建议关闭并手动绑定 `Default Root` | 可以 | 关闭且未填 Root 时，窗口生成可能失败 | Default Root 为空时是否自动创建运行时 Canvas。 |

`Layer Roots` 的 `LayerId` 要和 `UIViewRegistrySO` 条目里的 `LayerId` 完全一致。比如对话窗口条目填 `Dialogue`，这里就需要有一个 `LayerId = Dialogue` 的元素，并把 `Root` 拖到对话层父节点。

### UI 输入阻塞绑定方式

如果项目使用 `NiumaTPC`，推荐在玩家根物体下建一个子物体：`PlayerRoot/UIBridge`，挂 `TPCGameplayInputBlocker`，然后把这个脚本拖到 `UIManager.Input Blocker Provider`。

绑定后，UI 打开时会通过 `PlayerModuleController.DisableControl("UI.xxx")` 冻结玩家输入；UI 关闭时只释放 `UI.xxx` 原因，不会误解除死亡、剧情、场景加载等其他禁用原因。

如果暂时没有接 TPC，或者该场景不需要 UI 阻塞玩法输入，这个字段可以留空。后续需要支持其他玩法控制器时，再做对应的 `IGameplayInputBlocker` 桥接脚本。

### UIViewRegistrySO
建议配置位置：项目资产目录。

| 字段 | 怎么填 | 可否留空 | 不填会怎样 |
| --- | --- | --- | --- |
| `View Id` | 稳定字符串，例如 `DialogueWindow`、`InventoryPanel` | 不可以 | 外部桥接无法打开该 View |
| `Prefab` | 拖对应 UI 预制体 | 不可以 | ViewId 存在但实例化失败 |

### DialogueWindowBinding / DialogueWindowView
建议挂载位置：对话框预制体根节点。

| 字段 | 怎么填 | 可否留空 | 不填会怎样 |
| --- | --- | --- | --- |
| `Speaker Text` | 拖说话人 TMP 文本 | 可以 | 不显示说话人 |
| `Content Text` | 拖正文 TMP 文本 | 不可以 | 对话内容无法显示 |
| `Choice Root` | 拖选项按钮父节点 | 有选项时不可以 | 最后一句有选项时无法显示选择 |
| `Choice Button Prefab` | 拖选项按钮预制体 | 有选项时不可以 | 无法生成选项 |

### UIAudioBridge / UIButtonAudioBinder
建议挂载位置：`UIRoot/UIAudioRoot` 或具体 Button。

| 字段 | 怎么填 | 可否留空 | 不填会怎样 |
| --- | --- | --- | --- |
| `Audio Controller` | 拖 `NiumaAudioController` | 不建议 | UI 音效不播放 |
| `CueId` | 填 `AudioCueDefinition.CueId` | 不可以 | 对应 UI 动作无声音 |
| `Override Bus` | 普通 UI 建议关闭，除非明确要覆盖 Bus | 可以 | 开启会覆盖 CueDefinition 的 Bus |


