# Unity framework conventions

- Project short code: MM1. Preserve Assets/Scenes/MythMergeLancher.unity exactly.
- Read README.md first. Prefer the installed Unity CLI and connected Editor commands.
- Preserve the user's current layout: Assets/Scripts/Framework is MM1.Framework AOT, Launcher is MM1.Launcher AOT, InGame is MM1.HotUpdate. Launcher must not compile-reference hot types. InGame/Modules and InGame/GamePlay compile together.
- Latest reference is local TripleMatch5 for Framework Log/Exception/MemoryPool/Utility, Modules Asset/SoundModule/UI/UpdateScheduler/UIBinder/EventBus/IEventHandlerScheduler, and Launcher. Preserve the source's class organization and calling style; do not redesign without a user request.
- MythLauncher comes from TM5 MatchVillaLauncher. Keep its seven-state LauncherFsm, IFsmManager, IFsm<T>, FsmState<T>, Initialize/Start/Update and ChangeState<T>. No custom async state runner, Contracts or GameEntry.
- Reflect public static Task MythMerge.GameApp.Entrance(object[]) with LauncherSettings, prepared ResourcePackage and CancellationToken. Bind Task GameApp.Exit() for cleanup. Hot modules return handles before the Launcher destroys its package.
- Event API is MythMerge.EventBus, IEvent, IEventHandlerScheduler and EventBus.Listener. The latest explicit request supersedes the old MEvent naming.
- Keep gameplay, activities, monetization, SDKs and old-game art out of the generic framework. Preserve the existing generic object pool. The latest storage choice is the local-only TM5 implementation under Framework/Storage; do not restore the removed file-based UserProfile/Storage/IStorage stack.
- Use native YooAsset 3.x without the legacy define; retain resource handles for their owners' lifetimes.
- Retain resource bundle encryption per user direction. Runtime GameDecryptionServices uses YooAsset 3 IBundleMemoryDecryptor; keep the TM5 AES-CFB128/PKCS7 wire format, reversed 16-byte UTF-8 key and IV semantics unless a format migration is requested. Resource keys use LauncherSettings._AesKey, independent of storage encryption. Never copy reference-project production keys. Web receives bytes through its backend; do not reintroduce OS-file or stream-only Web decryption.
- Server selector uses LauncherSettings URLs; localhost is a placeholder. Do not copy old corporate servers or deploy a server without a task requesting it.
- E:/BaiduNetdiskDownload/MergeScrew2 and E:/BaiduNetdiskDownload/TripleMatch5 are read-only. Preserve existing asset GUIDs and source maps.
- Create scenes, prefabs and serialized assets with Unity APIs, never handwritten YAML.
- Author: xuejie.guo. Old company declarations were removed per user direction; preserve third-party package notices.
- Validate with CLI and isolated fixture/profile directories. Tools, snapshots and results live in ../AIProduction/tools/mythmerge-framework/.
- Native hot loading retains YooAsset MM1.HotUpdate.dll and generated Resources/AOTMeta/AssemblyList.json plus baseline-matched metadata. Do not restore custom SHA manifest protocols.

- Storage uses MythMerge.Save.StorageManager in MM1.Framework AOT. Preserve TM5 Init/GetStorage<T>/ReadFromLocal/SaveToLocal/FromJson/ToJson/Release style, local version tracking, encryption and PlayerPrefs. Concrete business StorageBase subclasses stay under InGame and are registered by GameApp.
- No remote profile synchronization, account/API/BI dependencies, legacy SDK account/attribution storage models or SyncForceRemote flags. Do not add these unless requested.
- Default local PlayerPrefs keys are MM1.StorageData, MM1.StorageVersion and MM1.RunOnce. Keep the fixed MM1 encryption parameters stable after shipping. Do not delete or silently reinterpret old profile.json files.
- Test local storage with an isolated ConfigureKeyPrefix and delete only the test's own keys. ClearAll must never use PlayerPrefs.DeleteAll.

- LauncherSettings follows TM5 ConfigurationController: ScriptableObject Instance loaded from Resources/Settings/LauncherSettings; VersionStatus.RELEASE selects production fields and DEBUG/LOCAL select Beta. Do not restore the removed DefaultHostServer/FallbackHostServer/VersionCheckUrl access style or separate GlobalSetting.ResourceServerOverride.
- LauncherSettings.HotAssemblyName is MM1.HotUpdate. Retain separate MMPlayMode, package name, local save interval and development server selection controls.
- Preserve the user's simplified MythLauncher. It initializes config/logging and read-only local StorageCommon, then starts the original FSM. Do not restore removed serialized settings fields or public diagnostic properties just to satisfy old validation scripts; inspect the FSM in tooling instead.
- StorageCommon was later added by the user for Locale/OriLocale; keep this local language storage and preserve its instance through AOT partial and hot full initialization. This does not authorize bringing back old account/BI models.
