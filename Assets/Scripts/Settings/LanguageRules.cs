using System.Collections.Generic;

namespace LearnLanguages.Settings
{
    /// <summary>
    /// Which languages can be trained and which can be used for the interface.
    /// </summary>
    public static class LanguageRules
    {
        public static readonly IReadOnlyList<Language> TrainableLanguages = new[] { Language.English, Language.French };

        public static readonly IReadOnlyList<Language> InterfaceLanguages =
            new[] { Language.Russian, Language.Ukrainian, Language.English, Language.French };

        public static bool IsValidPair(Language target, Language interfaceLanguage) => target != interfaceLanguage;
    }
}
