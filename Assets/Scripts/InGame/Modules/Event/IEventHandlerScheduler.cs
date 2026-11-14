/**********************************************


Author : xuejie.guo
Date : 2020-10-14 08:17:34
Ver : 1.0.0
Description : 
ChangeLog :  
**********************************************/


using System;
using System.Collections.Generic;
using MythMerge.LauncherEngine;

namespace MythMerge
{
    public interface IEventHandlerScheduler
    {
        void RemoveHandler(Delegate handler);
        void StopSchedule();
        void InvalidateHandler();

        bool IsHandleEvent();
    }
 
    //Event处理链式调度器，根据EventHandler的优先级，调用handleAction
    public class EventHandlerScheduler<T>:IEventHandlerScheduler where T : IEvent
    {
        private List<Handler> eventHandlers;

        protected bool isScheduleStopped = true;
        struct Handler
        {
            public Action<Action,T, IEventHandlerScheduler> handleAction;
            public int priority;

            public Handler(Action<Action,T, IEventHandlerScheduler> inHandleAction, int inPriority)
            {
                handleAction = inHandleAction;
                priority = inPriority;
            }
        }
        public EventHandlerScheduler()
        {
            eventHandlers = new List<Handler>();
        }

        public void AddingHandler(Action<Action,T,IEventHandlerScheduler> handler, int priority)
        {
            eventHandlers.Add(new Handler(handler, priority));
        }

        public void RemoveHandler(Delegate inHandler)
        {
           // XDebug.LogError(inHandler.Method);

            for (var i = 0; i < eventHandlers.Count; i++)
            {
                if ((Delegate) eventHandlers[i].handleAction == inHandler)
                {
                    eventHandlers.RemoveAt(i);
                  //  XDebug.Log("eventHandlers.Remove())");
                    break;
                }
            }
        }

        public void Schedule(T eventData, Action scheduleEndAction)
        {
            eventHandlers.Sort((handlerA, handlerB) => { return -handlerA.priority.CompareTo(handlerB.priority);});
             
            if (eventHandlers.Count > 0)
            {
                Log.Info($"PriorityHandleStart:{typeof(T)}");
                isScheduleStopped = false;
                ScheduleHandlerByIndex(0, eventData, scheduleEndAction);
                return;
            }
            scheduleEndAction?.Invoke();
        }

        public void InvalidateHandler()
        {
            eventHandlers.Clear();
        }

        public void ScheduleHandlerByIndex(int index, T eventData, Action scheduleEndAction)
        {
            if (!isScheduleStopped && eventHandlers.Count > index)
            {
                eventHandlers[index].handleAction.Invoke(() =>
                {
                    ScheduleHandlerByIndex(index + 1, eventData, scheduleEndAction);
                }, eventData, this);
            }
            else
            {
                isScheduleStopped = true;
                scheduleEndAction?.Invoke();
                
                Log.Info($"PriorityHandleEnd:{typeof(T)}");
                EventBus.Dispatch(new PriorityEventHandleEnd());
            }
        }

        public void StopSchedule()
        {
            isScheduleStopped = true;
        }

        public bool IsHandleEvent()
        {
            return !isScheduleStopped;
        }
    }
}
