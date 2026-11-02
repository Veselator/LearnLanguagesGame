using System.Collections.Generic;
using LearnLanguages.Gameplay.Economy;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Map.Data;
using UnityEngine;

namespace LearnLanguages.Gameplay.AI
{
    public sealed class AIBuildOption
    {
        public AIBuildOption(int index, int tileId, UpgradeDefinition upgrade, float incomeGain)
        {
            Index = index;
            TileId = tileId;
            Upgrade = upgrade;
            IncomeGain = incomeGain;
        }

        public int Index { get; }
        public int TileId { get; }
        public UpgradeDefinition Upgrade { get; }
        public float IncomeGain { get; }
    }

    public sealed class AIRecruitOption
    {
        public AIRecruitOption(int tileId, UnitTypeDefinition unit, ResourceBundle cost)
        {
            TileId = tileId;
            Unit = unit;
            Cost = cost;
        }

        public int TileId { get; }
        public UnitTypeDefinition Unit { get; }
        public ResourceBundle Cost { get; }
    }

    /// <summary>
    /// What does not change during the search: the map graph and the options available this turn.
    /// </summary>
    public sealed class AIWorld
    {
        private const int NearRadius = 2;

        private readonly bool[][] _near;

        public AIWorld(AIContext context)
        {
            PlayerId = context.Player.Id;
            CapitalTile = context.Player.CapitalTileId;

            var map = context.Map;
            TileCount = map.Count;
            Neighbours = new IReadOnlyList<int>[TileCount];
            IsLand = new bool[TileCount];
            IsSea = new bool[TileCount];
            IsPassable = new bool[TileCount];
            SettlementWeight = new float[TileCount];
            for (var i = 0; i < TileCount; i++)
            {
                var tile = map.Get(i);
                Neighbours[i] = map.GetNeighbours(i);
                IsLand[i] = tile.IsLand;
                IsSea[i] = tile.IsSea;
                IsPassable[i] = tile.IsPassable;
                SettlementWeight[i] = tile.IsLand ? WeightOf(tile.Settlement) : 0f;
                TotalSettlementWeight += SettlementWeight[i];
                if (tile.IsLand) LandCount++;
            }

            var capitals = new List<AICapital>();
            foreach (var player in context.Players.Players)
                if (player.Data.Id != PlayerId && player.Data.IsAlive)
                    capitals.Add(new AICapital(player.Data.Id, player.Data.CapitalTileId));
            EnemyCapitals = capitals;

            _near = new bool[TileCount][];
            for (var i = 0; i < TileCount; i++) _near[i] = BuildNear(i);

            var builds = new List<AIBuildOption>();
            var recruits = new List<AIRecruitOption>();
            foreach (var tile in map.Tiles)
            {
                if (tile.OwnerId != PlayerId || !tile.HasSettlement) continue;
                foreach (var upgrade in context.Construction.GetCandidates(tile))
                {
                    var blocker = context.Construction.GetBuildBlocker(tile, upgrade, context.Player);
                    if (blocker != null && blocker != "reason.no_resources") continue;
                    var gain = upgrade.IsProducing ? context.Economy.CalculateUpgradeYield(tile, upgrade).Value : 0f;
                    builds.Add(new AIBuildOption(builds.Count, tile.Id, upgrade, gain));
                }
                foreach (var unit in context.Units.Units)
                {
                    var blocker = context.Construction.GetRecruitBlocker(tile, unit, context.Player);
                    if (blocker != null && blocker != "reason.no_resources") continue;
                    recruits.Add(new AIRecruitOption(tile.Id, unit, context.Construction.GetRecruitCost(tile, unit)));
                }
            }
            BuildOptions = builds;
            RecruitOptions = recruits;
        }

        public int PlayerId { get; }
        public int CapitalTile { get; }
        public int TileCount { get; }
        public int LandCount { get; }
        public float TotalSettlementWeight { get; }
        public IReadOnlyList<int>[] Neighbours { get; }
        public bool[] IsLand { get; }
        public bool[] IsSea { get; }
        public bool[] IsPassable { get; }
        public float[] SettlementWeight { get; }
        public IReadOnlyList<AICapital> EnemyCapitals { get; }
        public IReadOnlyList<AIBuildOption> BuildOptions { get; }
        public IReadOnlyList<AIRecruitOption> RecruitOptions { get; }

        /// <summary>Whether two tiles are at most <see cref="NearRadius"/> steps apart.</summary>
        public bool AreNear(int a, int b) => _near[a][b];

        private bool[] BuildNear(int origin)
        {
            var near = new bool[TileCount];
            near[origin] = true;
            foreach (var first in Neighbours[origin])
            {
                near[first] = true;
                if (NearRadius < 2) continue;
                foreach (var second in Neighbours[first]) near[second] = true;
            }
            return near;
        }

        private static float WeightOf(SettlementType settlement)
        {
            switch (settlement)
            {
                case SettlementType.Village: return 1f;
                case SettlementType.Castle: return 2f;
                case SettlementType.City: return 3f;
                default: return 0f;
            }
        }
    }

