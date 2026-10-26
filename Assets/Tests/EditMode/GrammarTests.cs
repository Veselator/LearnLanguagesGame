using System.Linq;
using LearnLanguages.Learning.Data;
using LearnLanguages.Learning.Grammar;
using NUnit.Framework;

namespace LearnLanguages.Tests
{
    public sealed class GrammarTests
    {
        private static WordEntry FrenchVerb(string word) =>
            TestAssets.LanguageConfig.Find(Settings.Language.French).Entries.First(e => e.Word == word);

        private static WordEntry EnglishVerb(string word) =>
            TestAssets.LanguageConfig.Find(Settings.Language.English).Entries.First(e => e.Word == word);

        [TestCase("parler", Tense.Present, Person.FirstPlural, "nous parlons")]
        [TestCase("manger", Tense.Past, Person.FirstPlural, "nous mangions")]
        [TestCase("manger", Tense.Past, Person.FirstSingular, "je mangeais")]
        [TestCase("commencer", Tense.Present, Person.FirstPlural, "nous commençons")]
        [TestCase("finir", Tense.Present, Person.ThirdPlural, "ils finissent")]
        [TestCase("vendre", Tense.Future, Person.SecondPlural, "vous vendrez")]
        [TestCase("être", Tense.Past, Person.FirstSingular, "j'étais")]
        [TestCase("aller", Tense.Conditional, Person.FirstSingular, "j'irais")]
        [TestCase("aimer", Tense.Present, Person.FirstSingular, "j'aime")]
        public void FrenchConjugation(string verb, Tense tense, Person person, string expected) =>
            Assert.AreEqual(expected, new FrenchConjugator().ConjugateWithPronoun(FrenchVerb(verb), tense, person));

        [TestCase("go", Tense.Present, Person.ThirdSingular, "he goes")]
        [TestCase("carry", Tense.Past, Person.FirstSingular, "I carried")]
        [TestCase("be", Tense.Past, Person.ThirdPlural, "they were")]
        [TestCase("have", Tense.Present, Person.ThirdSingular, "he has")]
        [TestCase("fight", Tense.Future, Person.FirstPlural, "we will fight")]
        public void EnglishConjugation(string verb, Tense tense, Person person, string expected) =>
            Assert.AreEqual(expected, new EnglishConjugator().ConjugateWithPronoun(EnglishVerb(verb), tense, person));
    }
}
