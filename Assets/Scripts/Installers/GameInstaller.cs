using LearnLanguages.Battle.AI;
using LearnLanguages.Battle.Logic;
using LearnLanguages.Battle.Visual;
using LearnLanguages.Cameras;
using LearnLanguages.Gameplay.AI;
using LearnLanguages.Gameplay.Commands;
using LearnLanguages.Gameplay.Conquest;
using LearnLanguages.Gameplay.Economy;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Gameplay.Selection;
using LearnLanguages.Gameplay.Turns;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Gameplay.World;
using LearnLanguages.Learning.Tasks;
using LearnLanguages.Map.Logic;
using LearnLanguages.Map.Visual;
using LearnLanguages.Save;
using LearnLanguages.UI.Battle;
using LearnLanguages.UI.Common;
using LearnLanguages.UI.Global;
using LearnLanguages.UI.LanguageTasks;
using UnityEngine;
using Zenject;

namespace LearnLanguages.Installers
{
    /// <summary>
    /// Bindings of the game scene: world state, rules, turns, AI, battle and their views.
    /// </summary>
    public sealed class GameInstaller : MonoInstaller
    {
        [Header("Scene objects")]
        [SerializeField] private CameraController _cameraController;
        [SerializeField] private GameMapView _mapView;
        [SerializeField] private BattleView _battleView;
        [SerializeField] private LanguageTaskVisual _languageTaskVisual;
        [SerializeField] private ScreenFader _screenFader;

        [Header("Prefabs")]
        [SerializeField] private VisualGameTile _tilePrefab;
        [SerializeField] private ArmyView _armyPrefab;
        [SerializeField] private BattleTileView _battleTilePrefab;
        [SerializeField] private CohortView _cohortPrefab;
        [SerializeField] private FloatingTextView _floatingTextPrefab;
        [SerializeField] private PathMarkerView _pathMarkerPrefab;
        [SerializeField] private ActionButtonView _actionButtonPrefab;
        [SerializeField] private NotificationItemView _notificationPrefab;
        [SerializeField] private BattleGridCellView _gridCellPrefab;

        public override void InstallBindings()
        {
            BindWorld();
            BindRules();
            BindTurnsAndAi();
            BindBattle();
            BindLanguageTasks();
            BindViews();
        }

        private void BindWorld()
        {
            Container.BindInterfacesAndSelfTo<CurrentMap>().AsSingle();
            Container.BindInterfacesAndSelfTo<GameMap>().AsSingle();
            Container.BindInterfacesAndSelfTo<ArmiesRegistry>().AsSingle();
            Container.BindInterfacesAndSelfTo<PlayersRegistry>().AsSingle();
            Container.Bind<IArmyFactory>().To<ArmyFactory>().AsSingle();
            Container.Bind<IGameWorldBuilder>().To<GameWorldBuilder>().AsSingle();
            Container.BindInterfacesTo<GameBootstrapper>().AsSingle();
            Container.BindInterfacesTo<GameSaveController>().AsSingle();
            Container.BindInterfacesTo<SelectionService>().AsSingle();
        }

        private void BindRules()
        {
            Container.Bind<IEconomyService>().To<EconomyService>().AsSingle();
            Container.Bind<IConstructionRules>().To<ConstructionRules>().AsSingle();
            Container.Bind<IMovementRules>().To<MovementRules>().AsSingle();
            Container.Bind<IConquestService>().To<ConquestService>().AsSingle();
            Container.Bind<IWarService>().To<WarService>().AsSingle();
        }

        private void BindTurnsAndAi()
        {
            Container.Bind<IGlobalTurnsManager>().To<GlobalTurnsManager>().AsSingle();
            Container.Bind<MapCommandContext>().AsSingle();
            Container.BindInterfacesTo<MapCommandProcessor>().AsSingle();
            Container.Bind<IHumanTurnState>().To<HumanTurnState>().AsSingle();

            Container.Bind<BaseAILogic>().To<EasyAI>().AsSingle();
            Container.Bind<BaseAILogic>().To<MediumAI>().AsSingle();
            Container.Bind<BaseAILogic>().To<HardAI>().AsSingle();
            Container.BindInterfacesAndSelfTo<AIHandler>().AsSingle();
        }

