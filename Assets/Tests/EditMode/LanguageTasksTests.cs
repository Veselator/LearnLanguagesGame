using System.Collections.Generic;
using System.Linq;
using LearnLanguages.Core.Randomness;
using LearnLanguages.Learning.Grammar;
using LearnLanguages.Learning.Tasks;
using LearnLanguages.Learning.Tasks.Generators;
using LearnLanguages.Localization;
using LearnLanguages.Settings;
using NUnit.Framework;

namespace LearnLanguages.Tests
{
    public sealed class LanguageTasksTests
    {
        private LanguageTasksHandler _handler;

        [SetUp]
        public void SetUp()
        {
            var localization = new LocalizationService(TestAssets.Localization);
            var generators = new List<ILanguageTaskGenerator>
            {
                new ChooseTranslationGenerator(), new TypeTranslationGenerator(), new OddWordOutGenerator(),
                new ChooseTenseGenerator(localization), new ConjugateVerbGenerator(localization)
            };
            var conjugators = new ConjugatorProvider(new IConjugator[] { new EnglishConjugator(), new FrenchConjugator() });
            _handler = new LanguageTasksHandler(TestAssets.LanguageConfig, generators, conjugators, new RandomService(42));
        }

        [Test]
        public void EveryCombinationProducesValidTasks()
        {
            var seenTypes = new HashSet<LanguageTaskType>();
            foreach (var target in LanguageRules.TrainableLanguages)
            foreach (var ui in LanguageRules.InterfaceLanguages)
            {
                if (!LanguageRules.IsValidPair(target, ui)) continue;
                foreach (LanguageLevel level in System.Enum.GetValues(typeof(LanguageLevel)))
                {
                    var settings = new GameSettings("test", target, ui, level, GameDifficulty.Medium, 1, 0);
                    for (var i = 0; i < 60; i++)
                    {
                        var task = _handler.GetRandomLanguageTask(settings);
                        seenTypes.Add(task.Type);
                        AssertValid(task);
                    }
                }
            }
            Assert.AreEqual(5, seenTypes.Count, "All exercise types should appear.");
        }

        private static void AssertValid(BasicLanguageTaskData task)
        {
            Assert.IsNotEmpty(task.InstructionKey);
            Assert.Greater(task.TimeLimit, 0f);
            switch (task)
            {
                case ChoiceTaskData choice:
                    Assert.AreEqual(4, choice.Options.Count, task.Type.ToString());
                    Assert.AreEqual(choice.Options.Count, choice.Options.Distinct().Count(), "Options must be distinct");
                    Assert.IsTrue(choice.IsCorrect(choice.CorrectIndex));
                    break;
                case TextInputTaskData input:
                    Assert.IsTrue(input.IsCorrect(input.CorrectAnswerText));
                    Assert.IsTrue(input.IsCorrect("  " + input.CorrectAnswerText.ToUpperInvariant() + " "));
                    Assert.IsFalse(input.IsCorrect("definitely wrong answer"));
                    break;
                default:
                    Assert.Fail("Unknown task type");
                    break;
            }
        }

        [Test]
        public void AccentsAreForgivenInTypedAnswers()
        {
            var task = new TextInputTaskData(LanguageTaskType.TypeTranslation, "k", "p", 10f, new[] { "épée" });
            Assert.IsTrue(task.IsCorrect("epee"));
            Assert.IsTrue(task.IsCorrect("ÉPÉE"));
        }
    }
}
