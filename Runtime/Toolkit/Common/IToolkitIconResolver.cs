using System;
using UnityEngine;

namespace NiumaUI.Toolkit.Common
{
    /// <summary>
    /// UI Toolkit 图标解析器。第一版可用本地字典实现，后续可替换为 Addressables。
    /// </summary>
    public interface IToolkitIconResolver
    {
        Texture2D MissingIcon { get; }
        Texture2D LoadingIcon { get; }
        bool TryResolve(string addressKey, out Texture2D texture);
    }

    /// <summary>
    /// 异步图标解析器预留接口。回调必须回到 Unity 主线程后再修改 VisualElement。
    /// </summary>
    public interface IAsyncToolkitIconResolver : IToolkitIconResolver
    {
        void ResolveAsync(string addressKey, Action<string, Texture2D> completed);
    }
}
