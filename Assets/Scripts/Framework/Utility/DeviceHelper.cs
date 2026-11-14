// Author : xuejie.guo
// Adapted from TM5 Framework/Utils/DeviceHelper.cs.
using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MythMerge
{
    /// <summary>
    /// TM5 平台名称和数值，脱离原 SDK 的 Protobuf 协议类型。
    /// </summary>
    public enum Platform
    {
        Ios = 0,
        Facebook = 1,
        Amazon = 2,
        Google = 3,
        Wp8 = 4,
        UnityEditor = 5
    }

    /// <summary>
    /// 设备、应用、平台和屏幕信息工具。在 Unity 主线程使用。
    /// </summary>
    public class DeviceHelper
    {
        private static readonly string s_OSName = SystemInfo.operatingSystem;
        private static readonly string s_Resolution = Screen.currentResolution.ToString();
        private static readonly string s_SystemLanguage = Application.systemLanguage.ToString();
        private static readonly string s_AppVersion = Application.version;
        private static readonly int s_SystemMemorySize = SystemInfo.systemMemorySize;
        private static readonly string s_DeviceModel = SystemInfo.deviceModel;
        private static readonly string s_ProcessorType = SystemInfo.processorType;

        public static int s_mainOSVersion { get; private set; }
        public static int s_secondaryOSVersion { get; private set; }

        static DeviceHelper()
        {
            // Android 沿用 TM5 的 API 级别语义；其他平台解析操作系统版本。
            string pattern = Application.platform == RuntimePlatform.Android
                ? @"API-(\d+)(?:\.(\d+))?"
                : @"(\d+)(?:\.(\d+))?";
            Match match = Regex.Match(s_OSName ?? string.Empty, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                int.TryParse(match.Groups[1].Value, out int mainVersion);
                int.TryParse(match.Groups[2].Value, out int secondaryVersion);
                s_mainOSVersion = mainVersion;
                s_secondaryOSVersion = secondaryVersion;
            }
        }

        /// <summary>
        /// Unity 提供的设备标识；不依赖存档，也不保证卸载重装后保持不变。
        /// 未迁入旧 SDK 的 iOS Keychain 持久化。
        /// </summary>
        public static string GetDeviceId()
        {
            return SystemInfo.deviceUniqueIdentifier;
        }

        public static string GetAppVersion()
        {
            return s_AppVersion;
        }

        /// <summary>设备系统内存，单位 MB；不是当前可用内存。</summary>
        public static long GetTotalMemory()
        {
            return s_SystemMemorySize;
        }

        public static string GetDeviceModel()
        {
            return s_DeviceModel;
        }

        /// <summary>沿用 TM5 的语言回退值，不代表真实地区。</summary>
        public static string GetRegion()
        {
            return s_SystemLanguage;
        }

        /// <summary>沿用 TM5 的语言回退值，不代表真实国家。</summary>
        public static string GetCountry()
        {
            return s_SystemLanguage;
        }

        public static string GetOSName()
        {
            return s_OSName;
        }

        /// <summary>沿用 TM5：返回完整的 operatingSystem 字符串。</summary>
        public static string GetOSVersion()
        {
            return s_OSName;
        }

        public static string GetCPU()
        {
            return s_ProcessorType;
        }

        public static string GetTimezone()
        {
            try
            {
                return TimeZoneInfo.Local.StandardName;
            }
            catch
            {
                return "UNKNOWN";
            }
        }

        public static string GetPlatformString()
        {
#if UNITY_IOS
            return s_DeviceModel.Contains("iPad") ? "Ipad" : "Iphone";
#elif UNITY_ANDROID
#if SUB_CHANNEL_AMAZON
            return "Amazon";
#else
            return "Google";
#endif
#elif UNITY_FACEBOOK
            return "GameRoom";
#elif UNITY_WEBGL
            return "Facebook";
#else
            return "unity_editor";
#endif
        }

        public enum NetworkStatus
        {
            Unknown = 0,
            NoNetwork = 1,
            Cellular = 2,
            Wifi = 3
        }

        /// <summary>
        /// Unity 的网络可达类型，不能据此保证互联网或业务服务器可连接。
        /// Wifi 沿用 TM5 名称，也包含有线局域网。
        /// </summary>
        public static NetworkStatus GetNetworkStatus()
        {
            switch (Application.internetReachability)
            {
                case NetworkReachability.ReachableViaCarrierDataNetwork:
                    return NetworkStatus.Cellular;
                case NetworkReachability.ReachableViaLocalAreaNetwork:
                    return NetworkStatus.Wifi;
                case NetworkReachability.NotReachable:
                    return NetworkStatus.NoNetwork;
                default:
                    return NetworkStatus.Unknown;
            }
        }

        public static string GetDeviceType()
        {
#if UNITY_EDITOR
            return "UnityEditor";
#elif UNITY_ANDROID
            // 直接查询 Android 系统配置，不依赖 DragonNativeBridge 或自定义 Activity。
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var resources = activity.Call<AndroidJavaObject>("getResources"))
            using (var configuration = resources.Call<AndroidJavaObject>("getConfiguration"))
            {
                const int screenLayoutSizeMask = 0x0f;
                const int screenLayoutSizeLarge = 3;
                int size = configuration.Get<int>("screenLayout") & screenLayoutSizeMask;
                return size >= screenLayoutSizeLarge ? "tablet" : "phone";
            }
#elif UNITY_IOS
            return s_DeviceModel.Contains("iPad") ? "tablet" : "phone";
#else
            return "unknown";
#endif
        }

        // 以下平台判断保留 TM5 的 Editor 语义，Editor 返回 true。
        public static bool IsNative()
        {
#if UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID || UNITY_FACEBOOK
            return true;
#else
            return false;
#endif
        }

        public static bool IsIOSOrAndroid()
        {
#if UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID
            return true;
#else
            return false;
#endif
        }

        /// <summary>TM5 原接口也是空实现；当前没有接入深度链接参数服务。</summary>
        public static string GetSchemeParams()
        {
            return string.Empty;
        }

        public static Platform GetPlatform()
        {
#if UNITY_EDITOR
            return Platform.UnityEditor;
#elif UNITY_IOS
            return Platform.Ios;
#elif UNITY_ANDROID
#if SUB_CHANNEL_AMAZON
            return Platform.Amazon;
#else
            return Platform.Google;
#endif
#elif UNITY_FACEBOOK || UNITY_WEBGL
            return Platform.Facebook;
#else
            return Platform.Wp8;
#endif
        }

        public static ulong CurrentTimeMillis()
        {
            var epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (ulong)(DateTime.UtcNow - epochStart).TotalMilliseconds;
        }

        /// <summary>工具类首次使用时的显示器分辨率快照，不是实时游戏窗口尺寸。</summary>
        public static string GetResolution()
        {
            return s_Resolution;
        }

        public static string GetLanguage()
        {
            return s_SystemLanguage;
        }

        /// <summary>TM5 原接口也是占位值；当前不查询实际 IP 地址。</summary>
        public static string GetLocalIp()
        {
            return "0.0.0.0";
        }

        // 沿用 TM5 的机型列表与宽高比判定，非通用刘海/安全区检测。
        public static bool IsFullScreenIOS()
        {
#if UNITY_IOS && !UNITY_EDITOR
            var generation = UnityEngine.iOS.Device.generation;
            if (generation == UnityEngine.iOS.DeviceGeneration.iPhoneX ||
                generation == UnityEngine.iOS.DeviceGeneration.iPhoneXR ||
                generation == UnityEngine.iOS.DeviceGeneration.iPhoneXS ||
                generation == UnityEngine.iOS.DeviceGeneration.iPhoneXSMax ||
                generation == UnityEngine.iOS.DeviceGeneration.iPhone11 ||
                generation == UnityEngine.iOS.DeviceGeneration.iPhone11Pro ||
                generation == UnityEngine.iOS.DeviceGeneration.iPhone11ProMax ||
                generation.ToString().Contains("iPhone12") ||
                generation.ToString().Contains("iPhone13") ||
                generation.ToString().Contains("iPhone14") ||
                (generation == UnityEngine.iOS.DeviceGeneration.iPhoneUnknown && GetScreenWHRate() >= 2.0f))
            {
                return true;
            }
#endif
            return false;
        }

        public static bool IsFullScreenIOS_Portrait()
        {
#if UNITY_IOS && !UNITY_EDITOR
            var generation = UnityEngine.iOS.Device.generation;
            if (generation == UnityEngine.iOS.DeviceGeneration.iPhoneX ||
                generation == UnityEngine.iOS.DeviceGeneration.iPhoneXR ||
                generation == UnityEngine.iOS.DeviceGeneration.iPhoneXS ||
                generation == UnityEngine.iOS.DeviceGeneration.iPhoneXSMax ||
                generation == UnityEngine.iOS.DeviceGeneration.iPhone11 ||
                generation == UnityEngine.iOS.DeviceGeneration.iPhone11Pro ||
                generation == UnityEngine.iOS.DeviceGeneration.iPhone11ProMax ||
                generation.ToString().Contains("iPhone12") ||
                (generation == UnityEngine.iOS.DeviceGeneration.iPhoneUnknown && GetScreenWHRate() <= 0.5f))
            {
                return true;
            }
#endif
            return false;
        }

        public static bool IsWideScreenDevice()
        {
            return Screen.height > 0 && GetScreenWHRate() <= 1.5f;
        }

        public static float GetScreenWHRate()
        {
            return Screen.height > 0 ? (float)Screen.width / Screen.height : 0f;
        }

        public static string GetUserAgent()
        {
            return Application.productName + "/" + GetPlatform() + "/" + GetAppVersion();
        }
    }
}
