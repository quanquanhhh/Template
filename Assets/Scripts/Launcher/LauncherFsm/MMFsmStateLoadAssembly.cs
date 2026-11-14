// **********************************************
// 
// Author : xuejie.guo
// Date : 2023/06/25/11:29
// Ver : 1.0.0
// Description : LFsmStateLoadAssembly.cs
// ChangeLog :
// **********************************************

using System;
using System.Collections.Generic;
using System.Reflection;
using HybridCLR;
using MythMerge.LauncherEngine;
using UnityEngine;
using YooAsset;

namespace MythMerge
{
    public class MMFsmStateLoadAssembly : MMFsmStateBase
    {
        private static class AssemblyDefine
        {
            public static List<string> HotAssembly = new List<string>() {"Assembly-CSharp.dll"};
            public static List<string> AOTAssembly = new List<string>()
            {
                "DOTween.dll", 
                // "Google.Protobuf.dll",
                "Newtonsoft.Json.dll",
                "System.Core.dll",
                "System.dll",
                "MatchVillaFundamental.dll",
                "UniTask.dll",
                "UnityEngine.CoreModule.dll",
                "UnityEngine.JSONSerializeModule.dll",
                "YooAsset.dll",
                "mscorlib.dll",
                "spine-unity.dll",
                // "FlatBuffer.dll"
            };
        }
 
        private int LoadMetadataAssetCount = 0;
        private int LoadAssetCount = 0;

        protected bool NeedLoadDll => GlobalSetting.PlayMode == EPlayMode.HostPlayMode ||
                                      GlobalSetting.PlayMode == EPlayMode.OfflinePlayMode;

        protected bool AssemblyLoadComplete = false;
        protected bool AssemblyMetadataLoadComplete = false;
        protected int FailureAssetCount = 0;
        protected int FailureMetadataAssetCount = 0;

        protected override void OnInit(IFsm<MMLauncherFsm> procedureOwner)
        {
            stepDeltaProgress = 0.08f;
            base.OnInit(procedureOwner);
        }

        protected override void OnEnter(IFsm<MMLauncherFsm> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            
            AssemblyLoadComplete = false;
            AssemblyMetadataLoadComplete = false;
            LoadMetadataForAOTAssembly();

            LoadHotAssembly();
        }


        protected void LoadHotAssembly()
        {
#if UNITY_EDITOR || !ENABLE_HYBRIDCLR
            AssemblyLoadComplete = true;
            return;
#endif
            LoadAssetCount = 0;
            foreach (string aotDllName in AssemblyDefine.HotAssembly)
            {
                var assetLocation = aotDllName;
                LoadAssetCount++;
                LoadAsset(assetLocation, LoadAssetSuccess);
            }
        }

        private void LoadAssetSuccess(AssetHandle assetOperationHandle)
        { 
            if (assetOperationHandle.IsDone && assetOperationHandle.Status == EOperationStatus.Succeeded)
            {
                var assetName = assetOperationHandle.AssetObject.name;
                Debug.Log($"LoadAssetSuccess, assetName: [ {assetName} ]");

                var textAsset = assetOperationHandle.AssetObject as TextAsset;
                if (textAsset == null)
                {
                    Debug.LogWarning($"Load text asset [ {assetName} ] failed.");
                    return;
                }

                try
                { 
                    var assembly = Assembly.Load(textAsset.bytes);
                    assetOperationHandle.Release();
                    Debug.Log($"Assembly [ {assembly.GetName().Name} ] loaded");
                }
                catch (Exception e)
                {
                    FailureAssetCount++;
                    Debug.LogError(e);
                }
                finally
                {
                    LoadAssetCount--;
                    AssemblyLoadComplete = (LoadAssetCount == 0);
                }
            }
            else
            {
                
                LoadHotAssembly();
              
                Log.Error("LoadAssemblyError:" + assetOperationHandle.Error);
            }
        }