    public readonly struct AICapital
    {
        public AICapital(int owner, int tile)
        {
            Owner = owner;
            Tile = tile;
        }

        public int Owner { get; }
        public int Tile { get; }
    }

    public struct AIArmyState
    {
        public int ArmyId;
        public int Owner;
        public int Tile;
        public float Strength;
        public float FullStrength;
        public int Cohorts;
        public float Upkeep;
        public int ReinforceGold;
        public bool Alive;
        public bool Moved;
        /// <summary>Exists only in the plan (a cohort recruited into a new army): real commands cannot refer to it yet.</summary>
        public bool IsPlanned => ArmyId < 0;
    }

    /// <summary>
    /// A position of the search tree. Cheap to copy; battles are replaced by their expected outcome.
    /// </summary>
    public sealed class AISimulation
    {
        private const float DefenderAdvantage = 1.15f;

        private readonly AIWorld _world;
        private readonly int[] _owners;
        private AIArmyState[] _armies;
        // Rarely changed arrays are shared between copies until a building is planned.
        private bool[] _ports;
        private int[] _upgrades;
        private float[] _yields;
        private bool[] _builtOnTile;
        private int[] _goalDistance;

        public AISimulation(AIContext context, AIWorld world)
        {
            _world = world;
            var map = context.Map;
            _owners = new int[map.Count];
            _ports = new bool[map.Count];
            _upgrades = new int[map.Count];
            _yields = new float[map.Count];
            _builtOnTile = new bool[map.Count];
            for (var i = 0; i < map.Count; i++)
            {
                var tile = map.Get(i);
                _owners[i] = tile.OwnerId;
                _ports[i] = tile.HasPort;
                _upgrades[i] = tile.Upgrades.Count;
                _yields[i] = context.Economy.CalculateTileYield(tile).Value;
            }

            var all = context.Armies.All;
            _armies = new AIArmyState[all.Count];
            for (var i = 0; i < _armies.Length; i++)
            {
                var army = all[i];
                var state = new AIArmyState
                {
                    ArmyId = army.Id, Owner = army.OwnerId, Tile = army.TileId, Strength = army.Strength,
                    Cohorts = army.Cohorts.Count, Alive = true,
                    // Opponents get a fresh move on their own turn.
                    Moved = army.OwnerId == _world.PlayerId && army.HasMoved,
                    ReinforceGold = context.Construction.GetReinforceCost(army).Gold
                };
                foreach (var cohort in army.Cohorts)
                {
                    state.Upkeep += cohort.Type.Upkeep.Value;
                    state.FullStrength += cohort.Type.BaseStrength * (0.5f + 0.5f * cohort.Organization);
                }
                _armies[i] = state;
            }
            Resources = context.Player.Resources;
            _goalDistance = ComputeGoalDistances();
        }

        private AISimulation(AISimulation other)
        {
            _world = other._world;
            _owners = (int[])other._owners.Clone();
            _armies = (AIArmyState[])other._armies.Clone();
            _ports = other._ports;
            _upgrades = other._upgrades;
            _yields = other._yields;
            _builtOnTile = other._builtOnTile;
            _goalDistance = other._goalDistance;
            Resources = other.Resources;
        }

        public AIWorld World => _world;
        public ResourceBundle Resources { get; private set; }
        public int ArmyCount => _armies.Length;

        public AISimulation Clone() => new AISimulation(this);

        public int GetOwner(int tile) => _owners[tile];
        public float GetYield(int tile) => _yields[tile];
        public int GetUpgradeCount(int tile) => _upgrades[tile];
        public int GetGoalDistance(int tile) => _goalDistance[tile];
        public bool WasBuiltOn(int tile) => _builtOnTile[tile];
        public AIArmyState GetArmy(int index) => _armies[index];

        public bool OwnCapitalLost => _world.CapitalTile >= 0 && _owners[_world.CapitalTile] != _world.PlayerId;

        public bool AllEnemiesDefeated
        {
            get
            {
                foreach (var capital in _world.EnemyCapitals)
                    if (_owners[capital.Tile] == capital.Owner) return false;
                return _world.EnemyCapitals.Count > 0;
            }
        }

        public bool IsTerminal => OwnCapitalLost || AllEnemiesDefeated;

        public bool CanStep(int owner, int from, int to) =>
            MovementRules.IsStepAllowed(_world.IsPassable[to], _world.IsLand[from], _world.IsSea[to],
                _ports[from] && _owners[from] == owner);

        public bool HasHostileArmy(int tile, int owner)
        {
            foreach (var army in _armies)
                if (army.Alive && army.Tile == tile && army.Owner != owner) return true;
            return false;
        }

