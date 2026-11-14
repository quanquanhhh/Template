// Author : xuejie.guo
// Description : TM5 ResServerSelector，服务器地址由 MM1 FrameworkSettings 配置。
#if !PRODUCTION_PACKAGE || UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace MythMerge
{
    public class ResServerSelector : MonoBehaviour
    {
        private int index;
        private ToggleGroup _resourceDropdown;
        private Button _button;
        
        private TaskCompletionSource<bool> waitSelectTask;
        private string[] availableResUrls; 
       
        private void Awake()
        {
            LauncherSettings.Instance.version = VersionStatus.DEBUG;
            _resourceDropdown = transform.Find("Content/ToggleGroup").GetComponent<ToggleGroup>();
            
            if (PlayerPrefs.HasKey("ResUrlDropdownValue"))
            {
                index = PlayerPrefs.GetInt("ResUrlDropdownValue");
            }

            var lastToggleOn = index;
            for (int i = 0; i < _resourceDropdown.transform.childCount; i++)
            {
                var tmpIndex = i;
                var child = _resourceDropdown.transform.GetChild(i).GetComponent<Toggle>();
                child.onValueChanged.AddListener(arg0 =>
                {
                    if (arg0)
                        index = tmpIndex;
                });
                if (tmpIndex == lastToggleOn)
                    child.isOn = true;
            }
            
            _button = transform.Find("Content/ConfirmButton").GetComponent<Button>();
            _button.onClick.AddListener(OnConfirmClicked);
        }

        public async Task WaitServerSelection()
        {
            waitSelectTask = new TaskCompletionSource<bool>();
            await waitSelectTask.Task;
        }

        private void OnConfirmClicked()
        {
            var resUrl = availableResUrls[index];
            LauncherSettings.Instance.Res_Server_URL_Beta = resUrl;
            PlayerPrefs.SetInt("ResUrlDropdownValue", index);

            if (waitSelectTask != null)
                waitSelectTask.SetResult(true);
            GameObject.Destroy(gameObject);
        }


    }
}
#endif
