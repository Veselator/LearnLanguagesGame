using System.Collections.Generic;

namespace LearnLanguages.Gameplay.Units
{
    /// <summary>
    /// A detachment on the global map. Consists of up to <see cref="UnitsConfig.MaxCohortsPerArmy"/> cohorts.
    /// </summary>
    public sealed class Army
    {
        private readonly List<Cohort> _cohorts = new List<Cohort>();

        public Army(int id, int ownerId, int tileId)
        {
            Id = id;
            OwnerId = ownerId;
            TileId = tileId;
        }

        public int Id { get; }
        public int OwnerId { get; private set; }
        public int TileId { get; private set; }
        public bool HasMoved { get; private set; }
        public IReadOnlyList<Cohort> Cohorts => _cohorts;
        public bool IsFull => _cohorts.Count >= UnitsConfig.MaxCohortsPerArmy;
        public bool IsEmpty => _cohorts.Count == 0;

        public Cohort Commander
        {
            get
            {
                foreach (var cohort in _cohorts)
                    if (cohort.IsCommander) return cohort;
                return null;
            }
        }

        /// <summary>
        /// Morale of the army: organization of each cohort weighted by its strength relative to its maximum.
        /// </summary>
        public float Morale
        {
            get
            {
                if (_cohorts.Count == 0) return 0f;
                var commander = Commander;
                if (commander != null && commander.Healthy <= 0) return 0f;

                var sum = 0f;
                foreach (var cohort in _cohorts) sum += cohort.Organization * cohort.StrengthRatio;
                return sum / _cohorts.Count;
            }
        }

        public float Strength
        {
            get
            {
                var sum = 0f;
                foreach (var cohort in _cohorts) sum += cohort.Strength;
                return sum;
            }
        }

        public bool AddCohort(Cohort cohort)
        {
            if (IsFull) return false;
            _cohorts.Add(cohort);
            return true;
        }

        public void RemoveCohort(Cohort cohort) => _cohorts.Remove(cohort);

        public void RemoveDestroyedCohorts() => _cohorts.RemoveAll(c => c.IsDestroyed);

        public void SetTile(int tileId) => TileId = tileId;

        public void MarkMoved() => HasMoved = true;

        public void ResetMovement() => HasMoved = false;

        public void SetMovedState(bool moved) => HasMoved = moved;

        public void SetOwner(int ownerId) => OwnerId = ownerId;
    }
}
