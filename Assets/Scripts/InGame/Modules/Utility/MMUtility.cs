using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MythMerge
{
    public static class MMUtility
    {
        public static void PlayAnimation(Animator animator, string stateName, Action finishCallback = null,
            CancellationToken cancellationToken = default)
        {
            PlayAnimationWithCallback(animator, stateName, finishCallback, cancellationToken).Forget();
        }

        private static async UniTaskVoid PlayAnimationWithCallback(Animator animator, string state, Action callback, CancellationToken token)
        {
            if (await PlayAnimationAsync(animator, state, token).SuppressCancellationThrow()) return;
            callback?.Invoke();
        }

        public static async UniTask PlayAnimationAsync(Animator animator, string stateName, CancellationToken cancellationToken = default)
        {
            if (!animator.HasState(stateName)) return;
            cancellationToken.ThrowIfCancellationRequested();
            animator.Play(stateName, 0, 0);
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            while (animator != null && animator.isActiveAndEnabled)
            {
                var state = animator.GetCurrentAnimatorStateInfo(0);
                if (!state.IsName(stateName) || state.normalizedTime >= 1f || state.loop) break;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }
    }
}
