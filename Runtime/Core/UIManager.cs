using System.Collections.Generic;
using System.Linq;
using NiumaUI.Arbitration;
using NiumaUI.Core.Interface;
using NiumaUI.Enum;
using NiumaUI.RuntimeData;
using NiumaUI.State;
using NiumaUI.State.Base;
using NiumaUI.State.UIStates;
using UnityEngine;

namespace NiumaUI.Core
{
    /// <summary>
    /// UI 系统根控制器。
    /// 负责状态机、数据黑板、视图工厂和玩法输入阻塞的统一驱动。
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public const string SubtitleBarViewId = "SubtitleBar";

        [Header("外部依赖注入")]
        [Tooltip("视图工厂脚本。通常拖 DefaultViewFactory；如果使用自定义 UI 工厂，则拖团队制作的 ViewFactory 脚本。")]
        public MonoBehaviour ViewFactoryProvider;

        [Tooltip("默认视图工厂脚本。通常拖同物体上的 DefaultViewFactory；View Registry、窗口层级和生成父节点在 DefaultViewFactory 上配置。")]
        [SerializeField] private DefaultViewFactory defaultViewFactory;

        [Tooltip("玩法输入阻塞脚本。使用 NiumaTPC 时，拖 PlayerRoot/UIBridge 上的 TPCGameplayInputBlocker；没有需要 UI 阻塞的玩法输入时可留空。")]
        public MonoBehaviour InputBlockerProvider;

        public UIBlackboard Blackboard { get; private set; }
        public UIArbiter Arbiter { get; private set; }
        public UIStateMachine StateMachine { get; private set; }

        private IViewFactory _viewFactory;
        private IGameplayInputBlocker _inputBlocker;

        private UIMode _lastMode;
        private readonly List<string> _lastViewStack = new List<string>();
        private readonly List<string> _lastTickList = new List<string>();
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

                if (defaultViewFactory == null)
                    defaultViewFactory = gameObject.AddComponent<DefaultViewFactory>();

                ViewFactoryProvider = defaultViewFactory;
            }

            _viewFactory = ViewFactoryProvider as IViewFactory;
            _inputBlocker = InputBlockerProvider as IGameplayInputBlocker;

            if (_viewFactory == null)
                Debug.LogError("[UIManager] ViewFactoryProvider 绑定的不是视图工厂脚本，请拖 DefaultViewFactory 或团队制作的 ViewFactory 脚本。", this);
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

        /// <summary>
        /// 请求关闭指定视图，不要求该视图位于栈顶。
        /// </summary>
        public bool CloseViewById(string viewId)
        {
            return Arbiter != null && Arbiter.RequestClose(viewId);
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

        /// <summary>
        /// 尝试获取已打开的视图实例。仅用于当前活跃视图的临时调试或旧 UGUI 过渡逻辑。
        /// </summary>
        public bool TryGetView<T>(string viewId, out T view) where T : ViewBase
        {
            if (_activeViews.TryGetValue(viewId, out var rawView) && rawView is T typedView)
            {
                view = typedView;
                return true;
            }

            view = null;
            return false;
        }

        private void OnDestroy()
        {
            if (Blackboard != null)
                Blackboard.OnModeChanged -= OnModeChanged;

            foreach (var kvp in _activeViews)
                _viewFactory?.Release(kvp.Key);

            _activeViews.Clear();
        }

        private void Update()
        {
            SyncViewStack();
            SyncTickList();
            StateMachine.CurrentState?.LogicUpdate();
        }

        private void LateUpdate()
        {
            var deltaTime = Time.deltaTime;
            foreach (var viewId in _lastTickList)
            {
                if (_activeViews.TryGetValue(viewId, out var view))
                    view.Tick(deltaTime);
            }
        }

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

        private void SyncViewStack()
        {
            var current = Blackboard.ViewStack;

            for (var i = _lastViewStack.Count - 1; i >= current.Count; i--)
            {
                CloseView(_lastViewStack[i]);
                _lastViewStack.RemoveAt(i);
            }

            for (var i = 0; i < current.Count; i++)
            {
                if (i < _lastViewStack.Count && _lastViewStack[i] == current[i])
                    continue;

                for (var j = _lastViewStack.Count - 1; j >= i; j--)
                {
                    CloseView(_lastViewStack[j]);
                    _lastViewStack.RemoveAt(j);
                }

                for (var j = i; j < current.Count; j++)
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

        private void SyncTickList()
        {
            var current = Blackboard.TickList;

            foreach (var viewId in current)
            {
                if (_lastTickList.Contains(viewId))
                    continue;

                _lastTickList.Add(viewId);
                CacheView(viewId);
            }

            for (var i = _lastTickList.Count - 1; i >= 0; i--)
            {
                if (!current.Contains(_lastTickList[i]))
                    _lastTickList.RemoveAt(i);
            }
        }

        private void OpenView(string viewId)
        {
            if (_viewFactory == null)
                return;

            var view = _viewFactory.Get(viewId);
            if (view == null)
            {
                Debug.LogError($"[UIManager] 无法获取视图：{viewId}", this);
                return;
            }

            _activeViews[viewId] = view;
            view.Open();
        }

        private void CloseView(string viewId)
        {
            if (!_activeViews.TryGetValue(viewId, out var view))
                return;

            view.Close();
            _activeViews.Remove(viewId);
            _viewFactory?.Release(viewId);
        }

        private void CacheView(string viewId)
        {
            if (_viewFactory == null || _activeViews.ContainsKey(viewId))
                return;

            var view = _viewFactory.Get(viewId);
            if (view != null)
                _activeViews[viewId] = view;
        }

        private void CloseAllGameViews()
        {
            var toClose = new List<string>();
            foreach (var kvp in _activeViews)
            {
                if (kvp.Key != SubtitleBarViewId)
                    toClose.Add(kvp.Key);
            }

            foreach (var id in toClose)
                CloseView(id);
        }
    }
}
