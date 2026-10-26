using System;
using System.Collections.Generic;
using LearnLanguages.Core.Randomness;
using LearnLanguages.Learning.Data;
using LearnLanguages.Learning.Grammar;
using LearnLanguages.Learning.Tasks.Generators;
using LearnLanguages.Settings;

namespace LearnLanguages.Learning.Tasks
{
    /// <summary>
    /// Produces random language exercises matching the session settings.
    /// </summary>
    public sealed class LanguageTasksHandler
    {
        private readonly LanguageConfig _config;
        private readonly IReadOnlyList<ILanguageTaskGenerator> _generators;
        private readonly IConjugatorProvider _conjugators;
        private readonly IRandomService _random;
        private readonly List<ILanguageTaskGenerator> _candidates = new List<ILanguageTaskGenerator>();

        private LanguageTaskContext _cachedContext;
        private (Language target, Language ui, LanguageLevel level) _cachedKey;

        public LanguageTasksHandler(LanguageConfig config, List<ILanguageTaskGenerator> generators,
            IConjugatorProvider conjugators, IRandomService random)
        {
            _config = config;
            _generators = generators;
            _conjugators = conjugators;
            _random = random;
        }

        public BasicLanguageTaskData GetRandomLanguageTask(GameSettings settings)
        {
            var context = GetContext(settings);

            _candidates.Clear();
            var totalWeight = 0f;
            foreach (var generator in _generators)
            {
                if (!generator.CanGenerate(context)) continue;
                _candidates.Add(generator);
                totalWeight += _config.TasksSettings.GetWeight((int)generator.Type);
            }

            if (_candidates.Count == 0)
                throw new InvalidOperationException($"Not enough vocabulary for {settings.TargetLanguage} {settings.LanguageLevel}.");

            var roll = _random.Range(0f, totalWeight);
            foreach (var generator in _candidates)
            {
                roll -= _config.TasksSettings.GetWeight((int)generator.Type);
                if (roll <= 0f) return generator.Generate(context);
            }
            return _candidates[_candidates.Count - 1].Generate(context);
        }

        private LanguageTaskContext GetContext(GameSettings settings)
        {
            var key = (settings.TargetLanguage, settings.InterfaceLanguage, settings.LanguageLevel);
            if (_cachedContext != null && _cachedKey == key) return _cachedContext;

            var data = _config.Find(settings.TargetLanguage);
            if (data == null) throw new InvalidOperationException($"No vocabulary for {settings.TargetLanguage}.");

            _cachedKey = key;
            _cachedContext = new LanguageTaskContext(data, _conjugators.Get(settings.TargetLanguage),
                settings.InterfaceLanguage, settings.LanguageLevel, _config.TasksSettings, _random);
            return _cachedContext;
        }
    }
}
