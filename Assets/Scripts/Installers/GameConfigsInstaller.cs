using LearnLanguages.Battle.Data;
using LearnLanguages.Battle.Visual;
using LearnLanguages.Cameras;
using LearnLanguages.Gameplay.Economy;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Learning.Data;
using LearnLanguages.Localization;
using LearnLanguages.Map.Data;
using UnityEngine;
using Zenject;

namespace LearnLanguages.Installers
{
    /// <summary>
    /// Binds every configuration ScriptableObject for the whole application.
    /// </summary>
    [CreateAssetMenu(menuName = "LearnLanguages/Installers/Game Configs Installer", fileName = "GameConfigsInstaller")]
    public sealed class GameConfigsInstaller : ScriptableObjectInstaller<GameConfigsInstaller>
    {
        [SerializeField] private LocalizationTable _localization;
        [SerializeField] private LanguageConfig _languages;
        [SerializeField] private MapsConfig _maps;
        [SerializeField] private EconomyConfig _economy;
        [SerializeField] private UpgradesConfig _upgrades;
        [SerializeField] private UnitsConfig _units;
        [SerializeField] private PlayersConfig _players;
        [SerializeField] private BattleConfig _battle;
        [SerializeField] private BattleTileVisualConfig _battleTiles;
        [SerializeField] private CameraSettings _camera;

        public override void InstallBindings()
        {
            Container.BindInstance(_localization);
            Container.BindInstance(_languages);
            Container.BindInstance(_maps);
            Container.BindInstance(_economy);
            Container.BindInstance(_upgrades);
            Container.BindInstance(_units);
            Container.BindInstance(_players);
            Container.BindInstance(_battle);
            Container.BindInstance(_battleTiles);
            Container.BindInstance(_camera);
        }
    }
}
