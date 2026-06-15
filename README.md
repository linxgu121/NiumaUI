# NiumaUI

`NiumaUI` 是项目统一 UI 生命周期与表现层框架。当前正式主线是 **NiumaUI 2.1.0 / UI Toolkit**。

从 2.1.0 起，旧 UGUI 1.0 主链路已经硬退场：`UIManager`、`DefaultViewFactory`、`UIViewRegistrySO`、`ViewBase`、`ViewBindingBase`、`DialogueWindowView`、`DialogueWindowBinding` 等旧脚本不再作为运行时代码保留。新 UI、业务桥接和 MiniGame UI 都必须走 UI Toolkit。

## 模块职责

NiumaUI 负责：

- 通过 `ViewId` 打开、关闭、刷新、缓存和销毁 UI Toolkit View。
- 管理 UI 分层：`HUD`、`Prompt`、`Dialogue`、`Menu`、`Popup`、`Loading`、`Debug`。
- 管理模态、返回栈、焦点、玩法输入阻塞。
- 管理 `UXML / USS / BindingProvider / Binding / ViewModel`。
- 提供 Toast、Confirm、Loading 等通用 View 能力。

NiumaUI 不负责：

- 背包、任务、商店、剧情、小游戏等业务规则。
- 直接修改业务 Service 的运行时状态。
- 战斗、交互、场景加载等玩法事实判断。

业务模块继续输出自己的 `ViewData / UIUpdate`，再由对应 `XxxToolkitReceiver` 推给 `UIToolkitUIManager`。

## 推荐场景层级

核心场景建议只保留一套全局 UI Root：

```text
UIRoot
├─ EventSystem
├─ UIToolkitRoot
│  ├─ UIDocument_HUD
│  ├─ UIDocument_Prompt
│  ├─ UIDocument_Dialogue
│  ├─ UIDocument_Menu
│  ├─ UIDocument_Popup
│  ├─ UIDocument_Loading
│  └─ UIDocument_Debug
├─ UIManager
│  ├─ UIToolkitUIManager
│  └─ UIToolkitViewFactory
└─ UIBridges
   ├─ DialogueToolkitReceiver
   ├─ InventoryToolkitReceiver
   ├─ MiniGameToolkitReceiver
   └─ 其他模块 ToolkitReceiver
```

`UIManager` 是物体名，不是旧 UGUI `UIManager` 脚本。这个物体上建议只挂 `UIToolkitUIManager` 和 `UIToolkitViewFactory`。

## 必挂脚本

### EventSystem

挂 Unity 自带 `EventSystem`。如果按钮、输入框、ListView 没反应，优先检查这里。

### UIToolkitUIManager

建议挂在 `UIRoot/UIManager` 物体上。

常用字段：

| 字段 | 填什么 | 可留空 | 留空后果 |
|---|---|---|---|
| View Factory | 同物体或子物体上的 `UIToolkitViewFactory` | 不建议 | 无法打开任何 View |
| Input Blocker Provider | 需要冻结玩法输入时，拖玩家输入阻塞桥接，例如 `TPCGameplayInputBlocker` | 可以 | UI 打开时不会冻结玩家控制 |
| Toast / Confirm / Loading ViewId | 注册表中对应通用 View 的 `ViewId` | 可以 | 对应通用弹窗不可用 |
| Enable Keyboard Back | 是否监听 ESC 返回 | 可以 | 关闭后需要外部输入系统调用 `TryGoBack()` |
| Drive Tick In Update | 是否由本组件每帧 Tick | 可以 | 如果外部统一 Tick，应关闭避免重复驱动 |

### UIToolkitViewFactory

建议与 `UIToolkitUIManager` 挂在同一个物体上。

| 字段 | 填什么 | 可留空 | 留空后果 |
|---|---|---|---|
| Registry | `UIToolkitViewRegistrySO` | 不建议 | 无法按 ViewId 创建窗口 |
| Layer Roots | 每个 LayerId 对应的 `UIDocument` | 不建议 | 对应层无法显示 View |
| Binding Provider Behaviours | 各模块的 `XxxToolkitBindingProvider` | 可以 | 未匹配时使用默认空 Binding，窗口可能无内容 |
| Log Warnings | 建议开启 | 可以 | 缺配置时不提示 |

### UIAudioBridge（可选）

需要 UI 打开、关闭、焦点变化音效时，挂在 `UIRoot` 或 `UIBridges` 下。

| 字段 | 填什么 |
|---|---|
| Toolkit UI Manager | 拖 `UIToolkitUIManager`，不要拖旧 UGUI 脚本 |
| Audio Controller | 拖 `AudioRoot` 上的 `NiumaAudioController` |
| View Audio Cues | `ViewId` 填 `UIToolkitViewRegistrySO` 中注册的 ID；CueId 填 `AudioCueDefinition.CueId` |

