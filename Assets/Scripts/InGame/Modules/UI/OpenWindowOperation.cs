using YooAsset;

namespace MythMerge
{
    /// <summary>打开窗口操作句柄；由 YooAsset 3 的异步操作系统驱动。</summary>
    public class OpenWindowOperation : CustomAsyncOperation
    {
        private enum ESteps { None, Waiting, Done }
        private readonly UIWindow _window;
        private ESteps _steps;
        public UIWindow Window => _window;

        internal OpenWindowOperation(UIWindow window) { _window = window; }
        protected override void InternalStart() { _steps = ESteps.Waiting; }
        protected override void InternalUpdate()
        {
            if (_steps != ESteps.Waiting) return;
            var handle = _window.Handle;
            if (handle == null || !handle.IsValid)
            {
                SetError("Window was closed before it finished opening.");
            }
            else if (handle.IsDone)
            {
                if (handle.Status != EOperationStatus.Succeeded || _window.LoadError != null)
                {
                    SetError(_window.LoadError?.Message ?? handle.Error);
                }
                else if (_window.IsOpened) SetResult();
            }
            if (IsDone) _steps = ESteps.Done;
        }
        protected override void InternalWaitForCompletion()
        {
            _window.Handle?.WaitForAsyncComplete();
            InternalUpdate();
            if (!IsDone)
            {
                SetError("Window is still preparing asynchronously. Await ShowUIAsync instead.");
            }
        }
        public void WaitForAsyncComplete() { WaitForCompletion(); }
    }
}
