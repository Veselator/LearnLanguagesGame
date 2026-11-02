using LearnLanguages.UI.Common;
using LearnLanguages.UI.Menu;
using UnityEngine;
using Zenject;

namespace LearnLanguages.Installers
{
    /// <summary>
    /// Bindings of the main menu scene.
    /// </summary>
    public sealed class MainMenuInstaller : MonoInstaller
    {
        [SerializeField] private ActionButtonView _actionButtonPrefab;
        [SerializeField] private AIProfileFieldView _profileFieldPrefab;

        public override void InstallBindings()
        {
            Container.BindMemoryPool<ActionButtonView, ActionButtonView.Pool>().WithInitialSize(4)
                .FromComponentInNewPrefab(_actionButtonPrefab).UnderTransformGroup("UiButtonsPool");
            Container.BindMemoryPool<AIProfileFieldView, AIProfileFieldView.Pool>().WithInitialSize(14)
                .FromComponentInNewPrefab(_profileFieldPrefab).UnderTransformGroup("UiFieldsPool");
        }
    }
}
