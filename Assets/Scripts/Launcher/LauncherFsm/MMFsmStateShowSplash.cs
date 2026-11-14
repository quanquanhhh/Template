// **********************************************
// 
// Author : xuejie.guo
// Date : 2023/06/25/10:53
// Ver : 1.0.0
// Description : LFsmStateFetchVersionInfo.cs
// ChangeLog :
// **********************************************

using MythMerge.LauncherEngine;
using TMPro;
using UnityEngine;

namespace MythMerge
{
    public class MMFsmStateShowSplash:MMFsmStateBase
    {
        protected override void OnInit(IFsm<MMLauncherFsm> procedureOwner)
        {
            stepDeltaProgress = 0.001f;
            base.OnInit(procedureOwner);
        }

        protected override void OnEnter(IFsm<MMLauncherFsm> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            ShowSplash(procedureOwner);
        }
        
        protected void ShowSplash(IFsm<MMLauncherFsm> procedureOwner)
        {
            var uiLoading = Resources.Load<GameObject>("Launcher/UILoading");
            var uiCanvas = GameObject.Find("Root/UIRoot/UICanvas");
            
            if (uiCanvas != null && uiLoading != null)
            {
                var loadView = GameObject.Instantiate(uiLoading, uiCanvas.transform);
                procedureOwner.Owner.LoadingView = loadView;
               
                loadView.name = "UILoading";
                //var textMeshProUGUI = loadView.transform.Find("ProgressSlider/ProgressText").GetComponent<TextMeshProUGUI>();
                
                LocaleHelper.MatchLocale();

                //LocaleHelper.LocaleTextMeshProUGUI(textMeshProUGUI);
                
                var debugText = loadView.transform.Find("ProgressSlider/LoadingTips")?.GetComponent<TextMeshProUGUI>();
                if(debugText != null)
                    procedureOwner.Owner.debugText = debugText;
            }
          
            ChangeState<MMFsmStateFetchVersionInfo>(procedureOwner);
        }
    }
}
