// Author : xuejie.guo
// Description : 按 TM5 ConfigurationController 组织的启动配置。
using UnityEngine;
using UnityEngine.Serialization;
using YooAsset;

namespace MythMerge
{
    // public enum MMPlayMode { EditorSimulate, Offline, Host }

    public enum VersionStatus
    {
        RELEASE = 0,
        DEBUG = 1,
        LOCAL = 2
    }

    [CreateAssetMenu(menuName = "MythMerge/MM Settings")]
    public class LauncherSettings : ScriptableObject
    { 
        public static string LauncherSettingsPath = "Settings/GameSetting";

        private static LauncherSettings _instance;

        public static LauncherSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<LauncherSettings>(LauncherSettingsPath);
                }
                return _instance;
            }
        }

        public VersionStatus version = VersionStatus.DEBUG;

        public string APIServerURL
        {
            get { return version == VersionStatus.RELEASE ? API_Server_URL_Release : API_Server_URL_Beta; }
        }

        public int APIServerTimeout
        {
            get { return version == VersionStatus.RELEASE ? API_Server_Timeout_Release : API_Server_Timeout_Beta; }
        }

        public string ResServerURL
        {
            get { return version == VersionStatus.RELEASE ? Res_Server_URL_Release : Res_Server_URL_Beta; }
        }

        public string ResServerFallbackURL
        {
            get { return version == VersionStatus.RELEASE ? Res_Server_Fallback_URL_Release : Res_Server_Fallback_URL_Beta; }
        }

        public bool BuildAppBundle
        {
            get { return version == VersionStatus.RELEASE ? ReleaseBuildAppBundle : DebugBuildAppBundle; }
        }

        [Space(10)]
        [Header("API Server Prod")]
        public string API_Server_URL_Release = string.Empty;
        [Min(1)] public int API_Server_Timeout_Release = 15;

        [Space(10)]
        [Header("API Server Beta / Local")]
        [FormerlySerializedAs("VersionCheckUrl")]
        public string API_Server_URL_Beta = string.Empty;
        [Min(1)] public int API_Server_Timeout_Beta = 15;

        [Space(10)]
        [Header("线上资源服务器地址")]
        public string Res_Server_URL_Release = string.Empty;
        public string Res_Server_Fallback_URL_Release = string.Empty;

        [Space(10)]
        [Header("测试 / 本地资源服务器地址")]
        [FormerlySerializedAs("DefaultHostServer")]
        public string Res_Server_URL_Beta = "http://127.0.0.1:8080";
        [FormerlySerializedAs("FallbackHostServer")]
        public string Res_Server_Fallback_URL_Beta = string.Empty;

        [Space(10)]
        [Header("YooAsset")]
        public string PackageName = "DefaultPackage";
        public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;

        [Space(10)]
        [Header("资源包加密")]
        [Tooltip("TM5 兼容 AES-CFB128 密钥：必须为 16 个 UTF-8 字节，打包和运行时保持一致。留空时不能构建或加载加密资源。")]
        public string _AesKey = string.Empty;

        [Space(10)]
        [Header("本地存档")]
        [Min(0.1f)] public float LocalSaveInterval = 3;

        [Space(10)]
        [Header("开发环境资源服务器选择")]
        public bool ShowServerSelector;
        public string[] ResourceServerUrls = { "http://127.0.0.1:8080" };

        [Space(10)]
        [Header("Android Debug 是否打 AppBundle")]
        public bool DebugBuildAppBundle;

        [Space(10)]
        [Header("Android Release 是否打 AppBundle")]
        public bool ReleaseBuildAppBundle;
    }
}
