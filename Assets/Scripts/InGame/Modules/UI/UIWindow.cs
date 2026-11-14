using MythMerge.Core;
using MythMerge.LauncherEngine;
// **********************************************
// 
// Author : xuejie.guo
// Date : 2023/06/25/12:31
// Ver : 1.0.0
// Description : UIWindow.cs
// ChangeLog :
// **********************************************

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;
using Object = UnityEngine.Object;

namespace MythMerge
{
    public abstract class UIWindow : UIBase
    {
        #region Propreties

        private System.Action<UIWindow> _prepareCallback;

        private bool _isCreate = false;
        internal Exception LoadError;
        internal bool IsOpened;

        private GameObject _panel;

        private Canvas _canvas;

        private Canvas[] _childCanvas;
        private int[] _childOriginDepths;

        private GraphicRaycaster _raycaster;

        private GraphicRaycaster[] _childRaycaster;

        public override UIBaseType BaseType => UIBaseType.Window;

        /// <summary>
        /// 窗口矩阵位置组件。
        /// </summary>
        public override RectTransform rectTransform => _panel.transform as RectTransform;

        /// <summary>
        /// 窗口的实例资源对象。
        /// </summary>
        public override GameObject gameObject => _panel;

        /// <summary>
        /// 窗口名称。
        /// </summary>
        public string WindowName { private set; get; }

        /// <summary>
        /// 窗口层级。
        /// </summary>
        public int WindowLayer { private set; get; }

        /// <summary>
        /// 资源定位地址。
        /// </summary>
        public string AssetName { private set; get; }

        /// <summary>
        /// 是否为全屏窗口
        /// </summary>
        public bool FullScreen { private set; get; }
        
        /// <summary>
        /// 强制显示
        /// </summary>
        public bool ForceShow { private set; get; }

