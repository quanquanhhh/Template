using MythMerge.Core;
using MythMerge.LauncherEngine;
using System;

namespace MythMerge
{
    public class WidgetAttribute : Attribute
    {
        /// <summary>
        /// 资源定位地址。
        /// </summary>
        public string AssetName;
        
        public WidgetAttribute(string assetName = "")
        {
            AssetName = assetName;
        }
    }
}
