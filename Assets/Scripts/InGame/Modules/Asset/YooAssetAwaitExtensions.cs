using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace MythMerge
{
    public static class YooAssetAwaitExtensions
    {
        // 3.x 自带 await；此适配只为保留原框架的 UniTask 和取消调用习惯。
        // 取消等待不会取消共享 Provider，句柄仍由调用方负责释放。
        public static async UniTask ToUniTask(this HandleBase handle, CancellationToken cancellationToken = default)
        {
            if (handle == null || !handle.IsValid) throw new InvalidOperationException("Asset handle is invalid.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!handle.IsDone)
                await UniTask.WaitUntil(() => !handle.IsValid || handle.IsDone, cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!handle.IsValid) throw new ObjectDisposedException(nameof(handle));
            if (handle.Status != EOperationStatus.Succeeded) throw new InvalidOperationException(handle.Error);
        }
    }
}
