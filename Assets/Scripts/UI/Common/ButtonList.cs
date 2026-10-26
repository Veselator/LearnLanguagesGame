using System;
using System.Collections.Generic;
using UnityEngine;

namespace LearnLanguages.UI.Common
{
    /// <summary>
    /// A container filled with pooled <see cref="ActionButtonView"/> items.
    /// </summary>
    public sealed class ButtonList
    {
        private readonly Transform _container;
        private readonly ActionButtonView.Pool _pool;
        private readonly List<ActionButtonView> _items = new List<ActionButtonView>();

        public ButtonList(Transform container, ActionButtonView.Pool pool)
        {
            _container = container;
            _pool = pool;
        }

        public void Clear()
        {
            foreach (var item in _items) _pool.Despawn(item);
            _items.Clear();
        }

        public void Add(string title, string subtitle, bool interactable, Action onClick)
        {
            var item = _pool.Spawn();
            item.transform.SetParent(_container, false);
            item.transform.SetAsLastSibling();
            item.Bind(title, subtitle, interactable, onClick);
            _items.Add(item);
        }
    }
}
