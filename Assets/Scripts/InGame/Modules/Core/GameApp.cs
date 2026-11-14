// Author : xuejie.guo
using MythMerge.LauncherEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MythMerge.Core;
using MythMerge.Save;
using UnityEngine;

namespace MythMerge
{
    public class GameApp : Singleton<GameApp>
    {
        private readonly Dictionary<Type, IModule> _modules = new Dictionary<Type, IModule>();
        private readonly Dictionary<Type, LogicSys> _logicSystems = new Dictionary<Type, LogicSys>();
        private readonly List<IModule> _moduleOrder = new List<IModule>();
        private CancellationTokenSource _lifetime;
        private bool _initializing;
        public bool IsInitialized { get; private set; }
        public bool IsReady { get; private set; }
        public LauncherSettings Settings { get; private set; }

        // AOT reflects this method. Keep the argument order: settings, prepared package, lifetime token.
        public static async Task Entrance(object[] objects)
        {
            if (objects == null || objects.Length != 3 || !(objects[0] is LauncherSettings settings) || settings == null
                || !(objects[1] is YooAsset.ResourcePackage package) || !(objects[2] is CancellationToken token))
                throw new ArgumentException("Entrance expects settings, a prepared ResourcePackage and a CancellationToken.", nameof(objects));
            token.ThrowIfCancellationRequested();
            if (package.PackageName != settings.PackageName) throw new ArgumentException("The prepared package does not match LauncherSettings.", nameof(objects));
            if (package.InitializeStatus != YooAsset.EOperationStatus.Succeeded) throw new InvalidOperationException("The Launcher resource package is not initialized.");
            await Instance.InitializeCoreAsync(settings, package, token, Array.Empty<Assembly>());
        }

        // AOT binds this static shutdown method before invoking Entrance, including partial startup failures.
        public static async Task Exit() => await Instance.ShutdownAsync();

        public UniTask InitializeAsync(LauncherSettings settings, CancellationToken cancellationToken = default, params Assembly[] extraAssemblies)
            => InitializeCoreAsync(settings, null, cancellationToken, extraAssemblies);

        private async UniTask InitializeCoreAsync(LauncherSettings settings, YooAsset.ResourcePackage preparedPackage,
            CancellationToken cancellationToken, params Assembly[] extraAssemblies)
        {
            if (IsReady) return;
            if (_initializing) throw new InvalidOperationException("GameApp initialization is already in progress.");
            if (settings == null) throw new InvalidOperationException("Create Resources/Settings/LauncherSettings.asset before starting the framework.");
            _initializing = true;
            Settings = settings;
            _lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _lifetime.Token;
            try
            {
                var root = GameObject.Find("Root") ?? new GameObject("Root");
                UnityEngine.Object.DontDestroyOnLoad(root);
                var assemblies = new[] { typeof(GameApp).Assembly }.Concat(extraAssemblies ?? Array.Empty<Assembly>()).Distinct().ToArray();
                RegisterModule(assemblies);
                if (preparedPackage != null) AssetModule.Instance.SetDefaultPackage(preparedPackage);
                IsInitialized = true;
                StorageManager.Instance.LocalInterval = Mathf.Max(0.1f, settings.LocalSaveInterval);
                var storageTypes = GetConcreteTypes(assemblies, typeof(StorageBase));
                var common = StorageManager.Instance.Inited ? StorageManager.Instance.GetStorage<StorageCommon>() : new StorageCommon();
                var storages = new List<StorageBase> { common };
                storages.AddRange(storageTypes.Where(type => type != typeof(StorageCommon)).Select(type => (StorageBase)Activator.CreateInstance(type)));
                StorageManager.Instance.Init(storages);
                EventBus.Dispatch(new EventStorageFullInitialized());
                RegisterLogicSys(assemblies);
                foreach (var system in _logicSystems.Values) system.OnStart();
                if (preparedPackage == null) await AssetModule.Instance.InitializeAsync(settings, token);
                token.ThrowIfCancellationRequested();
                OnCoreResourceReady();
                foreach (var module in _moduleOrder) module.OnStartGameLogic();
                foreach (var system in _logicSystems.Values) system.OnLogicStart();
                IsReady = true;
                Log.Info("MythMerge framework ready.");
            }
            catch
            {
                OnShutDown();
                throw;
            }
            finally { _initializing = false; }
        }

        private void RegisterModule(Assembly[] assemblies)
        {
            // 保留反射注册与单例模块；显式确定基础模块依赖顺序。
            Type[] coreOrder = { typeof(UpdateScheduler), typeof(AssetModule), typeof(PoolModule), typeof(SoundModule), typeof(UIModule) };
            var types = coreOrder.Concat(GetConcreteTypes(assemblies, typeof(IModule)).Except(coreOrder));
            foreach (var type in types)
            {
                var property = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (property == null) throw new InvalidOperationException($"Module {type.FullName} must expose Singleton<T>.Instance.");
                var module = (IModule)property.GetValue(null);
                // 初始化中途失败时，同样纳入逆序清理。
                _modules.Add(type, module);
                _moduleOrder.Add(module);
                module.InitializeModule();
            }
        }
        private void RegisterLogicSys(Assembly[] assemblies)
        {
            foreach (var type in GetConcreteTypes(assemblies, typeof(LogicSys)))
            {
                var system = (LogicSys)Activator.CreateInstance(type);
                _logicSystems.Add(type, system);
                system.InitializeOnCreate();
            }
        }
        private static IEnumerable<Type> GetConcreteTypes(IEnumerable<Assembly> assemblies, Type baseType)
        {
            return assemblies.SelectMany(assembly => assembly.GetTypes())
                .Where(type => type != baseType && !type.IsAbstract && !type.IsInterface && !type.ContainsGenericParameters && baseType.IsAssignableFrom(type))
                .OrderBy(type => type.FullName, StringComparer.Ordinal);
        }
        public static T Get<T>() where T : LogicSys => Instance._logicSystems.TryGetValue(typeof(T), out var system) ? (T)system : null;
        public void OnCoreResourceReady() { foreach (var system in _logicSystems.Values) system.OnCoreAssetsReady(); }
        public void OnConfigLoadReady() { foreach (var system in _logicSystems.Values) system.OnConfigLoadReady(); }
        public void OnLoginSuccess() { foreach (var system in _logicSystems.Values) system.OnLoginSuccess(); }
        public void OnShutDown()
        {
            if (_moduleOrder.Count == 0 && !IsInitialized) return;
            IsReady = IsInitialized = false;
            _lifetime?.Cancel();
            foreach (var system in _logicSystems.Values.Reverse())
            {
                try { system.OnShutDown(); } catch (Exception error) { Debug.LogException(error); }
            }
            _logicSystems.Clear();
            for (int i = _moduleOrder.Count - 1; i >= 0; i--)
            {
                try { _moduleOrder[i].OnShutDown(); } catch (Exception error) { Debug.LogException(error); }
            }
            if (StorageManager.HasInstance)
            {
                try { StorageManager.Instance.Release(); } catch (Exception error) { Debug.LogException(error); }
            }
            _moduleOrder.Clear();
            _modules.Clear();
            EventBus.Reset();
            _lifetime?.Dispose();
            _lifetime = null;
        }
        public async UniTask ShutdownAsync()
        {
            OnShutDown();
            // 等待实例的 OnDestroy 归还资源句柄，再销毁包裹。
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            await AssetModule.Instance.ShutdownAsync();
        }
    }
}