        public void MoveArmy(int index, int target)
        {
            var mover = _armies[index];
            mover.Moved = true;

            var enemyStrength = 0f;
            foreach (var other in _armies)
                if (other.Alive && other.Tile == target && other.Owner != mover.Owner) enemyStrength += other.Strength;

            if (enemyStrength <= 0f)
            {
                mover.Tile = target;
                _armies[index] = mover;
                Capture(target, mover.Owner);
                return;
            }

            var attack = mover.Strength * mover.Strength;
            var defence = enemyStrength * enemyStrength * DefenderAdvantage;
            if (attack > defence)
            {
                mover.Strength *= 1f - 0.6f * Mathf.Sqrt(defence / attack);
                mover.Tile = target;
                _armies[index] = mover;
                for (var i = 0; i < _armies.Length; i++)
                {
                    if (!_armies[i].Alive || _armies[i].Tile != target || _armies[i].Owner == mover.Owner) continue;
                    _armies[i].Alive = false;
                }
                Capture(target, mover.Owner);
                return;
            }

            var ratio = Mathf.Sqrt(attack / Mathf.Max(0.0001f, defence));
            mover.Strength *= 0.35f;
            _armies[index] = mover;
            for (var i = 0; i < _armies.Length; i++)
            {
                if (!_armies[i].Alive || _armies[i].Tile != target || _armies[i].Owner == mover.Owner) continue;
                _armies[i].Strength *= 1f - 0.6f * ratio;
            }
        }

        public void Build(AIBuildOption option)
        {
            Resources -= option.Upgrade.Cost;
            _upgrades = (int[])_upgrades.Clone();
            _upgrades[option.TileId]++;
            _builtOnTile = (bool[])_builtOnTile.Clone();
            _builtOnTile[option.TileId] = true;
            _yields = (float[])_yields.Clone();
            _yields[option.TileId] += option.IncomeGain;
            if (!option.Upgrade.IsPort) return;
            _ports = (bool[])_ports.Clone();
            _ports[option.TileId] = true;
            _goalDistance = ComputeGoalDistances();
        }

        public void Recruit(AIRecruitOption option)
        {
            Resources -= option.Cost;
            var strength = option.Unit.BaseStrength;
            var upkeep = option.Unit.Upkeep.Value;
            for (var i = 0; i < _armies.Length; i++)
            {
                var army = _armies[i];
                if (!army.Alive || army.Tile != option.TileId || army.Owner != _world.PlayerId) continue;
                if (army.Cohorts >= UnitsConfig.MaxCohortsPerArmy) continue;
                army.Strength += strength;
                army.FullStrength += strength;
                army.Cohorts++;
                army.Upkeep += upkeep;
                _armies[i] = army;
                return;
            }

            var count = _armies.Length;
            System.Array.Resize(ref _armies, count + 1);
            _armies[count] = new AIArmyState
            {
                ArmyId = -1, Owner = _world.PlayerId, Tile = option.TileId, Strength = strength, FullStrength = strength,
                Cohorts = 1, Upkeep = upkeep, Alive = true, Moved = true
            };
        }

        public void Reinforce(int index)
        {
            var army = _armies[index];
            Resources -= new ResourceBundle(army.ReinforceGold, 0, 0, 0);
            army.Strength = Mathf.Max(army.Strength, army.FullStrength);
            army.ReinforceGold = 0;
            _armies[index] = army;
        }

        public void Merge(int sourceIndex, int targetIndex)
        {
            var source = _armies[sourceIndex];
            var target = _armies[targetIndex];
            target.Strength += source.Strength;
            target.FullStrength += source.FullStrength;
            target.Cohorts += source.Cohorts;
            target.Upkeep += source.Upkeep;
            target.ReinforceGold += source.ReinforceGold;
            target.Moved |= source.Moved;
            source.Alive = false;
            _armies[sourceIndex] = source;
            _armies[targetIndex] = target;
        }

        private void Capture(int tile, int owner)
        {
            if (!_world.IsLand[tile]) return;
            var previous = _owners[tile];
            if (previous == owner) return;
            _owners[tile] = owner;
            if (previous != PlayerData.NeutralId && IsCapitalOf(previous, tile)) Eliminate(previous, owner);
        }

        private bool IsCapitalOf(int player, int tile)
        {
            if (player == _world.PlayerId) return tile == _world.CapitalTile;
            foreach (var capital in _world.EnemyCapitals)
                if (capital.Owner == player) return capital.Tile == tile;
            return false;
        }

        /// <summary>The same rule as in the conquest service: a conqueror inherits the land, the armies disband.</summary>
        private void Eliminate(int loser, int conqueror)
        {
            for (var i = 0; i < _owners.Length; i++)
                if (_owners[i] == loser) _owners[i] = conqueror;
            for (var i = 0; i < _armies.Length; i++)
                if (_armies[i].Owner == loser) _armies[i].Alive = false;
        }

        /// <summary>Multi-source BFS from every land tile worth conquering, following legal steps backwards.</summary>
        private int[] ComputeGoalDistances()
        {
            var distances = new int[_owners.Length];
            var queue = new Queue<int>();
            for (var i = 0; i < distances.Length; i++)
            {
                var isGoal = _world.IsLand[i] && _world.IsPassable[i] && _owners[i] != _world.PlayerId;
                distances[i] = isGoal ? 0 : -1;
                if (isGoal) queue.Enqueue(i);
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var previous in _world.Neighbours[current])
                {
                    if (distances[previous] >= 0 || !CanStep(_world.PlayerId, previous, current)) continue;
                    distances[previous] = distances[current] + 1;
                    queue.Enqueue(previous);
                }
            }
            return distances;
        }
    }
}
