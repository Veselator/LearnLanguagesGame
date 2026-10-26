using System;
using System.Collections.Generic;

namespace LearnLanguages.Core.Events
{
    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();
        private readonly Stack<List<Delegate>> _snapshotPool = new Stack<List<Delegate>>();

        public void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) return;
            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                list = new List<Delegate>();
                _handlers.Add(typeof(T), list);
            }
            if (!list.Contains(handler)) list.Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) return;
            if (_handlers.TryGetValue(typeof(T), out var list)) list.Remove(handler);
        }

        public void Publish<T>(T gameEvent)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list) || list.Count == 0) return;

            // Handlers may (un)subscribe while being notified, so iterate over a pooled snapshot.
            var snapshot = _snapshotPool.Count > 0 ? _snapshotPool.Pop() : new List<Delegate>();
            snapshot.AddRange(list);
            try
            {
                for (var i = 0; i < snapshot.Count; i++) ((Action<T>)snapshot[i]).Invoke(gameEvent);
            }
            finally
            {
                snapshot.Clear();
                _snapshotPool.Push(snapshot);
            }
        }
    }
}
