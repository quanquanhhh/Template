using MythMerge.Core;
using MythMerge.LauncherEngine;
// **********************************************
// 
// Author : xuejie.guo
// Date : 2023/06/25/12:30
// Ver : 1.0.0
// Description : UIModule.cs
// ChangeLog :
// **********************************************

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;

namespace MythMerge
{
    public class UIModule: Singleton<UIModule>, IModule
    {
        private const int DefaultPriority = 0;

        private Transform instanceRoot = null;
      //  [SerializeField] private bool m_enableErrorLog = true;
        
        private Camera m_UICamera = null;
        private Camera m_WorldCamera = null;

        private readonly List<UIWindow> _stack = new List<UIWindow>(100);

        public const int LAYER_DEEP = 2000;
        public const int WINDOW_DEEP = 100;
        public const int WINDOW_HIDE_LAYER = 2; // Ignore Raycast
        public const int WINDOW_SHOW_LAYER = 5; // UI
        
        public Transform UIRoot => instanceRoot;
        
        public static Transform UIRootStatic;
        protected  GameObject mEventSystem;
        
        
        /// <summary>
        /// UI根节点。
        /// </summary>
        public Camera UICamera => m_UICamera;
        public Camera WorldCamera => m_WorldCamera;

        public void InitializeModule()
        {
            EnsureUIRoot();
            //TODo BindingRootObj
            
            if (instanceRoot == null)
            {
                instanceRoot = GameObject.Find("Root/UIRoot/UICanvas").transform;
                
                instanceRoot.localScale = Vector3.one;
                
                m_UICamera = GameObject.Find("Root/UIRoot/UICamera").GetComponent<Camera>();
                
                m_WorldCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
            }
            // else if (m_dontDestroyUIRoot)
            // {
            //    GameObject.DontDestroyOnLoad(instanceRoot.parent !=null ? instanceRoot.parent : instanceRoot);
            // }

            mEventSystem = GameObject.Find("Root/UIRoot/EventSystem");
            mEventSystem.gameObject.SetActive(true);
            
            instanceRoot.gameObject.layer = LayerMask.NameToLayer("UI");
            UIRootStatic = instanceRoot;
        }

