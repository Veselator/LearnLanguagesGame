# L'Apprendeur

A turn-based strategy game for learning English or French: a global map (Unity 3D terrain), 2D battles on 6 lines × 30 cells, and a language mini-game in every clash.

## Running

- `Assets/Scenes/MainMenu.unity` — entry point (new game, continue, interface language).
- `Assets/Scenes/Game.unity` — can be started directly: the match begins with default settings (French, Russian interface, A1).
- `Assets/Scenes/MapCreator.unity` — province editor (Unity Editor only).

Map controls: LMB — select a province/army, RMB — move/attack with the selected army, WASD/arrows/screen edges/middle mouse — camera, wheel — zoom, Space — end turn, Esc — pause.

Battle: click one of your cohorts, then a cell (move) or an enemy (attack/shoot); the buttons give Defend, War cry, Shoot and Execute. Works both in the scene and on the grid below. Space — start the battle / execute orders / continue. A/D and arrows pan along the lines, W/S and the wheel zoom. Planned paths are shown with dots, the destination with a cross, a shooting target with a ring.

## Architecture

- **DI** — Extenject (Zenject): `ProjectInstaller` + `GameConfigsInstaller` (Resources/ProjectContext) and `GameInstaller` (Game scene).
- **EventBus** (`Core/Events`) connects logic and visuals; the logic (`Gameplay`, `Battle/Logic`, `Learning`) knows nothing about MonoBehaviours.
- **Command** — `MapCommand` on the map (player and AI moves both go through `MapCommandProcessor`), `BattleOrder` in battle.
- **Strategy** — language grammar (`IConjugator`), exercise generators (`ILanguageTaskGenerator`), AI (`BaseAILogic`: Easy/Medium/Hard, minimax; `IBattleAI`), duel participants, camera modes (`ICurrentCameraMethod`).
- **Factory / Pool** — `ArmyFactory`, Zenject factories and `MonoMemoryPool` for every runtime object; all visual objects are prefabs in `Assets/Prefabs`.
- **Data** — ScriptableObjects in `Assets/Data` (vocabularies, localization, units, buildings, economy, maps, battlefields).
- **Saves** — JSON in `Application.persistentDataPath/savegame.json`, autosave at the start of the player's turn.

## MapCreator

Select the `MapCreator` object in the scene of the same name:
1. *Configure texture import settings* — makes the map images readable.
2. *Sync provinces with image* — creates/updates provinces from the colors of `Provinces.png` (tiny anti-aliasing slivers merge into neighbours), neighbours and positions.
3. Clicking a province in the Scene view selects it; the inspector edits type, landscape, settlement, production and spawn points; handles in the scene move the settlement, port and army positions.
4. *Generate map icon* — the icon for the menu.

Battlefields (`Assets/Data/Battle/Battlefields`) are painted with a brush right in the inspector; probabilities per landscape live in `BattlefieldMapsConfig`.

## Tests

`Window → General → Test Runner → EditMode` — grammar, exercise generation, battle simulation, map graph.
