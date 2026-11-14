# MythMerge（MM1）框架

启动场景：`Assets/Scenes/MythMergeLancher.unity`，保留此拼写。操作 Editor 优先使用已安装的 Unity CLI。

按用户本轮调整的目录，从本地 TripleMatch5（TM5）迁入通用源码，保留单例、窗口/Widget、事件、调度及 LauncherFsm 的组织方式。参考项目只读；不迁入玩法、活动、装修、登录、广告、支付、商业 SDK、旧服务器或美术资源。原有对象池保留。存档按后续明确要求切换为 TM5 的本地存储，原文件存档实现已归档。

## 目录与程序集

```text
Assets/Scripts/
├─ Framework/                         MM1.Framework.dll（AOT）
│  ├─ Core/                           Singleton、IModule、SDK 服务注册容器
│  ├─ Log/                            TM5 日志及日志辅助
│  ├─ Exception/                      GameFrameworkException
│  ├─ Storage/                       TM5 本地存档：StorageManager、Base、Utility
│  ├─ MemoryPool/                     IMemory、MemoryPool、MemoryPoolInfo
│  ├─ Utility/                        Assembly、Json、Text、Unity
│  ├─ Fsm/                            IFsmManager、IFsm<T>、FsmState<T> 等
│  ├─ DataStruct/                     TypeNamePair
│  └─ ViewResolution.cs
├─ Launcher/                          MM1.Launcher.dll（AOT）
│  ├─ MythLauncher.cs                 原 TM5 MatchVillaLauncher
│  ├─ GlobalSetting.cs
│  ├─ LocaleHelper.cs
│  ├─ ResServerSelector.cs
│  ├─ LauncherFsm/                    原七个启动状态
│  ├─ Config/                         LauncherSettings、AOTAssemblyList
│  └─ Editor/                         独立 Editor-only 程序集
└─ InGame/                            MM1.HotUpdate.dll（热更）
   ├─ Modules/
   │  ├─ App/                         GameApp、LogicSys
   │  ├─ Asset/                       AssetModule、AssetGroup、AssetReference
   │  ├─ SoundModule/
   │  ├─ UI/                          UIBase、UIWindow、UIWidget 等
   │  ├─ UpdateScheduler/
   │  ├─ Pool/                        现有对象池
   │  ├─ Events/                      通用窗口、存档、生命周期事件
   │  ├─ Utility/                     UI 扩展和 MMUtility
   │  ├─ UIBinder.cs
   │  ├─ EventBus.cs
   │  └─ IEventHandlerScheduler.cs
   └─ GamePlay/                       留给业务代码
```

依赖：Launcher → Framework；InGame → Launcher + Framework。Launcher 没有编译引用热更类型；没有 Contracts、GameEntry。Framework 按用户本轮已有的独立程序集保留为 AOT，其修改需要重新出主包。InGame 的模块和业务一起热更。

## 启动

```text
MythLauncher.Start
  → MatchQuality → InitEssential → StartLaunchProcedure
  → ResServerSelector（开发开关开启时）
  → MMLauncherFsm.Initialize / Start
  → MMFsmStateSetUpViewResolution
  → MMFsmStateShowSplash
  → MMFsmStateFetchVersionInfo
  → MMFsmStateInitializeResources
  → MMFsmStateCheckAndUpdateManifest
  → MMFsmStateLoadAssembly
  → MMFsmStateLaunchGame
  → 反射 MythMerge.GameApp.Entrance(object[])
```

LauncherSettings 通过 Instance 从 Resources/Settings/LauncherSettings 加载，沿用 TM5 ConfigurationController 的 ScriptableObject 单例和配置分组。配置资产移动后原 GUID 保留。

MythLauncher.Update 驱动原 TM5 FsmManager，保留 OnEnter/OnUpdate/OnLeave 和 ChangeState<T>。入口返回 Task，参数依次为 LauncherSettings、已准备的 ResourcePackage 和 CancellationToken。热更模块借用包，退出先 GameApp.Exit 归还句柄，Launcher 随后销毁包。元数据或热更 DLL 已加载后需重启进程才能更换。

