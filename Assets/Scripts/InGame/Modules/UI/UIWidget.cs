using MythMerge.Core;
using MythMerge.LauncherEngine;
// **********************************************
// 
// Author : xuejie.guo
// Date : 2023/06/25/12:29
// Ver : 1.0.0
// Description : UIWidget.cs
// ChangeLog :
// **********************************************

using System.Collections.Generic;
using UnityEngine;

namespace MythMerge
{
    public enum UIWidgetPriorityRight { None }
    public enum UIWidgetPriorityLeft { None }
    public abstract class UIWidget:UIBase,IUIBehaviour
    {
        /// <summary>
        /// 窗口组件的实例资源对象。
        /// </summary>
        public override GameObject gameObject { protected set; get; }
        
        /// <summary>
        /// 窗口组件矩阵位置组件。
        /// </summary>
        public override RectTransform rectTransform { protected set; get; }

        /// <summary>
        /// 窗口组件名称。
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public string name { private set; get; } = nameof(UIWidget);
        
        /// <summary>
        /// UI类型。
        /// </summary>
        public override UIBaseType BaseType => UIBaseType.Widget;
        public virtual int GetRightWidgetPriority()
        {
            return (int)UIWidgetPriorityRight.None;
        }
        public virtual int GetLeftWidgetPriority()
        {
            return (int)UIWidgetPriorityLeft.None;
        }
        
        /// <summary>
        /// 所属的窗口。
        /// </summary>
        public UIWindow OwnerWindow
        {
            get
            {
                var parentUI = base.parent;
                while (parentUI != null)
                {
                    if (parentUI.BaseType == UIBaseType.Window)
                    {
                        return parentUI as UIWindow;
                    }

                    parentUI = parentUI.Parent;
                }

                return null;
            }
        }

        public virtual void Show()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        public virtual void Hide()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        #region Create
        /// <summary>
        /// 创建窗口内嵌的界面。
        /// </summary>
        /// <param name="parentUI">父节点UI。</param>
        /// <param name="widgetRoot">组件根节点。</param>
        /// <param name="visible">是否可见。</param>
        /// <returns></returns>
        public bool Create(UIBase parentUI, GameObject widgetRoot, bool visible = true)
        {
            return CreateImp(parentUI, widgetRoot, false, visible);
        }
        
        /// <summary>
        /// 根据prefab或者模版来创建新的 widget。
        /// </summary>
        /// <param name="parentUI"></param>
        /// <param name="goPrefab"></param>
        /// <param name="parentTrans"></param>
        /// <param name="visible"></param>
        /// <returns></returns>
        public bool CreateByPrefab(UIBase parentUI, GameObject goPrefab, Transform parentTrans, bool visible = true)
        {
            if (parentTrans == null)
            {
                parentTrans = parentUI.rectTransform;
            }
            return CreateImp(parentUI, Object.Instantiate(goPrefab, parentTrans), true, visible);
        }

        private bool CreateImp(UIBase parentUI, GameObject widgetRoot, bool bindGo, bool visible = true)
        {
            if (!CreateBase(widgetRoot, bindGo))
            {
                return false;
            }
            AssetReference = AssetReference.BindAssetReference(widgetRoot,parent:parentUI.AssetReference);
            RestChildCanvas(parentUI);
            parent = parentUI;
            Parent.ListChild.Add(this);
            if (GetRightWidgetPriority() != (int)UIWidgetPriorityRight.None)
                Parent.ListChildRight.Add(this);
            if (GetLeftWidgetPriority() != (int)UIWidgetPriorityLeft.None)
                Parent.ListChildLeft.Add(this);

            gameObject.SetActive(visible);

            ScriptGenerator();
            BindMemberProperty();
            RegisterEvent();
            OnCreate();
            IsPrepare = true;
            return true;
        }
        
        protected bool CreateBase(GameObject go, bool bindGo)
        {
            if (go == null)
            {
                return false;
            }
            gameObject = go;
            rectTransform = go.GetComponent<RectTransform>();
            Log.Assert(rectTransform != null, $"{go.name} ui base element need to be RectTransform");
            return true;
        }
        
        protected void RestChildCanvas(UIBase parentUI)
        {
            Canvas parentCanvas = parentUI.gameObject.GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                return;
            }

            if (gameObject != null)
            {
                var listCanvas = gameObject.GetComponentsInChildren<Canvas>(true);
                for (var index = 0; index < listCanvas.Length; index++)
                {
                    var childCanvas = listCanvas[index];
                    childCanvas.sortingOrder = parentCanvas.sortingOrder + childCanvas.sortingOrder % UIModule.WINDOW_DEEP;
                }
            }
        }
        #endregion

        #region Destroy

        public override void OnDestroy()
        {
            for (int i = 0; i < ListChild.Count; i++)
            {
                var uiChild = ListChild[i];
                uiChild.OnDestroy();
                uiChild.OnDestroyWidget();
            }
            base.OnDestroy();
        }

        /// <summary>
        /// 组件被销毁调用。
        /// <remarks>请勿手动调用！</remarks>
        /// </summary>
        public void OnDestroyWidget()
        {
            
            CleanAllSubscribedEvents();
            
            if (Handle != null)
            {
                if (AssetReference != null && AssetReference.Parent != null)
                {
                    AssetReference.Parent.Release(Handle);
                }
            }
            
        }
        

        #endregion
    }
}
