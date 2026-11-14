// **********************************************
// 
// Author : xuejie.guo
// Date : 2023/06/25/11:49
// Ver : 1.0.0
// Description : LFsmStateLaunchGame.cs
// ChangeLog :
// **********************************************

using System;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using MythMerge.LauncherEngine;

namespace MythMerge
{
    public class MMFsmStateLaunchGame : MMFsmStateBase
    {
        protected override void OnInit(IFsm<MMLauncherFsm> procedureOwner)
        {
            stepDeltaProgress = 0.01f;
            base.OnInit(procedureOwner);
        }

        protected override void OnEnter(IFsm<MMLauncherFsm> procedureOwner)
        {
            base.OnEnter(procedureOwner);
             
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (assemblies.Length > 0)
            {
                Debug.Log($"Search Main Assemblies:{this.GetType().FullName}");

                string[] targetNames =
                {
                    "Assembly-CSharp"
#if UNITY_EDITOR
                    , "Custom-CSharp"
#endif

                };

                foreach (var assembly in assemblies)
                {
                    if (targetNames.Contains(assembly.GetName().Name))
                    {
                        var type = assembly.GetType("MythMerge.GameApp");
                        if (type != null)
                        {
                            var method = type.GetMethod("Entrance");
                            object[] objects = new object[] {new object[] {assembly.GetName().Name}};
                            if (method != null)
                            {
                                method ?.Invoke(type, objects);
                                Log.Info($"LaunchGame Called"); 
                            }
                        }
                    }
                }
            }
        }

 
    }
}
