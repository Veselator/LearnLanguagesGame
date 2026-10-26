using System.Collections.Generic;
using LearnLanguages.Core.Events;
using LearnLanguages.Gameplay.Events;
using UnityEngine;
using Zenject;

namespace LearnLanguages.UI.Global
{
    /// <summary>
    /// Toast messages stacked in a corner.
    /// </summary>
    public sealed class NotificationsView : MonoBehaviour
    {
        [SerializeField] private Transform _container;
        [SerializeField] private int _maxVisible = 5;

        private readonly List<NotificationItemView> _items = new List<NotificationItemView>();
        private IEventBus _eventBus;
        private NotificationItemView.Pool _pool;

        [Inject]
        public void Construct(IEventBus eventBus, NotificationItemView.Pool pool)
        {
            _eventBus = eventBus;
            _pool = pool;
            _eventBus.Subscribe<NotificationEvent>(OnNotification);
        }

        private void OnDestroy() => _eventBus?.Unsubscribe<NotificationEvent>(OnNotification);

        private void OnNotification(NotificationEvent e)
        {
            if (_items.Count >= _maxVisible) Remove(_items[0]);
            var item = _pool.Spawn();
            item.transform.SetParent(_container, false);
            item.transform.SetAsLastSibling();
            item.Show(e.Text, Remove);
            _items.Add(item);
        }

        private void Remove(NotificationItemView item)
        {
            if (!_items.Remove(item)) return;
            _pool.Despawn(item);
        }
    }
}
