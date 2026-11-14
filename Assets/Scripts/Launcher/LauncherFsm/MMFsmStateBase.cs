// **********************************************
// 
// Author : xuejie.guo
// Date : 2023/06/25/10:43
// Ver : 1.0.0
// Description : LauncherFSMState.cs
// ChangeLog :
// **********************************************

using MythMerge.LauncherEngine;
using UnityEngine;
using ProcedureOwner = MythMerge.LauncherEngine.IFsm<MythMerge.MMLauncherFsm>;

namespace MythMerge
{
    public class MMFsmStateBase:FsmState<MMLauncherFsm>
    {

        public  float stepDeltaProgress = 0.02f;
        
        /// <summary>
        /// 状态初始化时调用。
        /// </summary>
        /// <param name="procedureOwner">流程持有者。</param>
        protected override void OnInit(ProcedureOwner procedureOwner)
        {
            stepDeltaProgress = 0.02f;
            base.OnInit(procedureOwner);
        }

        /// <summary>
        /// 进入状态时调用。
        /// </summary>
        /// <param name="procedureOwner">流程持有者。</param>
        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (procedureOwner.Owner.debugText != null)
            {
                procedureOwner.Owner.debugText.gameObject.SetActive(true);
                procedureOwner.Owner.debugText.text = $"{Time.realtimeSinceStartup}" + this.GetType().FullName;
            }
            Debug.Log($"OnEnterState:{this.GetType().FullName}");
#endif
            base.OnEnter(procedureOwner);

            procedureOwner.Owner.accumulateProgress += stepDeltaProgress;
        }

        /// <summary>
        /// 状态轮询时调用。
        /// </summary>
        /// <param name="procedureOwner">流程持有者。</param>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
        }

        /// <summary>
        /// 离开状态时调用。
        /// </summary>
        /// <param name="procedureOwner">流程持有者。</param>
        /// <param name="isShutdown">是否是关闭状态机时触发。</param>
        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            base.OnLeave(procedureOwner, isShutdown);
        }

        /// <summary>
        /// 状态销毁时调用。
        /// </summary>
        /// <param name="procedureOwner">流程持有者。</param>
        protected override void OnDestroy(ProcedureOwner procedureOwner)
        {
            base.OnDestroy(procedureOwner);
        }
    }
}
