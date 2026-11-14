// **********************************************
// 
// Author : xuejie.guo
// Date : 2023/06/25/10:31
// Ver : 1.0.0
// Description : Launcher.cs
// ChangeLog :
// **********************************************

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MythMerge.LauncherEngine;
using MythMerge;
using MythMerge.Save;
using UnityEngine;

namespace MythMerge
{
    public class MythLauncher : MonoBehaviour
    {
        //SetUpViewResolution
        //1.CheckResVersion
        //2.SelectServer
        //3.ShowSplashView
        //4.CheckServerVersion
        //5.Check & And Update Manifest 
        //6.LauncherGame
         
        private MMLauncherFsm _mmLauncherFsm; 
       
        private void Start()
        {
            MatchQuality();
            
            InitEssential();
            
            StartLaunchProcedure();
        }

        public async void StartLaunchProcedure()
        {
            try
            {
                var settings = LauncherSettings.Instance;
    #if !PRODUCTION_PACKAGE || UNITY_EDITOR
                if (settings.ShowServerSelector)
                {
                    var prefab = Resources.Load<GameObject>("Prefabs/ServerOption");
                    if (prefab == null) 
                        throw new GameFrameworkException("Missing Resources/Prefabs/ServerOption.prefab.");
                    var selectorObject = GameObject.Instantiate(prefab, GameObject.Find("Root/UIRoot/UICanvas").transform, false);
                    var selector = selectorObject.AddComponent<ResServerSelector>();
                    await selector.WaitServerSelection();
                }
    #endif
                _mmLauncherFsm = new MMLauncherFsm();
                _mmLauncherFsm.Initialize();
                _mmLauncherFsm.Start();
                
                
                var logHelper = Activator.CreateInstance<LogHelper>();
                GameFrameworkLog.SetLogHelper(logHelper);
                
                Debug.Log("Start");
            }
            catch (OperationCanceledException) { }
            catch (Exception error) { Debug.LogException(error); }
        }

        private void InitEssential()
        {
            var storages = new List<StorageBase>()
            {
                new StorageCommon()
            };
            StorageManager.Instance.Init(storages, false);
        }
        
        private void Update()
        {
            if (_mmLauncherFsm != null)
                _mmLauncherFsm.Update();
        }
        
        

         public void MatchQuality()
         { 
            
            var useHighQuality = true;
            if (Application.platform == RuntimePlatform.Android)//CK只需对android机型做优化
            {
                if ((SystemInfo.processorFrequency != 0 && SystemInfo.processorFrequency < 1250) ||// CPU低于1.2GH
                    (SystemInfo.systemMemorySize != 0 && SystemInfo.systemMemorySize < 1200))// 或者内存低于1G
                {
                    useHighQuality = false;
                }
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)//苹果设备不能获取cpu频率，这里通过GPU型号和内存判断
            {
                //https://zh.wikipedia.org/wiki/IOS%E5%92%8CiPadOS%E8%AE%BE%E5%A4%87%E5%88%97%E8%A1%A8
#if UNITY_IOS
                var g = UnityEngine.iOS.Device.generation;
                if (g == UnityEngine.iOS.DeviceGeneration.iPhone4 ||// <1G iphone
                    g == UnityEngine.iOS.DeviceGeneration.iPhone4S ||
                    g == UnityEngine.iOS.DeviceGeneration.iPhone5 ||
                    g == UnityEngine.iOS.DeviceGeneration.iPhone5S ||
                    g == UnityEngine.iOS.DeviceGeneration.iPhone5C ||
                    g == UnityEngine.iOS.DeviceGeneration.iPhone6 ||
                    g == UnityEngine.iOS.DeviceGeneration.iPhone6Plus ||
                    g == UnityEngine.iOS.DeviceGeneration.iPad1Gen ||// <1G ipad
                    g == UnityEngine.iOS.DeviceGeneration.iPadMini1Gen ||
                    g == UnityEngine.iOS.DeviceGeneration.iPad2Gen ||
                    g == UnityEngine.iOS.DeviceGeneration.iPadMini2Gen ||
                    g == UnityEngine.iOS.DeviceGeneration.iPad3Gen ||
                    g == UnityEngine.iOS.DeviceGeneration.iPadMini3Gen ||
                    g == UnityEngine.iOS.DeviceGeneration.iPad4Gen ||
                    g == UnityEngine.iOS.DeviceGeneration.iPadAir1 ||// <1G ipadAir
                    g == UnityEngine.iOS.DeviceGeneration.iPodTouch1Gen ||// <1G ipod
                    g == UnityEngine.iOS.DeviceGeneration.iPodTouch2Gen ||
                    g == UnityEngine.iOS.DeviceGeneration.iPodTouch3Gen ||
                    g == UnityEngine.iOS.DeviceGeneration.iPodTouch4Gen ||
                    g == UnityEngine.iOS.DeviceGeneration.iPodTouch5Gen ||
                    g == UnityEngine.iOS.DeviceGeneration.iPodTouch6Gen)
                {
                    useHighQuality = false;
                }
#endif
            }
            Application.targetFrameRate = useHighQuality ? 60 : 30;
        }
    }
}
