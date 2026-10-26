using UnityEngine;

namespace LearnLanguages.Gameplay.Units
{
    /// <summary>
    /// A unit of soldiers inside an army.
    /// </summary>
    public sealed class Cohort
    {
        public Cohort(UnitTypeDefinition type, int maxSoldiers, int healthy, int wounded, float organization)
        {
            Type = type;
            MaxSoldiers = maxSoldiers;
            Healthy = Mathf.Clamp(healthy, 0, maxSoldiers);
            Wounded = Mathf.Clamp(wounded, 0, maxSoldiers - Healthy);
            Organization = Mathf.Clamp01(organization);
        }

        public UnitTypeDefinition Type { get; }
        public int MaxSoldiers { get; }
        public int Healthy { get; private set; }
        public int Wounded { get; private set; }
        public float Organization { get; private set; }

        public int Killed => MaxSoldiers - Healthy - Wounded;
        public bool IsDestroyed => Healthy <= 0 && Wounded <= 0;
        public bool IsCommander => Type.Class == UnitClass.Commander;
        public float StrengthRatio => MaxSoldiers > 0 ? (float)Healthy / MaxSoldiers : 0f;

        /// <summary>Approximate combat value used by heuristics.</summary>
        public float Strength => Type.BaseStrength * StrengthRatio * (0.5f + 0.5f * Organization);

        public void ApplyCasualties(int killed, int wounded)
        {
            killed = Mathf.Clamp(killed, 0, Healthy);
            Healthy -= killed;
            wounded = Mathf.Clamp(wounded, 0, Healthy);
            Healthy -= wounded;
            Wounded += wounded;
        }

        public void HealWounded(float share)
        {
            if (Wounded <= 0) return;
            var healed = Mathf.Max(1, Mathf.RoundToInt(Wounded * Mathf.Clamp01(share)));
            healed = Mathf.Min(healed, Wounded);
            Wounded -= healed;
            Healthy += healed;
        }

        public void Reinforce(int soldiers)
        {
            Healthy += Mathf.Clamp(soldiers, 0, Killed);
        }

        public void SetOrganization(float value) => Organization = Mathf.Clamp01(value);

        public void ChangeOrganization(float delta) => SetOrganization(Organization + delta);

        public void SetState(int healthy, int wounded, float organization)
        {
            Healthy = Mathf.Clamp(healthy, 0, MaxSoldiers);
            Wounded = Mathf.Clamp(wounded, 0, MaxSoldiers - Healthy);
            Organization = Mathf.Clamp01(organization);
        }
    }
}