场景原有 Launcher 脚本 GUID 随移动保留。启动生成通用 Root/UIRoot/UICanvas、UICamera、EventSystem 和 SoundContainer，不含业务 UI。可选 Splash 路径为 Resources/Launcher/UILoading，目前没有迁入原美术 prefab，缺失时跳过展示。

## 配置与服务器预留

- 配置：Assets/Resources/Settings/LauncherSettings.asset；默认 EditorSimulate，资源包 DefaultPackage，可直接进入 Play Mode。
- 收集器收集 Assets/AssetInApp，文件名寻址、按目录打包，标签 BuildIn。地址应唯一。FrameworkVersion.txt 是最小验证资源；AssetFromServer 暂未加入收集器。
- ShowServerSelector 默认关闭；开启后使用 Resources/Prefabs/ServerOption.prefab，Content/ToggleGroup 和 Content/ConfirmButton 路径沿用 TM5。
- ResourceServerUrls 默认预留 http://127.0.0.1:8080。资源选择器沿用 TM5 写法，确认后将 LauncherSettings.Instance.version 设为 DEBUG，并更新 Res_Server_URL_Beta；不会自动切换 YooAsset PlayMode。
- Host 模式通过 ResServerURL / ResServerFallbackURL 读取当前环境的资源地址。当前未搭建 HTTP 服务、发布资源包或验证联网下载。真机模拟时应改成电脑局域网地址和对应平台资源目录。
- APIServerURL 默认空，跳过可选的业务版本接口，仍由 YooAsset 请求包版本和加载清单；接口超时读取当前环境的 APIServerTimeout。

YooAsset 适配已安装的 3.0.7 原生 API：ResourcePackage 实例、AssetHandle、InstantiateOptions、InitializePackageOptions、LoadPackageManifestAsync、CustomAsyncOperation；未开启 YOOASSET_LEGACY_API。OpenWindowOperation 仍由 YooAssets.StartOperation 驱动。

## 模块约定

- 日志入口 MythMerge.LauncherEngine.Log，MythLauncher 注册 TEngineLogHelper，沿用 TM5 的 ENABLE_LOG 等编译宏。
- Utility.Unity 提供 Unity 生命周期驱动；热更中的 UpdateScheduler 注册，关闭时解除。Utility.Json 保留 TM5 IJsonHelper 注入接口，尚未指定辅助实现；存档的序列化和反序列化仍直接使用 Newtonsoft.Json。
- 事件按本次要求使用 MythMerge.EventBus、IEvent、IEventHandlerScheduler。Subscribe 返回 Listener，用 UnSubscribe 解除；旧 MEvent 名称不再使用。
- UI 使用普通 C# UIWindow/UIWidget、Window/WidgetAttribute、UIBinderAttribute 和 ComponentBinder.BindingComponent，保留窗口排序、全屏遮挡、Widget 和事件清理。
- 去掉 UIWaiting、ActivitySys、具体活动 Widget、旧玩法窗口判断和活动优先级枚举项。
- SoundModule 沿用 TM5 BGM/音效通道。SoundEnabled 替代旧业务 ConfigSys，按钮和切换音效地址默认空，待业务配置。
- GameObject 实例由 AssetReference 持有句柄；AssetGroup 随所有者释放。不要在实例仍被使用时释放所属组。
- 存档已改为 Framework/Storage 下的 TM5 StorageManager，本地调用为 StorageManager.Instance.Init / GetStorage<T> / SaveToLocal / Release。
- 保留 JSON → Rijndael 加密 → Base64 → PlayerPrefs 的本地格式；默认键为 MM1.StorageData、MM1.StorageVersion、MM1.RunOnce。PlayerPrefs.Save 在保存时调用。
- StorageBase、StorageList、StorageDictionary、MemoryInt 及必要的 Manager/RijndaelManager 依赖一并迁入，命名空间仍为 MythMerge.Save。具体业务 Storage 类放在 InGame/GamePlay，由 GameApp 反射收集并注册。
- 属性 setter 沿用 TM5 的 LocalVersion++；集合会通知变化，SyncForce 请求下一帧保存。本地间隔继续读取 LauncherSettings.LocalSaveInterval；暂停、退出及 GameApp 关闭时保存。
- 不含云端上传、拉取、冲突处理、远端版本、账号、API、埋点、归因数据或远端同步标记；也没有迁入 StorageCommon 等 SDK 数据模型。
- MM1 使用独立的固定加密参数，存放在 Framework/Storage/Utility/RijndaelManager.cs。已有存档发布后不要随意更换参数；客户端本地加密不等同于服务端防作弊。
- 原 InGame/Modules/Storage 的 UserProfile/Storage/IStorage 已移出编译，旧 profile.json 文件不删除、不自动转换。原来的文件校验、备份恢复机制不属于当前 TM5 本地实现。
- ClearAll 仅清理本存档数据，不调用 PlayerPrefs.DeleteAll。重复 FromJson 不追加已有集合；损坏数据读取失败会阻止初始化及后续自动覆盖。

