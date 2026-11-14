using MythMerge.Core;
using MythMerge.LauncherEngine;
using System;

namespace MythMerge
{
    /// <summary>
    /// UI层级枚举。
    /// </summary>
    public enum UILayer:int
    {
        Bottom = 0,
        UI = 1,
        Top = 2,
        Tips = 3,
        System = 4,
        HighPriorityPopup = 5,
        Loading = 6,
    }
    
    [AttributeUsage(AttributeTargets.Class)]
    public class WindowAttribute : Attribute
    {
        /// <summary>
        /// 窗口层级
        /// </summary>
        public int WindowLayer;
        
        /// <summary>
        /// 资源定位地址。
        /// </summary>
        public string AssetName;

        /// <summary>
        /// 全屏窗口标记。
        /// </summary>
        public bool FullScreen;
        
        /// <summary>
        /// 强制显示
        /// </summary>
        public bool ForceShow;

        public WindowAttribute(int windowLayer,string assetName = "", bool fullScreen = false, bool forceShow=false)
        {
            WindowLayer = windowLayer;
            AssetName = assetName;
            FullScreen = fullScreen;
            ForceShow = forceShow;
        }
        
        public WindowAttribute(UILayer windowLayer,string assetName = "", bool fullScreen = false, bool forceShow=false)
        {
            WindowLayer = (int)windowLayer;
            AssetName = assetName;
            FullScreen = fullScreen;
            ForceShow = forceShow;
        }
    }
}
