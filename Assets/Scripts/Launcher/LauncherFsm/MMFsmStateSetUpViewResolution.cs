// **********************************************
// 
// Author : xuejie.guo
// Date : 2023/06/25/10:48
// Ver : 1.0.0
// Description : LFsmStateSetUpViewResolution.cs
// ChangeLog :
// **********************************************

using MythMerge.LauncherEngine;
using UnityEngine;
using UnityEngine.UI;

namespace MythMerge
{
    public class MMFsmStateSetUpViewResolution:MMFsmStateBase
    {
        protected override void OnInit(IFsm<MMLauncherFsm> procedureOwner)
        {
            stepDeltaProgress = 0.001f;
            base.OnInit(procedureOwner);
        }

        protected override void OnEnter(IFsm<MMLauncherFsm> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            
            ViewResolution.SetUpViewResolution();
            
            // GameObject.Find("Root/UIRoot/UICanvas").GetComponent<CanvasScaler>().referenceResolution =
            //     ViewResolution.referenceResolutionPortrait;

            var rootGameObject = GameObject.Find("Root");

            
            GameObject.DontDestroyOnLoad(rootGameObject);
            
// #if !PRODUCTION_PACKAGE
//             if(!rootGameObject.GetComponent<ApplicationExceptionHandler>())
//                 rootGameObject.AddComponent<ApplicationExceptionHandler>();
// #endif
            ChangeState<MMFsmStateShowSplash>(procedureOwner);
          
        }
    }
}
