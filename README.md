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