        /// <summary>
        /// 自定义数据。
        /// </summary>
        public System.Object UserData
        {
            get
            {
                if (userDatas != null && userDatas.Length >= 1)
                {
                    return userDatas[0];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 自定义数据集。
        /// </summary>
        public System.Object[] UserDatas => userDatas;

        /// <summary>
        /// 窗口深度值
        /// </summary>
        public int Depth
        {
            get
            {
                if (_canvas != null)
                {
                    return _canvas.sortingOrder;
                }
                else
                {
                    return 0;
                }
            }

            set
            {
                if (_canvas != null)
                {
                    if (_canvas.sortingOrder == value)
                    {
                        return;
                    }

                    // 设置父类
                    _canvas.sortingOrder = value;

                    // 设置子类
                    int depth = value;
                    for (int i = 0; i < _childCanvas.Length; i++)
                    {
                        var canvas = _childCanvas[i];
                        if (canvas != _canvas)
                        {
                         //   depth += 5; //注意递增值
                            canvas.sortingOrder = _childOriginDepths[i] + depth;
                        }
                    }

                    // 虚函数
                    if (_isCreate)
                    {
                        OnSortDepth(value);
                    }
                }
            }
        }

        /// <summary>
        /// 窗口可见性
        /// </summary>
        public bool Visible
        {
            get
            {
                if (_canvas != null)
                {
                    return _canvas.gameObject.layer == UIModule.WINDOW_SHOW_LAYER;
                }
                else
                {
                    return false;
                }
            }

            set
            {
                if (_canvas != null)
                {
                    int setLayer = value ? UIModule.WINDOW_SHOW_LAYER : UIModule.WINDOW_HIDE_LAYER;
                    if (_canvas.gameObject.layer == setLayer)
                        return;

                    // 显示设置
                    _canvas.gameObject.layer = setLayer;
                    for (int i = 0; i < _childCanvas.Length; i++)
                    {
                        _childCanvas[i].gameObject.layer = setLayer;
                    }

                    // 交互设置
                    Interactable = value;

                    // 虚函数
                    if (_isCreate)
                    {
                        OnSetVisible(value);
                    }
                }
            }
        }

        /// <summary>
        /// 窗口交互性
        /// </summary>
        protected bool Interactable
        {
            get
            {
                if (_raycaster != null)
                {
                    return _raycaster.enabled;
                }
                else
                {
                    return false;
                }
            }

            set
            {
                if (_raycaster != null)
                {
                    _raycaster.enabled = value;
                    for (int i = 0; i < _childRaycaster.Length; i++)
                    {
                        _childRaycaster[i].enabled = value;
                    }
                }
            }
        }

        /// <summary>
        /// 是否加载完毕。
        /// </summary>
        internal bool IsLoadDone => Handle == null || Handle.IsDone;

        #endregion

        public void Init(string name, int layer, bool fullScreen, bool forceShow, string assetName)
        {
            WindowName = name;
            WindowLayer = layer;
            FullScreen = fullScreen;
            ForceShow = forceShow;
            AssetName = assetName;
        }

        internal void TryInvoke(System.Action<UIWindow> prepareCallback, System.Object[] userDatas)
        {
            base.userDatas = userDatas;
            if (IsPrepare)
            {
                prepareCallback?.Invoke(this);
            }
            else
            {
                _prepareCallback = prepareCallback;
            }
        }

        public virtual bool HasPreLoadAssets()
        {
            return false;
        }

        public virtual async UniTask PreLoadAssets()
        {
            
        }

        internal void InternalLoad(string location, System.Action<UIWindow> prepareCallback, System.Object[] userDatas)
        {
            if (Handle != null)
            {
                return;
            }

            _prepareCallback = prepareCallback;
            base.userDatas = userDatas;
            Handle = AssetModule.Instance.Package.LoadAssetAsync<GameObject>(location);
            Handle.Completed += Handle_Completed;
        }

        internal void InternalCreate()
        {
            if (_isCreate == false)
            {
                _isCreate = true;
                ScriptGenerator();
                BindMemberProperty();
                RegisterEvent();
                OnCreate();
                PlayOpenAni();
            }
        }

        public virtual string GetOpenAudioStr()
        {
            return string.Empty;
        }

        private async void PlayOpenAni()
        {
            var openAniName = GetOpenAniName();
            if (!string.IsNullOrEmpty(openAniName))
            {
                var openAudio = GetOpenAudioStr();
                if (!string.IsNullOrEmpty(openAudio))
                {
                    SoundModule.PlaySfx(openAudio);
                }
                var animator = gameObject.GetComponent<Animator>();
                if (animator != null && animator.HasState(openAniName))
                {
                    await MMUtility.PlayAnimationAsync(animator, openAniName,
                        gameObject.GetCancellationTokenOnDestroy());
                    OpenAniEndCallBack();
                }
            }
        }

        protected virtual string GetOpenAniName()
        {
            return "PopupAppear";
        }

        protected virtual void OpenAniEndCallBack()
        {
            
        }

        internal void InternalRefresh()
        {
            OnRefresh();
        }

        internal void InternalDestroy()
        {
            _isCreate = false;
            IsPrepare = IsOpened = false;

            CleanAllSubscribedEvents();

            for (int i = 0; i < ListChild.Count; i++)
            {
                var uiChild = ListChild[i];
                uiChild.OnDestroy();
                uiChild.OnDestroyWidget();
            }

            // 注销回调函数
            _prepareCallback = null;

            // 卸载面板资源
            if (Handle != null)
            {
                Handle.Release();
                Handle = null;
            }

            // 销毁面板对象
            if (_panel != null)
            {
                OnDestroy();
                
                _panel.gameObject.SetActive(true);
                
                Object.Destroy(_panel);
                _panel = null;
            }
        }

        /// <summary>
        /// 处理资源加载完成回调。
        /// </summary>
        /// <param name="handle"></param>
        /// <exception cref="System.Exception"></exception>
        private void Handle_Completed(AssetHandle handle)
        {
            if (Handle != handle) return;
            try
            {
            if (handle.AssetObject == null)
            {
                return;
            }

            // 实例化对象
            _panel = handle.InstantiateSync(new InstantiateOptions(true, UIModule.UIRootStatic, false));
            _panel.transform.localPosition = Vector3.zero;

            // 绑定引用
            AssetReference = AssetReference.BindAssetReference(_panel);

            // 获取组件
            _canvas = _panel.transform.Bind<Canvas>(true);
            _raycaster = _panel.transform.Bind<GraphicRaycaster>(true);
           
            if (_canvas == null)
            {
                throw new Exception($"Not found {nameof(Canvas)} in panel {WindowName}");
            }

            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 0;
            _canvas.sortingLayerName = "Default";

            // 获取组件
            _childCanvas = _panel.GetComponentsInChildren<Canvas>(true);
            _childRaycaster = _panel.GetComponentsInChildren<GraphicRaycaster>(true);

            _childOriginDepths = new int[_childCanvas.Length];
            
            for (var i = 0; i < _childOriginDepths.Length; i++)
            {
                _childOriginDepths[i] = _childCanvas[i].sortingOrder;
            }

            // 通知UI管理器
            IsPrepare = true;
            _prepareCallback?.Invoke(this);
            }
            catch (Exception error) { LoadError = error; }
        }


        protected virtual void Close()
        {
            UIModule.Instance.CloseWindow(this.GetType());
        }

        public virtual string GetCloseAnimation()
        {
            return "UIDisappear";
        }

        public virtual string GetCloseAudioStr()
        {
            return string.Empty;
        }

        public virtual async UniTask ShowCloseAnimation()
        {
            var closeAnimationName = GetCloseAnimation();
            if (!string.IsNullOrEmpty(closeAnimationName))
            {
                var closeAudio = GetCloseAudioStr();
                if (!string.IsNullOrEmpty(closeAudio))
                {
                    SoundModule.PlaySfx(closeAudio);
                }

                if (gameObject)
                {
                    var animator = gameObject.GetComponent<Animator>();
                    if (animator != null && animator.HasState(closeAnimationName))
                    {
                        await MMUtility.PlayAnimationAsync(animator, closeAnimationName,
                            gameObject.GetCancellationTokenOnDestroy());
                    }   
                }
                else
                {
                    Log.Error($"{WindowName} has destroyed!");
                }
            }
        }
    }
}