## 构建与检查

Tools/MythMerge/Create Framework Setup 补建缺失的场景、配置和收集器，不覆盖现有场景。Hot Update/Configure Assembly 将 InGame 配置为唯一热更程序集。Compile Active Target 编译热更 DLL；Export Player Content 要先为目标 IL2CPP 主包完成 HybridCLR 安装和 Generate/All，再导出与主包裁剪基线匹配的元数据并打资源包。

本轮源码映射：../AIProduction/tools/mythmerge-framework/tm5-framework-module-source-map.json。修改前快照：../AIProduction/tools/mythmerge-framework/history/10-before-tm5-framework-modules/。CLI 验证脚本与结果位于 ../AIProduction/tools/mythmerge-framework/；验证资源移出 Assets，验证存档使用 Library 独立目录。

官方 API 源码：<https://github.com/tuyoogame/YooAsset/tree/3.0.7/Assets/YooAsset>。编辑器模拟通过不等于真机 HybridCLR 元数据加载、原生 IL2CPP 构建或服务器下载通过，后续接入主包和资源服务器时再验证。

## TM5 本地存档迁移记录

参考来源：TripleMatch5/Assets/DragonSDK/DragonU3DSDK/Framework/Storage，以及该 Framework 下的 Manager/Manager.cs 和 Utils/RijndaelManager.cs。本地流程按原代码裁剪，未接入整个 DragonSDK。

替换前快照：../AIProduction/tools/mythmerge-framework/history/11-before-tm5-local-storage/。来源映射：../AIProduction/tools/mythmerge-framework/tm5-local-storage-source-map.json。本轮验证：VerifyTM5LocalStorage.cs 及 results/tm5-local-storage-runtime-01.json。测试使用独立 PlayerPrefs 前缀，并在结束时清理测试键。

## LauncherSettings 配置

参考 TM5 DragonU3DSDK/Framework/Configuration/ConfigurationController.cs，类名保持 LauncherSettings，资源位置为 Assets/Resources/Settings/LauncherSettings.asset。使用 LauncherSettings.Instance 获取配置；运行时不会自动创建配置资产。