## UIToolkitViewRegistrySO

创建方式：Project 右键 `Create / NiumaUI / Toolkit View Registry`。

每个 View 条目建议这样填：

| 字段 | 填什么 | 说明 |
|---|---|---|
| View Id | 稳定 ID，例如 `InventoryPanel` | 业务桥接和 `OpenView` 都靠它找窗口 |
| Visual Tree Asset | 对应 `.uxml` | UI 结构 |
| Style Sheets | 对应 `.uss` | UI 样式 |
| Binding Provider Id | Provider 的 ID | 留空时走 `Default` |
| Layer Id | `HUD/Menu/Popup` 等 | 决定挂到哪个 UIDocument |
| Cache Policy | 常用面板 `HideAndCache`，一次性弹窗 `DestroyOnClose` | 决定关闭后是否缓存 |
| Modal Policy | Popup/Loading 通常 Modal，HUD/Prompt 通常 None | 是否阻塞底层 UI 点击 |
| Input Policy | 菜单/对话/加载通常阻塞玩法输入，HUD 通常不阻塞 | 是否冻结玩家控制 |
| Input Block Mode | `Dialogue/Menu/Cinematic` 等 | 传给输入阻塞桥接的原因 |
| Back Policy | 菜单/弹窗通常 `CloseOnBack`，HUD 通常 `None` | ESC/返回键策略 |
| Default Focus Name | 默认聚焦元素 name | 可留空 |

## 业务模块接入规则

每个业务模块推荐独立一个 `NiumaXxx.ToolkitBridge` 程序集。

依赖方向固定为：

```text
NiumaXxx.ToolkitBridge -> NiumaXxx.Runtime + NiumaUI.Runtime
```

禁止 `NiumaUI.Runtime` 反向引用任何业务模块。

每个业务 ToolkitBridge 至少包含：

- `XxxToolkitReceiver`：接收模块 UIUpdate，调用 `OpenView / RefreshView / CloseView`。
- `XxxToolkitBindingProvider`：在 Inspector 里配置 UXML 元素 name 和按钮命令入口。
- `XxxToolkitBinding`：把 ViewData 写入 VisualElement。
- `XxxToolkitViewModel`：保存 UI 局部状态，例如选中项、搜索关键字、分页、临时输入。

按钮点击只能回到对应模块的 `UIViewBridge`、`CommandRelay` 或 Controller 公开命令，不允许直接改 Service 内部状态。

## UXML 命名建议

| 名称 | 用途 |
|---|---|
| `TitleText` | 标题文本 |
| `StatusText` | 状态或错误文本 |
| `ContentRoot` | 正常内容根节点 |
| `EmptyRoot` | 空状态根节点 |
| `ErrorRoot` | 错误状态根节点 |
| `LoadingRoot` | 加载状态根节点 |
| `CloseButton` | 关闭按钮 |
| `ConfirmButton` | 确认按钮 |
| `CancelButton` | 取消按钮 |
| `ListRoot` / `ItemList` | 列表 |
| `DetailRoot` | 详情区域 |
| `ResultText` | 结果提示 |

具体模块可以扩展，但 README 和 Tooltip 必须写清楚元素应该放在哪里、填什么、不填会怎样。

## Legacy UGUI 退场说明

旧 UGUI 主链路已经删除，不再推荐也不再兼容新增功能。

已退场脚本包括：

- `UIManager`
- `DefaultViewFactory`
- `UIViewRegistrySO`
- `ViewBase`
- `ViewBindingBase`
- `DialogueWindowView`
- `DialogueWindowBinding`
- 旧 `UIArbiter / UIBlackboard / UIStateMachine`

如果旧场景或旧 Prefab 挂了这些脚本，Unity 会显示 Missing Script。处理方式是：删除旧组件，改挂 UI Toolkit 的 `UIToolkitUIManager`、`UIToolkitViewFactory` 和对应 `XxxToolkitReceiver / XxxToolkitBindingProvider`。

## 验证清单

新增或迁移 UI 面板后至少验证：

1. ViewId 能通过 `UIToolkitUIManager.OpenView(ViewId)` 打开。
2. `Refresh / Result / Cleared` 能正确映射到 Toolkit View。
3. Cleared 且 `closeOnCleared = true` 时关闭后立即返回，不重新打开。
4. 列表使用 `ToolkitListBinding<T>`，避免每次刷新手写 `new Label + Add`。
5. 按钮回调只调用 Bridge / CommandRelay / Controller 的公开方法。
6. 面板打开时输入阻塞策略正确，关闭后能释放对应原因。
