// **********************************************
// 
// Author : xuejie.guo
// Date : 2023/06/25/11:12
// Ver : 1.0.0
// Description : GloabalSetting.cs
// ChangeLog :
// **********************************************

using YooAsset;

namespace MythMerge
{
    public class GlobalSetting
    { 
        //don't made any change to the follow config
        //config updated by app builder

        /// <summary>
        /// app 底包的版本号：例如 1.0.1
        /// </summary>
        protected static string version = "1.0.0";
        
        /// <summary>
        /// app 底包的版本代码：例如 3
        /// </summary>
        protected static string versionCode = "1";
        
        protected static string serverVersion = "v1";
        
        protected static string resourcesVersion = "v1";
        
        protected static string resourcesRootVersion = "rv1";
        
        protected static string hotfixStamp = "0";
        
#if UNITY_IOS
        public const string BundleFolderName = "ios";
#elif UNITY_ANDROID
        public const string BundleFolderName = "android";
#endif        
        public static string DefaultPackageName => LauncherSettings.Instance.PackageName;
        
        public static string FetchedRootVersion = "";
        
        
        public static string ResourceVersion
        {
            get { return resourcesVersion; }
            set { resourcesVersion = value; }
        }

        public static string HotfixStamp => hotfixStamp;
        public static string RootVersion => resourcesRootVersion;

        public static string VersionCode => versionCode;
        public static string Version => version;

        public static string GetHostResUrl()
        {
            return LauncherSettings.Instance.ResServerURL;
        }

        public static EPlayMode PlayMode
        {
            get
            {
#if UNITY_EDITOR
                return EPlayMode.EditorSimulateMode;
#else
                return EPlayMode.HostPlayMode;
#endif
            }
        }
    }
}
