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

## 场景使用方法
推荐放置方式：`UIRoot` 一个 UI 根物体管理 Canvas、EventSystem、UIManager 和窗口预制体。

- `UIRoot`：挂 `Canvas`、`GraphicRaycaster`，建议作为全局或当前场景唯一 UI 根。
- `UIRoot/EventSystem`：放 Unity `EventSystem`。按钮无响应时先检查它是否存在。
- `UIRoot/UIManager`：挂 `UIManager` 和 `DefaultViewFactory`，统一打开、关闭和创建 View。
- `UIRoot/Windows`：放对话框、背包、任务、商店等窗口预制体实例或模板。
- `UIRoot/PromptLayer`：放交互提示、Toast、Loading 等轻量面板。
- 各业务模块的 `XXXUIViewBridge` 可以挂在对应模块根物体，也可以统一放在 `UIRoot/Bridges`，但不要把业务 Service 直接挂到 UI 按钮上。
- 每个窗口建议一个 Binding 组件管理 Text/Button/Image 引用；ViewBase 保持纯 C# 逻辑。

## 协作边界
NiumaUI 只做表现通道，不做业务判断。任务是否可接、物品是否能用、技能是否能放，都由对应业务模块输出 ViewData 或 CanXXX 结果。