        private static void EnsureUIRoot()
        {
            var root = GameObject.Find("Root") ?? new GameObject("Root");
            UnityEngine.Object.DontDestroyOnLoad(root);
            Transform Child(Transform parent, string name, bool rect = false)
            {
                var child = parent.Find(name);
                if (child != null) return child;
                var go = rect ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
                go.transform.SetParent(parent, false); return go.transform;
            }
            var ui = Child(root.transform, "UIRoot");
            var cameraNode = Child(ui, "UICamera");
            var camera = cameraNode.GetComponent<Camera>();
            if (camera == null) camera = cameraNode.gameObject.AddComponent<Camera>();
            camera.orthographic = true; camera.clearFlags = CameraClearFlags.Depth; camera.cullingMask = 1 << 5; camera.depth = 10;
            var canvasNode = Child(ui, "UICanvas", true);
            var canvas = canvasNode.GetComponent<Canvas>();
            if (canvas == null) canvas = canvasNode.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
            var scaler = canvasNode.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvasNode.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(768, 1536); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            if (canvasNode.GetComponent<GraphicRaycaster>() == null) canvasNode.gameObject.AddComponent<GraphicRaycaster>();
            var events = Child(ui, "EventSystem");
            if (events.GetComponent<UnityEngine.EventSystems.EventSystem>() == null) events.gameObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
            if (events.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null) events.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        public bool EnableEventSystem
        {
            set
            {
                if (mEventSystem == null)
                    return;

                Debug.Log("SetValue:" + value);
                mEventSystem.SetActive(value);
            }
            get
            {
                if (mEventSystem == null)
                    return false;

                return mEventSystem.activeSelf;
            }
        }
        
        public void EnableTouch(bool enable)
        {
            instanceRoot.GetComponent<GraphicRaycaster>().enabled = enable;
        }

        public void OnStartGameLogic()
        {
        }

        public void OnShutDown()
        {
            CloseAll();
        }
        
        private void OnDestroy()
        {
            CloseAll();
        }

        /// <summary>
        /// 获取指定层级下顶部的窗口名称。
        /// </summary>
        public string GetTopWindow(int layer)
        {
            UIWindow lastOne = null;
            for (int i = 0; i < _stack.Count; i++)
            {
                if (_stack[i].WindowLayer == layer)
                    lastOne = _stack[i];
            }

            if (lastOne == null)
                return string.Empty;

            return lastOne.WindowName;
        }

        
        /// <summary>
        /// 是否有任意窗口正在加载。
        /// </summary>
        public bool IsAnyLoading()
        {
            for (int i = 0; i < _stack.Count; i++)
            {
                var window = _stack[i];
                if (window.IsLoadDone == false)
                    return true;
            }

            return false;
        }
        
        /// <summary>
        /// 查询窗口是否存在。
        /// </summary>
        /// <typeparam name="T">界面类型。</typeparam>
        /// <returns>是否存在。</returns>
        public bool HasWindow<T>()
        {
            return HasWindow(typeof(T));
        }

        /// <summary>
        /// 查询窗口是否存在。
        /// </summary>
        /// <param name="type">界面类型。</param>
        /// <returns>是否存在。</returns>
        public bool HasWindow(Type type)
        {
            return IsContains(type.FullName);
        }
        
        /// <summary>
        /// 异步打开窗口。
        /// </summary>
        /// <param name="userDatas">用户自定义数据。</param>
        /// <returns>打开窗口操作句柄。</returns>
        public OpenWindowOperation ShowUIAsync<T>(params System.Object[] userDatas) where T : UIWindow
        {
            return ShowUIAsync(typeof(T), userDatas);
        }

        /// <summary>
        /// 显示等待窗口
        /// </summary>


        /// <summary>
        /// 隐藏等待窗口
        /// </summary>


        
        /// <summary>
        /// 异步打开窗口。
        /// </summary>
        /// <param name="type">界面类型。</param>
        /// <param name="userDatas">用户自定义数据。</param>
        /// <returns>打开窗口操作句柄。</returns>
        public OpenWindowOperation ShowUIAsync(Type type, params System.Object[] userDatas)
        {
            string windowName = type.FullName;

            // 如果窗口已经存在
            if (IsContains(windowName))
            {
                UIWindow window = GetWindow(windowName);
                Pop(window); //弹出窗口
                Push(window); //重新压入
                window.TryInvoke(OnWindowPrepare, userDatas);
                var operation = new OpenWindowOperation(window);
                YooAssets.StartOperation(operation);
                return operation;
            }
            else
            {
                UIWindow window = CreateInstance(type);
                Push(window); //首次压入
                window.InternalLoad(window.AssetName, OnWindowPrepare, userDatas);
                var operation = new OpenWindowOperation(window);
                YooAssets.StartOperation(operation);
                return operation;
            }
        }
        
        /// <summary>
        /// 同步打开窗口。
        /// </summary>
        /// <param name="type"></param>
        /// <param name="userDatas"></param>
        /// <returns>打开窗口操作句柄。</returns>
        public OpenWindowOperation ShowUI(Type type, params System.Object[] userDatas)
        {
            var operation = ShowUIAsync(type, userDatas);
            operation.WaitForAsyncComplete();
            return operation;
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        public void CloseWindow<T>(Action finishCallback = null) where T : UIWindow
        {
            CloseWindow(typeof(T),  finishCallback);
        }
        
        public async void CloseWindow(Type type, Action finishCallback = null)
        {
            string windowName = type.FullName;
            UIWindow window = GetWindow(windowName);
            if (window == null)
                return;

            await window.ShowCloseAnimation();
            
            window.InternalDestroy();
            Pop(window);
            EventBus.Dispatch(new WindowCloseEvent());
            OnSortWindowDepth(window.WindowLayer);
            OnSetWindowVisible();
            
            finishCallback?.Invoke();
        }
        /// <summary>
        /// 关闭所有窗口。
        /// </summary>
        public void CloseAll()
        {
            for (int i = 0; i < _stack.Count; i++)
            {
                UIWindow window = _stack[i];
                window.InternalDestroy();
            }

            _stack.Clear();
        }
        
        private async void OnWindowPrepare(UIWindow window)
        {
            window.IsOpened = false;
            try
            {
            if (window.HasPreLoadAssets())
            {
                window.gameObject.SetActive(false);
                await window.PreLoadAssets();
                if (window.Handle == null || !window.Handle.IsValid) return;
                window.gameObject.SetActive(true);
            }
            OnSortWindowDepth(window.WindowLayer);
            window.InternalCreate();
            window.InternalRefresh();
            OnSetWindowVisible();
            EventBus.Dispatch(new WindowOpenEvent());
            window.IsOpened = true;
            }
            catch (Exception error) { window.LoadError = error; }
        }
        
        private void OnSortWindowDepth(int layer)
        {
            int depth = layer * LAYER_DEEP;
            for (int i = 0; i < _stack.Count; i++)
            {
                if (_stack[i].WindowLayer == layer)
                {
                    _stack[i].Depth = depth;
                    depth += WINDOW_DEEP;
                }
            }
        }
        private void OnSetWindowVisible()
        {
            bool isHideNext = false;
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                UIWindow window = _stack[i];
                if (isHideNext == false)
                {
                    window.Visible = true;
                    if (window.IsPrepare && window.FullScreen)
                    {
                        isHideNext = true;
                    }
                }
                else
                {
                    if (!window.ForceShow)
                        window.Visible = false;
                }
            }
        }

        private UIWindow CreateInstance(Type type)
        {
            UIWindow window = Activator.CreateInstance(type) as UIWindow;
            WindowAttribute attribute = Attribute.GetCustomAttribute(type, typeof(WindowAttribute)) as WindowAttribute;

            if (window == null)
                throw new Exception($"Window {type.FullName} create instance failed.");
            if (attribute == null)
                throw new Exception($"Window {type.FullName} not found {nameof(WindowAttribute)} attribute.");

            string assetName = string.IsNullOrEmpty(attribute.AssetName) ? type.Name : attribute.AssetName;
            window.Init(type.FullName, attribute.WindowLayer, attribute.FullScreen, attribute.ForceShow, assetName);
            return window;
        }

        
        private UIWindow GetWindow(string windowName)
        {
            for (int i = 0; i < _stack.Count; i++)
            {
                UIWindow window = _stack[i];
                if (window.WindowName == windowName)
                {
                    return window;
                }
            }

            return null;
        }

        public T GetWindow<T>() where T : UIWindow
        {
            for (int i = 0; i < _stack.Count; i++)
            {
                if (_stack[i] is T)
                {
                    return (T)_stack[i];
                }
            }
            return null;
        }
        
        private bool IsContains(string windowName)
        {
            for (int i = 0; i < _stack.Count; i++)
            {
                UIWindow window = _stack[i];
                if (window.WindowName == windowName)
                {
                    return true;
                }
            }

            return false;
        }
        
        private void Push(UIWindow window)
        {
            // 如果已经存在
            if (IsContains(window.WindowName))
                throw new System.Exception($"Window {window.WindowName} is exist.");

            // 获取插入到所属层级的位置
            int insertIndex = -1;
            for (int i = 0; i < _stack.Count; i++)
            {
                if (window.WindowLayer == _stack[i].WindowLayer)
                {
                    insertIndex = i + 1;
                }
            }

            // 如果没有所属层级，找到相邻层级
            if (insertIndex == -1)
            {
                for (int i = 0; i < _stack.Count; i++)
                {
                    if (window.WindowLayer > _stack[i].WindowLayer)
                    {
                        insertIndex = i + 1;
                    }
                }
            }

            // 如果是空栈或没有找到插入位置
            if (insertIndex == -1)
            {
                insertIndex = 0;
            }

            // 最后插入到堆栈
            _stack.Insert(insertIndex, window);
        }
        
        private void Pop(UIWindow window)
        {
            // 从堆栈里移除
            _stack.Remove(window);
        }

        public bool CheckHasWindow()
        {
            foreach (var window in _stack)
                if (window.Visible && !window.ForceShow) return true;
            return false;
        }
    }
}
