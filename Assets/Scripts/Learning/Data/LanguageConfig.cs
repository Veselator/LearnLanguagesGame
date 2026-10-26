using System;
using System.Collections.Generic;
using LearnLanguages.Settings;
using UnityEngine;

namespace LearnLanguages.Learning.Data
{
    [CreateAssetMenu(menuName = "LearnLanguages/Language Config", fileName = "LanguageConfig")]
    public sealed class LanguageConfig : ScriptableObject
    {
        [SerializeField] private LanguageData[] _languages = Array.Empty<LanguageData>();
        [SerializeField] private LanguageTasksSettings _tasksSettings = new LanguageTasksSettings();

        public IReadOnlyList<LanguageData> Languages => _languages;
        public LanguageTasksSettings TasksSettings => _tasksSettings;

        public LanguageData Find(Language language)
        {
            foreach (var data in _languages)
                if (data != null && data.TargetLanguage == language) return data;
            return null;
        }
    }

    [Serializable]
    public sealed class LanguageTasksSettings
    {
        [SerializeField] private float _choiceTimeLimit = 15f;
        [SerializeField] private float _inputTimeLimit = 25f;
        [SerializeField] private float _resultDisplayTime = 1.4f;
        [Tooltip("Relative weights: ChooseTranslation, TypeTranslation, OddWordOut, ChooseTense, Conjugate.")]
        [SerializeField] private float[] _taskWeights = { 3f, 2f, 2f, 1.5f, 1.5f };

        public float ChoiceTimeLimit => _choiceTimeLimit;
        public float InputTimeLimit => _inputTimeLimit;
        public float ResultDisplayTime => _resultDisplayTime;

        public float GetWeight(int taskTypeIndex) =>
            _taskWeights != null && taskTypeIndex < _taskWeights.Length ? _taskWeights[taskTypeIndex] : 1f;
    }
}
