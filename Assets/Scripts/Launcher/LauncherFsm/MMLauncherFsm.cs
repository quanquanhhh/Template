// **********************************************
// 
// Author : xuejie.guo
// Date : 2023/06/25/10:38
// Ver : 1.0.0
// Description : LauncherFsm.cs
// ChangeLog :
// **********************************************
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using YooAsset;
using DG.Tweening;
using MythMerge.LauncherEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MythMerge
{
    public class MMLauncherFsm
    {
        private IFsmManager _fsmManager;
        private IFsm<MMLauncherFsm> _fsm;

        public TextMeshProUGUI debugText;

        public float accumulateProgress = 0.0f;
        public float displayProgress = 0.0f;

        public bool IsFetchVersionInfoSucceed = false;

        public GameObject LoadingView;

        public Type[] fsmStateTypes =
        {
            typeof(MMFsmStateSetUpViewResolution),
            typeof(MMFsmStateShowSplash),
            typeof(MMFsmStateFetchVersionInfo),
            typeof(MMFsmStateInitializeResources),
            typeof(MMFsmStateCheckAndUpdateManifest),
            typeof(MMFsmStateLoadAssembly),
            typeof(MMFsmStateLaunchGame)
        };
 
        private Tween _tween;

        public void ShowInitializeText(float initializeProgress)
        {
            if (LoadingView != null)
            {
                var slider = LoadingView.transform.Find("ProgressSlider").GetComponent<Slider>();
                var textMeshProUGUI = LoadingView.transform.Find("ProgressSlider/ProgressText").GetComponent<TextMeshProUGUI>();
                slider.value = initializeProgress;
                textMeshProUGUI.text = LocaleHelper.GetInitializeText(initializeProgress*100);
            }
        }
 
        public void Initialize()
        {
            _fsmManager = new FsmManager();

            var fsmStates = new FsmState<MMLauncherFsm>[fsmStateTypes.Length];

            for (var i = 0; i < fsmStateTypes.Length; i++)
            {
                fsmStates[i] = (FsmState<MMLauncherFsm>) Activator.CreateInstance(fsmStateTypes[i]);
            }

            _fsm = _fsmManager.CreateFsm("Launcher", this, fsmStates);
        }
      
        public void Start() 
        {
            if (_fsm == null)
            {
                throw new GameFrameworkException("You must initialize procedure first.");
            }
            
            _fsm.Start<MMFsmStateSetUpViewResolution>();
        }

        public void Update()
        {
            if (_fsm != null )
            {
                 if (LoadingView != null && !(_fsm.CurrentState is MMFsmStateLaunchGame))
                 {
                     if (displayProgress < accumulateProgress)
                     {
                         displayProgress = displayProgress + (accumulateProgress - displayProgress) * 0.1f;
                     }
                     
                     ShowInitializeText(displayProgress);
                 }
                 
                _fsmManager.Update(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }
    }
}
