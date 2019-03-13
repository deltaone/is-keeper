using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
//using System.Linq;

//ToDo:
// add tags to messages
    //static public Dictionary<string, Delegate> eventTable = new Dictionary<string, Delegate>();
    ////Message handlers that should never be removed, regardless of calling Cleanup
    //static public List< string > permanentMessages = new List< string > ();
// change to MessageEventHandler - Action
    //public static void OnBroadcastMessage(string eventName, TypeOfMessage type) {
    //    if(type == TypeOfMessage.requireReceiver && !tableName.ContainsKey(eventName)) {
    //        throw new BroadcasterInner.MessageException(string.Format("Sending message {0} but no listener found.", eventName));

namespace Core
{
    public delegate void MessageEventHandler(ref object arg);

    public class MessageManager
    {
        private struct MessageHandler 
        {
            public MessageEventHandler handler;
            public bool persistent;
            public string tag;
        }

        private struct MessageEvent
        {
            public string tag;
            public string message;
            public object arg;
            public TimeSpan delay;
            public DateTime time;
            public long runsMax;
            public long runs;
        }

        private HashSet<string> _activeBroadcasts;
        private Dictionary<string, List<MessageHandler>> _messages;
        private List<MessageEvent> _events;

        private object _threadLock = new object();

        //---------------------------------------------------------------------
        public MessageManager()
        {
            Reset(true);
        }

        public void Reset(bool fullReset = true)
        {
            lock (_threadLock)
            {
                if (fullReset)
                {
                    _activeBroadcasts = new HashSet<string>();
                    _messages = new Dictionary<string, List<MessageHandler>>();
                    _events = new List<MessageEvent>();                    
                    return;
                }

                foreach (var handlers in _messages)
                    for (int i = 0; i < handlers.Value.Count; i++)
                        if (handlers.Value[i].persistent == false) handlers.Value.RemoveAt(i--);
            }
        }

        public void Subscribe(string message, MessageEventHandler handler, bool persistent)
        {
            Subscribe(message, handler, persistent, null);
        }

        public void Subscribe(string message, MessageEventHandler handler, string tag)
        {
            Subscribe(message, handler, false, tag);
        }

        public void Subscribe(string message, MessageEventHandler handler, bool persistent = false, string tag = null)
        {
            lock (_threadLock)
            {
                if (!_messages.ContainsKey(message))
                    _messages.Add(message, new List<MessageHandler>());

                var subscribers = _messages[message];
                foreach (var s in subscribers)
                {
                    if (s.handler == handler)
                    {
                        GM.Warning(String.Format("Subscriber already added <{0}> !", handler.Method), 1);
                        return;
                    }
                }
                subscribers.Add(new MessageHandler() { handler = handler, persistent = persistent, tag = tag });
            }
        }

        public void UnsubscribeByTag(string tag)
        {
            lock (_threadLock)
            {
                //foreach (string key in _messages.Keys.ToArray()) // req System.Linq extension
                //(new List<string>(_messages.Keys)).ToArray() 
                foreach (string key in (new List<string>(_messages.Keys)))
                {
                    for (int i = 0; i < _messages[key].Count; i++)
                    {
                        if (_messages[key][i].tag != tag) continue;
                        _messages[key].RemoveAt(i--);
                    }
                    if (_messages[key].Count == 0) _messages.Remove(key);
                }
            }
        }

        public void Unsubscribe(string message, MessageEventHandler handler = null)
        {
            lock (_threadLock)
            {
                if (!_messages.ContainsKey(message))
                {
                    GM.Warning(String.Format("Try to unsubscribe from nonexistent message '{0}'!", message), 1);
                    return;
                }

                if (handler == null)
                {
                    _messages.Remove(message);
                    return;
                }

                var subscribers = _messages[message];
                for (var i = 0; i < subscribers.Count; i++)
                {
                    if (subscribers[i].handler != handler) continue;
                    subscribers.RemoveAt(i);
                    if (subscribers.Count == 0) _messages.Remove(message);
                    return;
                }                
                GM.Warning(String.Format("Try to unsubscribe nonexistent handler '{0}' from '{1}'!", 
                    handler.Method.ReflectedType.Name + "." + handler.Method.Name + "()", message), 1);
            }
        }

        public void Broadcast(string message, object arg = null)
        {
            lock (_threadLock)
            {
                if (!_messages.ContainsKey(message))
                    GM.Warning(String.Format("Try to broadcast nonexistent message '{0}'!", message), 1);
                else if (_activeBroadcasts.Contains(message))
                    GM.Warning(String.Format("Message '{0}' already broadcasted!", message), 1);
                else 
                {
                    _activeBroadcasts.Add(message);
                    var listeners = _messages[message];
                    foreach (var e in listeners)
                    {
                        try
                        {
                            ((MessageEventHandler)e.handler)(ref arg);
                        }
                        catch (Exception ex)
                        {
                            GM.Warning(String.Format("Exception on message '{0}'!", message), 1);
                            GM.Log(String.Format("Exception: {0}\n{1}", ex.Message, ex.StackTrace));
                        }
                    }
                    _activeBroadcasts.Remove(message);
                }                
            }
        }

        public void EventAddDelayed(string message, object arg, TimeSpan delay)
        {
            EventAdd(message, arg, delay, "", 1);
        }

        public void EventEnqueue(string message, object arg)
        {
            EventAdd(message, arg, TimeSpan.FromMilliseconds(0), "", 1);
        }

        public void EventAdd(string message, object arg, TimeSpan delay, string tag = "", int runs = 0)
        {    
            lock (_threadLock)
            {
                _events.Add(new MessageEvent()
                {
                    tag = tag,
                    message = message,
                    arg = arg,
                    delay = delay,
                    time = DateTime.Now,
                    runsMax = runs,
                    runs = 0,
                });
            }
        }

        public void EventRemoveByTag(string tag)
        {
            lock (_threadLock)
            {
                for (int i = 0; i < _events.Count; i++)
                {
                    if (!String.Equals(_events[i].tag, tag, StringComparison.InvariantCultureIgnoreCase)) continue;
                    _events.RemoveAt(i--);
                }
            }
        }

        public void EventRemoveByMessage(string message)
        {
            lock (_threadLock)
            {
                for (int i = 0; i < _events.Count; i++)
                {
                    if (!String.Equals(_events[i].message, message, StringComparison.InvariantCultureIgnoreCase)) continue;
                    _events.RemoveAt(i--);
                }
            }
        }

        public void UpdateEvents()
        {
            lock (_threadLock)
            {
                for (int i = 0; i < _events.Count; i++)
                {
                    if (_events[i].time + _events[i].delay >= DateTime.Now) continue;
                    Broadcast(_events[i].message, _events[i].arg);
                    var e = _events[i];
                    e.time = DateTime.Now;
                    _events[i] = e;
                    if (_events[i].runsMax == 0) continue;
                    e.runs++;
                    _events[i] = e;
                    if (_events[i].runs < _events[i].runsMax) continue;
                    _events.RemoveAt(i--);
                }
            }
        }

        public void Dump()
        {
            lock (_threadLock)
            {
                GM.Log("Messenger have " + _messages.Count.ToString() + " registered messages");
                foreach (var m in _messages)
                {
                    GM.Log("   '" + m.Key + "' have '" + _messages[m.Key].Count + "' handlers");
                    foreach (var h in _messages[m.Key])
                    {
                        foreach (var f in h.handler.GetInvocationList())
                        {
                            GM.Log("      " + f.Method.ReflectedType.Name + "." + f.Method.Name + "()");
                        }                        
                    }
                }
            }
        }
    }
}