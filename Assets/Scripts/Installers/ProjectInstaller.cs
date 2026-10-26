using LearnLanguages.Core.Events;
using LearnLanguages.Core.Input;
using LearnLanguages.Core.Randomness;
using LearnLanguages.Learning.Grammar;
using LearnLanguages.Learning.Tasks;
using LearnLanguages.Learning.Tasks.Generators;
using LearnLanguages.Localization;
using LearnLanguages.Save;
using LearnLanguages.Settings;
using LearnLanguages.UI;
using LearnLanguages.UI.Common;
using Zenject;

namespace LearnLanguages.Installers
{
    /// <summary>
    /// Application-wide services living across scenes.
    /// </summary>
    public sealed class ProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<IEventBus>().To<EventBus>().AsSingle();
            Container.Bind<IRandomService>().To<RandomService>().AsSingle();
            Container.Bind<IInputService>().To<InputService>().AsSingle();
            Container.Bind<IGameSession>().To<GameSession>().AsSingle();
            Container.Bind<ISaveService>().To<FileSaveService>().AsSingle();
            Container.Bind<ISceneLoader>().To<SceneLoader>().AsSingle();
            Container.Bind<ILocalizationService>().To<LocalizationService>().AsSingle();
            Container.Bind<UiFormat>().AsSingle();

            // Language rules (strategy per trained language) and exercise generators (strategy per task type).
            Container.Bind<IConjugator>().To<EnglishConjugator>().AsSingle();
            Container.Bind<IConjugator>().To<FrenchConjugator>().AsSingle();
            Container.Bind<IConjugatorProvider>().To<ConjugatorProvider>().AsSingle();
            Container.Bind<ILanguageTaskGenerator>().To<ChooseTranslationGenerator>().AsSingle();
            Container.Bind<ILanguageTaskGenerator>().To<TypeTranslationGenerator>().AsSingle();
            Container.Bind<ILanguageTaskGenerator>().To<OddWordOutGenerator>().AsSingle();
            Container.Bind<ILanguageTaskGenerator>().To<ChooseTenseGenerator>().AsSingle();
            Container.Bind<ILanguageTaskGenerator>().To<ConjugateVerbGenerator>().AsSingle();
            Container.Bind<LanguageTasksHandler>().AsSingle();
        }
    }
}