- version 使用 VersionStatus.RELEASE / DEBUG / LOCAL；按 TM5 原逻辑，RELEASE 选正式字段，DEBUG 和 LOCAL 都选 Beta 字段。
- APIServerURL / APIServerTimeout 读取 API_Server_URL_Release/Beta、API_Server_Timeout_Release/Beta。
- ResServerURL / ResServerFallbackURL 读取 Res_Server_URL_Release/Beta、Res_Server_Fallback_URL_Release/Beta。
- BuildAppBundle 读取 ReleaseBuildAppBundle / DebugBuildAppBundle。这些字段当前是配置入口，尚未接入自动构建 AppBundle 的流程。
- PackageName、PlayMode、LocalSaveInterval、ShowServerSelector 和 ResourceServerUrls 保留。环境选择和 YooAsset 运行模式是两项独立配置。
- 旧 DefaultHostServer、FallbackHostServer、VersionCheckUrl 字段通过 FormerlySerializedAs 迁移到 Beta 配置；正式地址为空，测试资源地址默认本机 8080，API 地址为空。
- HotAssemblyName 常量集中在 LauncherSettings，值仍为 MM1.HotUpdate。GlobalSetting.Settings 直接转发 Instance，资源初始化、选择器和热更内部使用同一份配置。
- 保留用户后来添加的 StorageCommon 语言字段：Launcher 先只读初始化，GameApp 完整注册时保留该实例。未恢复旧账号、广告归因或云存档数据模型。
- 资源选择器按 TM5 修改 ScriptableObject 实例中的 Beta 地址；开发时注意 Unity Editor 中 ScriptableObject 的运行时修改。验证脚本通过克隆配置隔离修改，不改变持久化配置资产。

变更前快照：../AIProduction/tools/mythmerge-framework/history/12-before-launcher-settings-controller/。配置与启动验证：VerifyLauncherSettings.cs，结果为 results/launcher-settings-runtime-01.json。

## DeviceHelper

按用户要求，仅从 TM5 的 Framework/Utils/DeviceHelper.cs 迁入 DeviceHelper，不迁入同文件中的返回键 Device 组件。位置为 Assets/Scripts/Framework/Utility/DeviceHelper.cs，命名空间 MythMerge，编入 MM1.Framework AOT，Launcher 和 InGame 都可以访问。

- 沿用静态调用：DeviceHelper.GetPlatform()、GetAppVersion()、GetDeviceModel()、GetTotalMemory()、GetNetworkStatus()、CurrentTimeMillis() 等。设备与应用信息多数为首次访问时的快照；内存单位 MB。应在 Unity 主线程使用。
- Platform 改为本地枚举，保留 TM5 的 Ios/Facebook/Amazon/Google/Wp8/UnityEditor 名称和数值，不依赖旧 Protobuf 协议。GetPlatform() 的 Editor 分支优先，返回 UnityEditor；GetPlatformString() 则沿用 TM5 的构建平台字符串规则，Android 构建目标的 Editor 中可能返回 Google。
- GetDeviceId() 直接读取 SystemInfo.deviceUniqueIdentifier，不创建存档、不写 PlayerPrefs、不接入旧账号或 iOS Keychain；不保证卸载重装后标识不变。
- GetNetworkStatus() 直接读取 Application.internetReachability，无 APIManager 依赖；Wifi 也包含有线局域网，这不是服务器连通性测试。GetDeviceType() 通过 Unity/Android 系统配置区分 UnityEditor、phone、tablet，无 DragonNativeBridge 或自定义 Activity 依赖。
- 保留 TM5 的部分兼容语义：GetRegion()/GetCountry() 实际返回系统语言；GetOSVersion() 返回完整系统字符串；GetSchemeParams() 为空；GetLocalIp() 为占位的 0.0.0.0；IsNative()/IsIOSOrAndroid() 在 Editor 中为 true。旧 iOS 全面屏方法保留原机型列表和宽高比判断，不是通用安全区检测。
- 未迁入 CancelNotification()/ScheduleLocalNotification() 及其 Activity 桥接；当前项目没有对应原生通知实现。需要通知时应单独接入可用的通知模块，不能直接调用旧接口。
- 修正 OS 版本解析为 TryParse，避免异常格式导致工具类初始化失败；屏幕宽高比在高度为零时返回 0。

原文件副本、SHA256 来源映射和迁移记录位于 ../AIProduction/tools/mythmerge-framework/。VerifyTM5DeviceHelper.cs 通过已安装的 Unity CLI 执行独立源码的 8 组条件编译和 Editor 检查，结果为 results/tm5-device-helper-01.json。这不代表移动端构建或真机验证通过。迁移前项目已有 30 个 Launcher 编译错误，整项目编译结果单独记录于 results/tm5-device-helper-project-compile-01.json。

## 资源包加密（YooAsset 3.0.7）