        private void BindBattle()
        {
            Container.Bind<IBattleAI>().To<EasyBattleAI>().AsSingle();
            Container.Bind<IBattleAI>().To<MediumBattleAI>().AsSingle();
            Container.Bind<IBattleAI>().To<HardBattleAI>().AsSingle();
            Container.Bind<IBattleAIProvider>().To<BattleAIProvider>().AsSingle();
            Container.Bind<IDuelResolver>().To<DuelResolver>().AsSingle();
            Container.Bind<BattleTurnsManager>().AsSingle();
            Container.BindInterfacesTo<BattleController>().AsSingle();
            Container.Bind<IBattleSimulator>().To<BattleSimulator>().AsSingle();
            Container.Bind<IBattleService>().To<BattleService>().AsSingle();
            Container.Bind<IBattleSelection>().To<BattleSelection>().AsSingle();
        }

        private void BindLanguageTasks()
        {
            Container.Bind<ILanguageTaskView>().FromInstance(_languageTaskVisual);
            Container.BindInterfacesTo<InGameLanguageTasksManager>().AsSingle();
        }

        private void BindViews()
        {
            Container.BindInstance(_cameraController);
            Container.BindInstance(_mapView);
            Container.BindInstance(_battleView);
            Container.Bind<IScreenFader>().FromInstance(_screenFader);
            Container.Bind<RtsCameraMethod>().AsSingle();
            Container.Bind<BattleCameraMethod>().AsSingle();
            Container.Bind<TransitionCameraMethod>().AsSingle();
            Container.BindInterfacesTo<MapInputController>().AsSingle();
            Container.BindInterfacesTo<BattleInputController>().AsSingle();

            Container.BindFactory<VisualGameTile, VisualGameTile.Factory>().FromComponentInNewPrefab(_tilePrefab)
                .UnderTransformGroup("ProvinceVisuals");
            Container.BindMemoryPool<ArmyView, ArmyView.Pool>().WithInitialSize(8).FromComponentInNewPrefab(_armyPrefab)
                .UnderTransformGroup("Armies");
            Container.BindMemoryPool<BattleTileView, BattleTileView.Pool>().WithInitialSize(180)
                .FromComponentInNewPrefab(_battleTilePrefab).UnderTransformGroup("BattleTiles");
            Container.BindMemoryPool<CohortView, CohortView.Pool>().WithInitialSize(20)
                .FromComponentInNewPrefab(_cohortPrefab).UnderTransformGroup("BattleCohorts");
            Container.BindMemoryPool<FloatingTextView, FloatingTextView.Pool>().WithInitialSize(10)
                .FromComponentInNewPrefab(_floatingTextPrefab).UnderTransformGroup("FloatingTexts");
            Container.BindMemoryPool<PathMarkerView, PathMarkerView.Pool>().WithInitialSize(40)
                .FromComponentInNewPrefab(_pathMarkerPrefab).UnderTransformGroup("PathMarkers");
            Container.BindMemoryPool<ActionButtonView, ActionButtonView.Pool>().WithInitialSize(16)
                .FromComponentInNewPrefab(_actionButtonPrefab).UnderTransformGroup("UiButtonsPool");
            Container.BindMemoryPool<NotificationItemView, NotificationItemView.Pool>().WithInitialSize(5)
                .FromComponentInNewPrefab(_notificationPrefab).UnderTransformGroup("UiNotificationsPool");
            Container.BindMemoryPool<BattleGridCellView, BattleGridCellView.Pool>().WithInitialSize(180)
                .FromComponentInNewPrefab(_gridCellPrefab).UnderTransformGroup("UiGridCellsPool");
        }
    }
}