        protected override void OnUpdate(IFsm<MMLauncherFsm> procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
            Log.Error($"^~^ OnUpdate:{this.GetType().FullName}:" +
                      $"{AssemblyLoadComplete}/{AssemblyMetadataLoadComplete}"
                      +$"{LoadAssetCount}/{LoadMetadataAssetCount}" );
            
            if (!AssemblyLoadComplete)
            {
                return;
            }

            if (!AssemblyMetadataLoadComplete)
            {
                return;
            }

            OnAssemblyLoadComplete(procedureOwner);
        }

        public void OnAssemblyLoadComplete(IFsm<MMLauncherFsm> procedureOwner)
        {
            //不直接调用 GameModule.GameModuleLaunch.Start 是防止编译dll的时候找不到 GameModule部分的GameModule.GameModuleLaunch类而报错
            Resources.UnloadUnusedAssets();
            ChangeState<MMFsmStateLaunchGame>(procedureOwner);
        }

        /// <summary>
        /// 为Aot Assembly加载原始metadata， 这个代码放Aot或者热更新都行。
        /// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行。
        /// </summary>
        public void LoadMetadataForAOTAssembly()
        {
#if UNITY_EDITOR
            AssemblyMetadataLoadComplete = true;
            return;
#endif


            // 可以加载任意aot assembly的对应的dll。但要求dll必须与unity build过程中生成的裁剪后的dll一致，而不能直接使用原始dll。
            // 我们在BuildProcessor_xxx里添加了处理代码，这些裁剪后的dll在打包时自动被复制到 {项目目录}/HybridCLRData/AssembliesPostIl2CppStrip/{Target} 目录。

            // 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
            // 热更新dll不缺元数据，不需要补充，如果调用LoadMetadataForAOTAssembly会返回错误

            if (AssemblyDefine.AOTAssembly.Count == 0)
            {
                AssemblyMetadataLoadComplete = true;
                return;
            }

            foreach (string aotDllName in AssemblyDefine.AOTAssembly)
            {
                LoadMetadataAssetCount++;
                var textAsset = Resources.Load<TextAsset>("AOTMeta/" + aotDllName);
                if (textAsset != null)
                {
                    LoadMetadataAssetSuccess(aotDllName, textAsset);
                }
            }
        }

        private void LoadAsset(string location, Action<AssetHandle> finishAction)
        { 
            var assetOperationHandle = YooAssets.GetPackage(GlobalSetting.DefaultPackageName).LoadAssetAsync<TextAsset>(location);
            assetOperationHandle.Completed += finishAction;
        }

        private unsafe void LoadMetadataAssetSuccess(string assetName, TextAsset textAsset)
        {
            if (null == textAsset)
            {
                LoadMetadataAssetCount--;
                Debug.Log($"^~^ LoadMetadataAssetSuccess:Load text asset [ {assetName} ] failed.");
                return;
            }
#if ENABLE_IL2CPP || ENABLE_HYBRIDCLR
            try
            {
                byte[] dllBytes = textAsset.bytes;
                fixed (byte* ptr = dllBytes)
                {
                    // 加载assembly对应的dll，会自动为它hook。一旦Aot泛型函数的native函数不存在，用解释器版本代码
                    HomologousImageMode mode = HomologousImageMode.SuperSet;
                    LoadImageErrorCode err =
                        (LoadImageErrorCode) HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
                    Debug.LogWarning($"LoadMetadataForAOTAssembly:{assetName}. mode:{mode} ret:{err}");
                    if (err != LoadImageErrorCode.OK) throw new GameFrameworkException("AOT metadata failed: " + assetName + " / " + err);
                }
            }
            catch (Exception e)
            {
                FailureMetadataAssetCount++;
                Debug.LogError(e.Message);
            }
            finally
            {
                LoadMetadataAssetCount--;
                AssemblyMetadataLoadComplete = (LoadMetadataAssetCount == 0);
            }
#else
            AssemblyMetadataLoadComplete = true;
#endif
        }
 
    }
}
