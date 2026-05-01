using System.Collections.Generic;
using System.Linq;
using NiumaUI.Arbitration;
using NiumaUI.Core.Interface;
using NiumaUI.Enum;
using NiumaUI.RunTimeData;
using NiumaUI.State;
using NiumaUI.State.Base;
using NiumaUI.State.UIStates;
using UnityEngine;

namespace NiumaUI.Core
{
    /// <summary>
    /// UI 系统根控制器
    /// 状态机 + 数据黑板 + 仲裁器 + 根控制类
    /// 生命周期由本类统一驱动，View 不自驱
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("外部依赖注入")]
        [Tooltip("实现 IViewFactory 的组件")]
        public MonoBehaviour ViewFactoryProvider;

        [SerializeField] private DefaultViewFactory defaultViewFactory;

        [Tooltip("实现 IGameplayInputBlocker 的组件（如 TPC 输入阻塞器）")]
        public MonoBehaviour InputBlockerProvider;

        // 核心系统
        public UIBlackboard Blackboard { get; private set; }
        public UIArbiter Arbiter { get; private set; }
        public UIStateMachine StateMachine { get; private set; }

        // 外部接口
        private IViewFactory _viewFactory;
        private IGameplayInputBlocker _inputBlocker;

        // 快照（用于检测黑板变更）
        private UIMode _lastMode;
        private readonly List<string> _lastViewStack = new List<string>();
        private readonly List<string> _lastTickList = new List<string>();

        // 活跃视图缓存（仅用于 Tick 驱动与释放，不用于状态判断）
        private readonly Dictionary<string, ViewBase> _activeViews = new Dictionary<string, ViewBase>();

        private void Awake()
        {
            Blackboard = new UIBlackboard();
            Arbiter = new UIArbiter(Blackboard);
            StateMachine = new UIStateMachine();

            if (ViewFactoryProvider == null)
            {
                if (defaultViewFactory == null)
                    defaultViewFactory = GetComponent<DefaultViewFactory>();

                ViewFactoryProvider = defaultViewFactory;
            }

            _viewFactory = ViewFactoryProvider as IViewFactory;
            _inputBlocker = InputBlockerProvider as IGameplayInputBlocker;

            if (_viewFactory == null)
                Debug.LogError("[UIManager] ViewFactoryProvider 未实现 IViewFactory");
        }

        private void Start()
        {
            _lastMode = Blackboard.CurrentMode;
            Blackboard.OnModeChanged += OnModeChanged;
            StateMachine.Initialize(new GameplayUIState(_inputBlocker));
        }

        public bool RequestMode(UIMode mode)
        {
            return Arbiter != null && Arbiter.RequestMode(mode);
        }

        public bool PushView(string viewId)
        {
            return Arbiter != null && Arbiter.RequestPush(viewId);
        }

        public bool PopView()
        {
            return Arbiter != null && Arbiter.RequestPop();
        }

        public bool CloseFocusView()
        {
            return Arbiter != null && Arbiter.RequestCloseFocus();
        }

        public bool ClearAllViews()
        {
            return Arbiter != null && Arbiter.RequestClearAll();
        }

        public bool AddTickView(string viewId)
        {
            return Arbiter != null && Arbiter.RequestAddTick(viewId);
        }

        public bool RemoveTickView(string viewId)
        {
            return Arbiter != null && Arbiter.RequestRemoveTick(viewId);
        }

        private void OnDestroy()
        {
            Blackboard.OnModeChanged -= OnModeChanged;

            foreach (var kvp in _activeViews)
                _viewFactory?.Release(kvp.Key);
            _activeViews.Clear();
        }

        private void Update()
        {
            // 1. 同步视图栈与 Tick 列表
            SyncViewStack();
            SyncTickList();

            // 2. 状态机逻辑更新
            StateMachine.CurrentState?.LogicUpdate();
        }

        private void LateUpdate()
        {
            // 驱动 Tick 列表中的视图
            float dt = Time.deltaTime;
            foreach (var viewId in _lastTickList)
            {
                if (_activeViews.TryGetValue(viewId, out var view))
                    view.Tick(dt);
            }
        }

        #region 模式切换

        private void OnModeChanged(UIMode oldMode, UIMode newMode)
        {
            UIStateBase newState = newMode switch
            {
                UIMode.Gameplay => new GameplayUIState(_inputBlocker),
                UIMode.Dialogue => new DialogueUIState(_inputBlocker),
                UIMode.Menu => new MenuUIState(_inputBlocker),
                UIMode.Cinematic => new CinematicUIState(_inputBlocker, CloseAllGameViews),
                _ => new GameplayUIState(_inputBlocker)
            };
            StateMachine.ChangeState(newState);
        }

        #endregion

        #region 视图栈同步

        private void SyncViewStack()
        {
            var current = Blackboard.ViewStack;

            // 关闭超出当前栈长度的旧视图
            for (int i = _lastViewStack.Count - 1; i >= current.Count; i--)
            {
                CloseView(_lastViewStack[i]);
                _lastViewStack.RemoveAt(i);
            }

            // 打开新增或变更的视图
            for (int i = 0; i < current.Count; i++)
            {
                if (i >= _lastViewStack.Count || _lastViewStack[i] != current[i])
                {
                    for (int j = _lastViewStack.Count - 1; j >= i; j--)
                    {
                        CloseView(_lastViewStack[j]);
                        _lastViewStack.RemoveAt(j);
                    }

                    for (int j = i; j < current.Count; j++)
                    {
                        OpenView(current[j]);
                        if (_lastViewStack.Count <= j)
                            _lastViewStack.Add(current[j]);
                        else
                            _lastViewStack[j] = current[j];
                    }
                    break;
                }
            }
        }

        private void SyncTickList()
        {
            var current = Blackboard.TickList;

            foreach (var viewId in current)
            {
                if (!_lastTickList.Contains(viewId))
                {
                    _lastTickList.Add(viewId);
                    CacheView(viewId);
                }
            }

            for (int i = _lastTickList.Count - 1; i >= 0; i--)
            {
                if (!current.Contains(_lastTickList[i]))
                    _lastTickList.RemoveAt(i);
            }
        }

        #endregion

        #region 视图操作

        private void OpenView(string viewId)
        {
            if (_viewFactory == null) return;

            var view = _viewFactory.Get(viewId);
            if (view == null)
            {
                Debug.LogError($"[UIManager] 无法获取视图: {viewId}");
                return;
            }

            _activeViews[viewId] = view;
            view.Open();
        }

        private void CloseView(string viewId)
        {
            if (_activeViews.TryGetValue(viewId, out var view))
            {
                view.Close();
                _activeViews.Remove(viewId);
                _viewFactory?.Release(viewId);
            }
        }

        private void CacheView(string viewId)
        {
            if (_viewFactory == null || _activeViews.ContainsKey(viewId)) return;

            var view = _viewFactory.Get(viewId);
            if (view != null)
                _activeViews[viewId] = view;
        }

        /// <summary>
        /// 关闭所有游戏视图（Cinematic 状态调用）
        /// </summary>
        private void CloseAllGameViews()
        {
            var toClose = new List<string>();
            foreach (var kvp in _activeViews)
            {
                if (kvp.Key != "SubtitleBar")
                    toClose.Add(kvp.Key);
            }
            foreach (var id in toClose)
                CloseView(id);
        }

        #endregion
    }
}
