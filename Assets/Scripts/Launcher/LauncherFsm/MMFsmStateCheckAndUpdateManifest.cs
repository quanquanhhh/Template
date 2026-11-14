// **********************************************
// 
// Author : xuejie.guo
// Date : 2023/06/25/11:16
// Ver : 1.0.0
// Description : LFsmStateUpdateManifest.cs
// ChangeLog :
// **********************************************

using MythMerge.LauncherEngine;
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace MythMerge
{
    public class MMFsmStateCheckAndUpdateManifest:MMFsmStateBase
    {
        protected override void OnInit(IFsm<MMLauncherFsm> procedureOwner)
        {
            stepDeltaProgress = 0.05f;
            base.OnInit(procedureOwner);
        }

        protected override void OnEnter(IFsm<MMLauncherFsm> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            if (GlobalSetting.PlayMode == EPlayMode.EditorSimulateMode ||
                Application.internetReachability == NetworkReachability.NotReachable)
            {
                ChangeState<MMFsmStateLoadAssembly>(procedureOwner);
            }
            else
            {
                CheckAndUpdateManifest(procedureOwner);
            }

        }

        protected async void CheckAndUpdateManifest(IFsm<MMLauncherFsm> procedureOwner)
        {
            Log.Info("[Launcher] CheckAndUpdateManifest");
             
            try
            {
                Log.Info("CheckAndUpdatePatchManifest");
                var defaultPackage = YooAssets.GetPackage(GlobalSetting.DefaultPackageName);
                
                var operation = defaultPackage.UnloadAllAssetsAsync();
                await operation.ToUniTask();
                Log.Info($"CheckServerVersion Pass");

                var host = GlobalSetting.GetHostResUrl(); 
                Log.Info($"ResourceHostServer:{host}");
                var options = new LoadPackageManifestOptions(GlobalSetting.ResourceVersion, 30);
                var operation2 = defaultPackage.LoadPackageManifestAsync(options);
                await operation2.ToUniTask(); 
                Log.Info($"UpdateManifest Pass");
               

                ChangeState<MMFsmStateLoadAssembly>(procedureOwner);
                if (operation2.Status == EOperationStatus.Succeeded)
                {

                    var packageVersion = YooAssets.GetPackage(GlobalSetting.DefaultPackageName).GetPackageVersion();
                    if (!string.IsNullOrEmpty(packageVersion))
                    {
                        PlayerPrefs.SetString("MM1.ResourceVersion", packageVersion);
                        Log.Error("Store Local Res Version:" + packageVersion);
                        
                    }
                }
                else
                {
                    Log.Error("UpdateManifest: Error-> :" + operation2.Error); 
                }

            }
            catch (Exception error) { Log.Debug("[Launcher]"+error); }
        }
    }
}
