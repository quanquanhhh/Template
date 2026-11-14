// **********************************************
// 
// Author : xuejie.guo
// Date : 2023/06/25/10:53
// Ver : 1.0.0
// Description : LFsmStateFetchVersionInfo.cs
// ChangeLog :
// **********************************************

using System;
using System.Collections;
using System.Collections.Generic;
using MythMerge.LauncherEngine;
using MythMerge.Save;
using UnityEngine;
using UnityEngine.Networking;

namespace MythMerge
{
    public class MMFsmStateFetchVersionInfo : MMFsmStateBase
    {
        private IFsm<MMLauncherFsm> fsm;
        
        protected override void OnInit(IFsm<MMLauncherFsm> procedureOwner)
        {
            stepDeltaProgress = 0.082f;
            base.OnInit(procedureOwner);
        }

        protected override void OnEnter(IFsm<MMLauncherFsm> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            fsm = procedureOwner;
            FetchVersionInfo();
        }

        protected void FetchVersionInfo()
        {
            string strClientResVersion = PlayerPrefs.GetString("MM1.ResourceVersion", GlobalSetting.ResourceVersion);

            try
            {
                //避免覆盖安装，读取本地版本配置，导致资源版本不对的问题
                int.TryParse(strClientResVersion.Replace("v", ""), out var lastLocalVersion);
                int.TryParse(GlobalSetting.ResourceVersion.Replace("v", ""), out var packageDefaultVersion);

                if (lastLocalVersion > packageDefaultVersion)
                {
                    GlobalSetting.ResourceVersion = strClientResVersion;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
            if (string.IsNullOrWhiteSpace(LauncherSettings.Instance.APIServerURL))
            {
                ChangeState<MMFsmStateInitializeResources>(fsm);
                return;
            }
            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                var playerId = "0";
                StorageCommon storage = StorageManager.Instance.GetStorage<StorageCommon>();
                if (storage != null)
                {
                    playerId = storage.PlayerId.ToString();
                }
                Utility.Unity.StartCoroutine(GetServerInfoCoroutine(LauncherSettings.Instance.APIServerURL,
                    playerId, () => { ChangeState<MMFsmStateInitializeResources>(fsm); }));
            }
            else
            {
                Debug.LogError("Network not NotReachable: skip server version check, use lastResourceVersion:" +
                          strClientResVersion);
                ChangeState<MMFsmStateInitializeResources>(fsm);
            }
        }

        private void ParseRequestedVersionInfo(string requestInfo)
        {
            if (string.IsNullOrEmpty(requestInfo))
                return;
            try
            {
                var versionInfoDict =
                    Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(requestInfo);
                if (versionInfoDict != null && versionInfoDict.Keys.Count > 0)
                {
                    var fetchedVersion = "";
                    var fetchedRootVersion = "";
#if UNITY_ANDROID
                    if (versionInfoDict.ContainsKey("resource-android-version"))
                    {
                        fetchedVersion = versionInfoDict["resource-android-version"];
                    }

                    if (versionInfoDict.ContainsKey("client-android-version"))
                    {
                        fetchedRootVersion = versionInfoDict["client-android-version"];
                    }
#else
                    if (versionInfoDict.ContainsKey("resource-ios-version"))
                    {
                        fetchedVersion = versionInfoDict["resource-ios-version"];
                    }
                     if (versionInfoDict.ContainsKey("client-ios-version"))
                    {
                        fetchedRootVersion = versionInfoDict["client-ios-version"];
                    }
#endif
                    if (!string.IsNullOrEmpty(fetchedRootVersion))
                    {
                        GlobalSetting.FetchedRootVersion = "r" + fetchedRootVersion;

                        Debug.Log("Requested Root Version:" + GlobalSetting.ResourceVersion);
                    }
                    
                    if (!string.IsNullOrEmpty(fetchedVersion))
                    {
                        var fetchedVersionNum = int.Parse(fetchedVersion.Replace("v", ""));
                        var localVersionNum = int.Parse(GlobalSetting.ResourceVersion.Replace("v", ""));
                       
                        if (fetchedVersionNum > localVersionNum && GlobalSetting.FetchedRootVersion == GlobalSetting.RootVersion)
                        {
                            GlobalSetting.ResourceVersion = "v" + fetchedVersionNum;
                            Debug.Log("Use New Version:" + GlobalSetting.ResourceVersion);
                        }

                        fsm.Owner.IsFetchVersionInfoSucceed = true;
                    }
                }
            }
            catch (Exception e)
            {
                fsm.Owner.IsFetchVersionInfoSucceed = false;

                Debug.LogError(e.Message);
                throw;
            }
        }

        protected IEnumerator GetServerInfoCoroutine(string serverUrl, string playerId,
            Action finishCallback)
        {
            var url =
                $@"{serverUrl}/version?id={playerId}&code={GlobalSetting.RootVersion}&platform={DeviceHelper.GetPlatform()}";

            Debug.LogError($"==== RequestVersion url:{url}");

            var request = new UnityWebRequest(new Uri(url), UnityWebRequest.kHttpVerbGET);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.disposeCertificateHandlerOnDispose = true;
            request.timeout = LauncherSettings.Instance.APIServerTimeout;
            Debug.Log($"[[ShowOnExceptionHandler]] SendWebRequest");
            request.SendWebRequest();
            var timer = 0f;
            while (!request.isDone)
            { 
                yield return new WaitForEndOfFrame();
                timer += Time.deltaTime;
                if (timer >= LauncherSettings.Instance.APIServerTimeout && !request.isDone)
                {
                    request.Dispose();
                    Debug.LogError($"GetServerInfoError Time out Failed Not Done");
                    finishCallback?.Invoke();
                    yield break;
                }
            }
            
            if (request.isDone)
            {
                Debug.Log($"[[ShowOnExceptionHandler]] SendWebRequestBack");
                if (request.result == UnityWebRequest.Result.ProtocolError ||
                    request.result == UnityWebRequest.Result.ConnectionError)
                {
                    Debug.LogError($"GetServerInfoError:{request.result.ToString()}/ {request.error}");
                    finishCallback?.Invoke();
                    yield break;
                }

                Debug.LogError($"Success:${request.downloadHandler.text}");

                ParseRequestedVersionInfo(request.downloadHandler.text);
                finishCallback?.Invoke();
                yield break;
            }
            else
            {
                request.Dispose();
            }

            Debug.LogError($"GetServerInfoError Failed Not Done");
            finishCallback?.Invoke();
        }
    }
}
