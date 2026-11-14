using MythMerge.Core;

namespace MythMerge
{
    public struct EventOnApplicationPause : IEvent
    {
        public bool isPause;
        public EventOnApplicationPause(bool inIsPause) { isPause = inIsPause; }
    }
    public struct WindowOpenEvent : IEvent { }
    public struct WindowCloseEvent : IEvent { }
    public struct EventWindowStackSizeChange : IEvent { }
    public struct EventStorageFullInitialized : IEvent { }
}
