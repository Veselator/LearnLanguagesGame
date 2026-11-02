using System;
using System.Collections.Generic;
using UnityEngine;

namespace LearnLanguages.Gameplay.AI
{
    /// <summary>
    /// The models the game ships with; players edit their own copies through <see cref="IAIProfilesStore"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "LearnLanguages/AI/Profiles Config", fileName = "AIProfilesConfig")]
    public sealed class AIProfilesConfig : ScriptableObject
    {
        [SerializeField] private AIProfile[] _defaults = Array.Empty<AIProfile>();

        public IReadOnlyList<AIProfile> Defaults => _defaults;

        public void SetDefaults(AIProfile[] defaults) => _defaults = defaults ?? Array.Empty<AIProfile>();
    }
}
