using System;

namespace LearnLanguages.Map.Data
{
    public enum ProvinceType
    {
        Land = 0,
        Sea = 1
    }

    public enum SettlementType
    {
        None = 0,
        Village = 1,
        Castle = 2,
        City = 3
    }

    [Flags]
    public enum SettlementMask
    {
        None = 0,
        Village = 1 << 0,
        Castle = 1 << 1,
        City = 1 << 2
    }

    public enum LandscapeType
    {
        Steppe = 0,
        Meadow = 1,
        Forest = 2,
        Hill = 3,
        Mountain = 4,
        Sea = 5
    }

    public enum ProductionType
    {
        None = 0,
        Wheat = 1,
        Vineyard = 2,
        Pasture = 3,
        Quarry = 4
    }

    public static class SettlementMaskExtensions
    {
        public static bool Allows(this SettlementMask mask, SettlementType settlement)
        {
            switch (settlement)
            {
                case SettlementType.Village: return (mask & SettlementMask.Village) != 0;
                case SettlementType.Castle: return (mask & SettlementMask.Castle) != 0;
                case SettlementType.City: return (mask & SettlementMask.City) != 0;
                default: return false;
            }
        }
    }
}