按用户要求保留 TM5 资源加密。实际文件名仍为 Assets/Scripts/Launcher/HotResouceServices.cs，保留原 .meta/GUID。GameDecryptionServices 改为 IBundleMemoryDecryptor，移除无实际行为的旧 DeliveryQueryServices。原生 App 从 YooAsset 提供的 FilePath 读取密文；Web/小游戏必须提供 FileData，不执行系统文件读取或 AssetBundle.LoadFromStream。

- 保留 TM5 密文格式：UTF-8 密钥字节倒序、AES-CFB128、PKCS7、Key 同时作为 IV。显式指定 BlockSize/FeedbackSize 为 128，密钥必须恰好为 16 个 UTF-8 字节。旧的 16 字节文件偏移接口不属于实际流加密格式，已移除。
- 新密钥入口为 LauncherSettings._AesKey。默认留空，不生成密钥，也不复制 TM5 的真实密钥；打包和运行时必须配置相同的值，已发布资源不能单方面换密钥。此字段独立于本地存档 RijndaelManager 的固定参数，不得混用。
- 打包端已加入 MythMerge.Editor.GameEncryptionServices : IBundleEncryptor。配置密钥后，在 YooAsset 构建窗口的资源包加密器选项选择此类；使用 LZ4 打包。该类与运行时共用算法，构建输出会被标记为加密。EditorSimulate 不验证实际资源加密。
- Launcher 和 AssetModule 的独立初始化入口共用 HotResourceServices.CreateHostOptions/CreateOfflineOptions，给内置及缓存/远端文件系统注册 AssetBundle、RawBundle、ArchiveBundle 的解密器。未标记为加密的资源仍由 YooAsset 正常加载。
- Web 构建使用 WebPlayModeOptions 和 WebServer/WebNetwork 文件系统，关闭 Unity 默认 Web 缓存，并将 BundleLoadingMaxConcurrency 设为 1，降低并发解密的内存峰值。小游戏 SDK、下载后端、平台缓存、域名与发布工具仍需按各平台接入；这些代码不代表已经完成小游戏发布适配。原生 Android 使用 HostPlayModeOptions 与本地沙盒缓存。
- 这是整包 AES 解密，密文、明文与 Unity 内部 Bundle 数据可能同时占用内存，并在解密期间占用执行线程。建议小游戏拆分较小 Bundle，并在实际机型测量峰值内存与耗时；不能声称对大资源包性能友好。此路径没有接入 WXAssetBundle/TTAssetBundle 的按需读文件优化。
- 保留旧格式也保留其固定 IV、无认证的限制；客户端内置密钥的资源保护不等同于防破解或真实性认证。若以后升级为随机 IV/认证加密，需要版本化资源格式并重新打包，不能仅替换运行时算法。

验证脚本：../AIProduction/tools/mythmerge-framework/VerifyHotResourceEncryption.cs。结果 results/hot-resource-encryption-01.json 包含 6 组条件编译、TM5 字节级兼容、边界长度、异常数据、配置挂接与打包加密器检查，共 177 项通过。测试仅使用公开的临时测试密钥、独立 ScriptableObject 和 Library/FrameworkValidation/Crypto-*，不修改真实配置/存档。真实 LZ4 Bundle 构建返回失败；当前项目仍有 25 个既有 Launcher 编译错误（Token/Fail 等缺失），结果记录于 results/hot-resource-encryption-project-compile-01.json。尚未完成 IL2CPP/WASM 构建或微信、抖音、Google Play Android 真机测试。

参考：[YooAsset Web 加密资源实现](https://github.com/tuyoogame/YooAsset/blob/3.0.7/Assets/YooAsset/Runtime/BundleCache/Operations/Common/LoadWebEncryptedAssetBundleOperation.cs)、[Unity Web AssetBundle 限制](https://docs.unity.com/en-us/engine/6000.0/manual/platform-specific/webgl/building-distribution/assetbundles)、[抖音 TTAssetBundle 内存说明](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/guide/performance-optimization-both/unity/startup/sc-webgl-tt-ab)。
