using System;
using System.Collections.Generic;
using UnityEngine;

namespace LearnLanguages.Map.Data
{
    [CreateAssetMenu(menuName = "LearnLanguages/Map/Maps Config", fileName = "MapsConfig")]
    public sealed class MapsConfig : ScriptableObject
    {
        [SerializeField] private Map[] _maps = Array.Empty<Map>();

        public IReadOnlyList<Map> Maps => _maps;

        public Map GetById(string id)
        {
            foreach (var map in _maps)
                if (map != null && map.Id == id) return map;
            return _maps.Length > 0 ? _maps[0] : null;
        }
    }
}
