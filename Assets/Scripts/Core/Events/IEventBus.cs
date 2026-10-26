using System;

namespace LearnLanguages.Core.Events
{
    /// <summary>
    /// Typed publish/subscribe channel used to decouple game logic from its visualisation.
    /// </summary>
    public interface IEventBus
    {
        void Subscribe<T>(Action<T> handler);
        void Unsubscribe<T>(Action<T> handler);
        void Publish<T>(T gameEvent);
    }
}
