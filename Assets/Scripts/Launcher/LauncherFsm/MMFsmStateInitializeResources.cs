// **********************************************
// 
// Author : xuejie.guo
// Date : 2023/06/25/11:08
// Ver : 1.0.0
// Description : LFsmStateInitializeResources.cs
// ChangeLog :
// **********************************************

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Threading.Tasks;
using MythMerge.LauncherEngine;
using UnityEngine;
using YooAsset;

namespace MythMerge
{
    public class MMRemoteServices : IRemoteService
    {
        private readonly string hostUrl; 
        public MMRemoteServices(string inHostUrl)
        {
            hostUrl = inHostUrl;
        }
        public System.Collections.Generic.IReadOnlyList<string> GetRemoteUrls(string fileName)
        {
            return new[] { $"{hostUrl}/{fileName}"};
        }
    }

    public class MMFsmStateInitializeResources : MMFsmStateBase
    {
        protected override void OnInit(IFsm<MMLauncherFsm> procedureOwner)
        {
            stepDeltaProgress = 0.08f;
            base.OnInit(procedureOwner);
        }

        protected override async void OnEnter(IFsm<MMLauncherFsm> procedureOwner)
        {
            base.OnEnter(procedureOwner);

            InitializePackageOperation initOp = null;
            
            YooAssets.Initialize();

            if (GlobalSetting.PlayMode == EPlayMode.EditorSimulateMode)
            {
    
                var defaultPackage = YooAssets.CreatePackage(GlobalSetting.DefaultPackageName);
                var buildResult = EditorSimulateBuildInvoker.Build(
                    GlobalSetting.DefaultPackageName, (int)EBundleType.VirtualAssetBundle);
                var options = new EditorSimulateModeOptions
                {
                    EditorFileSystemParameters =
                        FileSystemParameters.CreateDefaultEditorFileSystemParameters(
                            buildResult.PackageRootDirectory)
                };
                initOp = defaultPackage.InitializePackageAsync(options);
            }
            // 联机运行模式
            else
            {
                var options = HotResourceServices.CreateHostOptions(
                    new MMRemoteServices(GlobalSetting.GetHostResUrl()),
                    LauncherSettings.Instance._AesKey);

                var defaultPackage = YooAssets.CreatePackage(GlobalSetting.DefaultPackageName);
                initOp = defaultPackage.InitializePackageAsync(options);
                
            }

            await initOp.ToUniTask();
            if (initOp.Status != EOperationStatus.Succeeded)
            {
                Log.Error("[Launcher] FsmStateInitializeResources:Error-> :" + initOp.Error);
            }
            ChangeState<MMFsmStateCheckAndUpdateManifest>(procedureOwner);
            
        }
    }
}
