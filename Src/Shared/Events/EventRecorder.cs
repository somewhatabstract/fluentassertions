using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace FluentAssertions.Events
{
    /// <summary>
    ///   Records activity for a single event.
    /// </summary>
    [DebuggerNonUserCode]
    public class EventRecorder : IEventRecorder
    {
        private readonly IList<RecordedEvent> raisedEvents = new List<RecordedEvent>();
        private readonly object lockable = new object();
        private WeakReference eventObject;
        private bool isDisposed;

#if !SILVERLIGHT && !WINRT && !PORTABLE && !CORE_CLR
        private readonly EventInfo eventInfo;

        /// <summary>
        /// </summary>
        /// <param name = "eventRaiser">The object events are recorded from</param>
        /// <param name = "targetEventInfo">The <see cref="EventInfo" /> for the event that's recorded</param>
        public EventRecorder(object eventRaiser, EventInfo targetEventInfo)
        {
            EventObject = eventRaiser;
            eventInfo = targetEventInfo;
            EventName = eventInfo.Name;
            Handler = EventHandlerFactory.GenerateHandler(eventInfo.EventHandlerType, this);
            eventInfo.AddEventHandler(eventRaiser, Handler);
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                Reset();
                eventInfo.RemoveEventHandler(EventObject, Handler);
            }
        }

        public Delegate Handler { get; }
#else
        /// <summary>
        /// </summary>
        /// <param name = "eventRaiser">The object events are recorded from</param>
        public EventRecorder(System.ComponentModel.INotifyPropertyChanged eventRaiser)
        {
            EventObject = eventRaiser;
            EventName = "PropertyChanged";
            eventRaiser.PropertyChanged += OnRecordEvent;
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                Reset();
                var notifier = (EventObject as System.ComponentModel.INotifyPropertyChanged);
                if (notifier != null)
                {
                    notifier.PropertyChanged -= OnRecordEvent;
                }
            }
        }

        private void OnRecordEvent(object sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            RecordEvent(sender, args);
        }
#endif

        /// <summary>
        ///   The object events are recorded from
        /// </summary>
        public object EventObject
        {
            get { return eventObject?.Target; }
            private set { eventObject = new WeakReference(value); }
        }

        /// <summary>
        ///   The name of the event that's recorded
        /// </summary>
        public string EventName { get; }

        /// <summary>
        ///   Enumerate raised events
        /// </summary>
        public IEnumerator<RecordedEvent> GetEnumerator()
        {
            lock (lockable)
            {
                return raisedEvents.ToList().GetEnumerator();
            }
        }

        /// <summary>
        ///   Enumerate raised events
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (lockable)
            {
                return raisedEvents.ToList().GetEnumerator();
            }
        }

        /// <summary>
        ///   Called by the auto-generated IL, to record information about a raised event.
        /// </summary>
        public void RecordEvent(params object [] parameters)
        {
            lock (lockable)
            {
                raisedEvents.Add(new RecordedEvent(EventObject, parameters));
            }
        }

        /// <summary>
        ///   Resets recorder to clear records of events raised so far.
        /// </summary>
        public void Reset()
        {
            lock (lockable)
            {
                raisedEvents.Clear();
            }
        }
    }
}