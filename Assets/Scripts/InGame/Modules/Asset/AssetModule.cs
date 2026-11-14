// **********************************************
// 
// Author : xuejie.guo
// Date : 2023/06/25/12:09
// Ver : 1.0.0
// Description : Asset.cs
// ChangeLog :
// **********************************************

using System.Reflection;
using Object = UnityEngine.Object;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MythMerge.LauncherEngine;
using MythMerge.Core;
using MythMerge;
using UnityEngine;
using UnityEngine.U2D;
using YooAsset;

namespace MythMerge
{
    public class AssetModule : Singleton<AssetModule>, IModule
    {
        public async UniTask InitializeAsync(LauncherSettings settings, CancellationToken cancellationToken = default)
        {
            if (IsReady) return;
            if (_initializing) throw new InvalidOperationException("Resource initialization is already running.");
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (Package != null) throw new InvalidOperationException("Await ShutdownAsync before initializing another resource package.");
            _initializing = true;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!YooAssets.IsInitialized) YooAssets.Initialize();
                if (YooAssets.TryGetPackage(settings.PackageName, out _))
                    throw new InvalidOperationException("Package already exists. Adopt the prepared package instead of initializing it twice.");
                var package = YooAssets.CreatePackage(settings.PackageName);
                Package = package;
                _ownsPackage = true;
                InitializePackageOptions options;
#if UNITY_EDITOR
                if (settings.PlayMode == EPlayMode.EditorSimulateMode )
                {
                    var result = EditorSimulateBuildInvoker.Build(settings.PackageName, (int)EBundleType.VirtualAssetBundle);
                    options = new EditorSimulateModeOptions
                    {
                        EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(result.PackageRootDirectory)
                    };
                }
                else
#endif
                if (settings.PlayMode == EPlayMode.HostPlayMode)
                {
                    if (string.IsNullOrWhiteSpace(settings.ResServerURL)) throw new InvalidOperationException("Host mode requires a resource server URL.");
                    options = HotResourceServices.CreateHostOptions(
                        new RemoteServices(settings.ResServerURL, settings.ResServerFallbackURL), settings._AesKey);
                }
                else
                {
                    options = HotResourceServices.CreateOfflineOptions(settings._AesKey);
                }
                var initialize = package.InitializePackageAsync(options);
                await initialize;
                CheckOperation(initialize);
                cancellationToken.ThrowIfCancellationRequested();
                var version = package.RequestPackageVersionAsync();
                await version;
                CheckOperation(version);
                cancellationToken.ThrowIfCancellationRequested();
                var manifest = package.LoadPackageManifestAsync(new LoadPackageManifestOptions(version.PackageVersion, 60));
                await manifest;
                CheckOperation(manifest);
                cancellationToken.ThrowIfCancellationRequested();
    
            }
            catch
            {
                if (Package != null)
                {
                    var failedPackage = Package;
                    await failedPackage.DestroyPackageAsync();
                    YooAssets.RemovePackage(failedPackage.PackageName);
                    Package = null;
                    _ownsPackage = false;
                }
                throw;
            }
            finally { _initializing = false; }
        }

        public async UniTask ShutdownAsync()
        {
            OnShutDown();
            if (Package == null) return;
            var package = Package;
            Package = null;
            if (!_ownsPackage) return;
            _ownsPackage = false;
            await package.DestroyPackageAsync();
            YooAssets.RemovePackage(package.PackageName);
        }


        private static void CheckOperation(AsyncOperationBase operation)
        {
            if (operation.Status != EOperationStatus.Succeeded) throw new GameFrameworkException(operation.Error);
        }

        public int downloadingMaxNum = 3;
        public int failedTryAgain = 3;
        
        /// <summary>
        /// 资源下载器，用于下载当前资源版本所有的资源包文件。
        /// </summary>
        public ResourceDownloaderOperation Downloader { get; set; }
        public ResourcePackage Package { get; private set; }
        public bool IsReady => Package != null && Package.InitializeStatus == EOperationStatus.Succeeded;
        private AssetGroup _assets;
        private bool _ownsPackage;
        private bool _initializing;

        public void InitializeModule() { _assets = AssetGroup.Alloc(); }

        public void OnStartGameLogic()
        {
        }

        public void OnShutDown() { Downloader?.CancelDownload(); Downloader = null; if (_assets != null) AssetGroup.Release(_assets); _assets = null; }


        public void UnloadAsset(object asset)
        {
            if (asset is GameObject instance) Object.Destroy(instance);
            else if (asset is Object resource) _assets?.Release(resource);
        }

        public void UnloadUnusedAssets()
        {
            Package.UnloadUnusedAssetsAsync();
            Resources.UnloadUnusedAssets();
#if !UNITY_EDITOR
            System.GC.Collect();
#endif
        }

        public void ForceUnloadAllAssets()
        {
            Package.UnloadAllAssetsAsync();
        }

        /// <summary>
        /// 创建资源下载器，用于下载当前资源版本所有的资源包文件。
        /// </summary>
        public ResourceDownloaderOperation CreateResourceDownloader()
        {
            var package = Package;
            Downloader = package.CreateResourceDownloader(new ResourceDownloaderOptions(new[] { "BuildIn" }, downloadingMaxNum, failedTryAgain));
            return Downloader;
        }
        
        /// <summary>
        /// 创建资源下载器，用于下载当前资源版本所有的资源包文件。
        /// </summary>
        public ResourceDownloaderOperation CreateResourceDownloader(string[] tags)
        {
            var package = Package;
            Downloader = package.CreateResourceDownloader(new ResourceDownloaderOptions(tags, downloadingMaxNum, failedTryAgain));
            return Downloader;
        }

        public HasAssetResult HasAsset(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }

            AssetInfo assetInfo = Package.GetAssetInfo(assetName);

            if (!CheckLocationValid(assetName))
            {
                return HasAssetResult.Valid;
            }

            if (assetInfo == null)
            {
                return HasAssetResult.NotExist;
            }

            if (IsNeedDownloadFromRemote(assetInfo))
            {
                return HasAssetResult.AssetOnline;
            }

            return HasAssetResult.AssetOnDisk;
        }

        public HasAssetResult CheckTagNeedDownload(string tag)
        {
            var assetInfos = Package.GetAssetInfos(tag);
            if (assetInfos != null && assetInfos.Length > 0)
            {
                for (int i = 0; i < assetInfos.Length; i++)
                {
                    if (IsNeedDownloadFromRemote(assetInfos[i]))
                        return HasAssetResult.AssetOnline;
                }

                return HasAssetResult.AssetOnDisk;
            }
            
            return HasAssetResult.Valid;
        } 

        /// <summary>
        /// 设置默认的资源包。
        /// </summary>

        public void SetDefaultPackage(ResourcePackage package)
        {
            if (package == null || package.InitializeStatus != EOperationStatus.Succeeded) throw new GameFrameworkException("Package is not initialized.");
            Package = package;
            _ownsPackage = false;
        }

        #region 资源信息

        /// <summary>
        /// 是否需要从远端更新下载。
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        public bool IsNeedDownloadFromRemote(string location)
        {
            return Package.GetDownloadSize(location) > 0;
        }

        /// <summary>
        /// 是否需要从远端更新下载。
        /// </summary>
        /// <param name="assetInfo">资源信息。</param>
        public bool IsNeedDownloadFromRemote(AssetInfo assetInfo)
        {
            return Package.GetDownloadSize(assetInfo) > 0;
        }

        /// <summary>
        /// 获取资源信息列表。
        /// </summary>
        /// <param name="tag">资源标签。</param>
        /// <returns>资源信息列表。</returns>
        public AssetInfo[] GetAssetInfos(string tag)
        {
            return Package.GetAssetInfos(tag);
        }

        /// <summary>
        /// 获取资源信息列表。
        /// </summary>
        /// <param name="tags">资源标签列表。</param>
        /// <returns>资源信息列表。</returns>
        public AssetInfo[] GetAssetInfos(string[] tags)
        {
            return Package.GetAssetInfos(tags);
        }

        /// <summary>
        /// 获取资源信息。
        /// </summary>
        /// <param name="location">资源的定位地址。</param>
        /// <returns>资源信息。</returns>
        public AssetInfo GetAssetInfo(string location)
        {
            return Package.GetAssetInfo(location);
        }

        /// <summary>
        /// 检查资源定位地址是否有效。
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        public bool CheckLocationValid(string location)
        {
            return Package.IsLocationValid(location);
        }

        #endregion

        /// <summary>
        /// 同步加载资源。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <typeparam name="T">要加载资源的类型。</typeparam>
        /// <returns>资源实例。</returns>
        public T LoadAsset<T>(string assetName) where T : Object
        {
            if (string.IsNullOrEmpty(assetName))
            {
                Log.Error("Asset name is invalid.");
                return default;
            }

            AssetHandle handle = Package.LoadAssetSync<T>(assetName);

            if (typeof(T) == typeof(GameObject))
            {
                GameObject ret = handle.InstantiateSync(new InstantiateOptions(true));
                AssetReference.BindAssetReference(ret, handle, assetName);
                return ret as T;
            }
            else
            {
                T ret = handle.AssetObject as T;
                _assets.Reference(assetName, handle);
                return ret;
            }
        }
        
        /// <summary>
        /// 同步加载原生文件。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <returns>资源路径。</returns>


        
        /// <summary>
        /// 异步加载原生文件。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <returns>资源路径。</returns>



        /// <summary>
        /// 同步加载资源。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <param name="parent">父节点位置。</param>
        /// <typeparam name="T">要加载资源的类型。</typeparam>
        /// <returns>资源实例。</returns>
        public T LoadAsset<T>(string assetName, Transform parent) where T : Object
        {
            if (string.IsNullOrEmpty(assetName))
            {
                Log.Error("Asset name is invalid.");
                return default;
            }

            AssetHandle handle = Package.LoadAssetSync<T>(assetName);

            if (typeof(T) == typeof(GameObject))
            {
                GameObject ret = handle.InstantiateSync(new InstantiateOptions(true));
                ret.transform.SetParent(parent,false);
                AssetReference.BindAssetReference(ret, handle, assetName);
                return ret as T;
            }
            else
            {
                T ret = handle.AssetObject as T;
                _assets.Reference(assetName, handle);
                return ret;
            }
        }

        /// <summary>
        /// 同步加载资源。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <param name="handle">资源操作句柄。</param>
        /// <param name="parent">父节点位置。</param>
        /// <typeparam name="T">要加载资源的类型。</typeparam>
        /// <returns>资源实例。</returns>
        public T LoadAsset<T>(string assetName, Transform parent, out AssetHandle handle) where T : Object
        {
            handle = Package.LoadAssetSync<T>(assetName);

            if (string.IsNullOrEmpty(assetName))
            {
                Log.Error("Asset name is invalid.");
                return default;
            }

            if (typeof(T) == typeof(GameObject))
            {
                GameObject ret = handle.InstantiateSync(new InstantiateOptions(true, parent, false));
                return ret as T;
            }
            else
            {
                return handle.AssetObject as T;
            }
        }
        
        
        /// <summary>
        /// 异步加载资源实例。
        /// </summary>
        /// <param name="assetName">要加载的实例名称。</param>
        /// <param name="cancellationToken">取消操作Token。</param>
        /// <returns>资源实实例。</returns>
        public async UniTask<T> LoadAssetAsync<T>(string assetName, CancellationToken cancellationToken) where T : Object
        {
            AssetHandle handle = LoadAssetAsyncHandle<T>(assetName);

            try { await handle.ToUniTask(cancellationToken:cancellationToken); }
            catch { handle.Release(); throw; }
            
            if (typeof(T) == typeof(GameObject))
            {
                GameObject ret = handle.InstantiateSync(new InstantiateOptions(true));
                AssetReference.BindAssetReference(ret, handle, assetName);
                return ret as T;
            }
            else
            {
                _assets.Reference(assetName, handle);
                return handle.AssetObject as T;
            }
        }

        /// <summary>
        /// 异步加载游戏物体。
        /// </summary>
        /// <param name="assetName">要加载的游戏物体名称。</param>
        /// <param name="cancellationToken">取消操作Token。</param>
        /// <returns>异步游戏物体实例。</returns>
        public async UniTask<GameObject> LoadGameObjectAsync(string assetName, CancellationToken cancellationToken)
        {
            AssetHandle handle = LoadAssetAsyncHandle<GameObject>(assetName);
            try { await handle.ToUniTask(cancellationToken:cancellationToken); }
            catch { handle.Release(); throw; }
            GameObject ret = handle.InstantiateSync(new InstantiateOptions(true));
            AssetReference.BindAssetReference(ret, handle, assetName);
            
            return ret;
        }

        /// <summary>
        /// 同步加载资源并获取句柄。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <typeparam name="T">要加载资源的类型。</typeparam>
        /// <returns>同步加载资源句柄。</returns>
        public AssetHandle LoadAssetGetOperation<T>(string assetName) where T : Object
        {
            return Package.LoadAssetSync<T>(assetName);
        }
        
        /// <summary>
        /// 异步加载资源并获取句柄。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <typeparam name="T">要加载资源的类型。</typeparam>
        /// <returns>异步加载资源句柄。</returns>
        public AssetHandle LoadAssetAsyncHandle<T>(string assetName) where T : Object
        {
            return Package.LoadAssetAsync<T>(assetName);
        }

        public Sprite GetSprite(string spriteAtlasName, string spriteName)
        {
            var spriteAtlas = LoadAsset<SpriteAtlas>(spriteAtlasName);
            if (spriteAtlas != null)
            {
                return spriteAtlas.GetSprite(spriteName);
            }
            
            return null;
        }
    }
    internal sealed class RemoteServices : IRemoteService
    {
        private readonly string _primary, _fallback;
        public RemoteServices(string primary, string fallback)
        {
            _primary = primary.TrimEnd('/');
            _fallback = string.IsNullOrWhiteSpace(fallback) ? _primary : fallback.TrimEnd('/');
        }
        public System.Collections.Generic.IReadOnlyList<string> GetRemoteUrls(string fileName)
        {
            return new[] { $"{_primary}/{fileName}", $"{_fallback}/{fileName}" };
        }
    }
}
