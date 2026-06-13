using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public readonly struct StrategyResidentDeathSnapshot
    {
        public StrategyResidentDeathSnapshot(
            int residentId,
            string fullName,
            StrategyResidentGender gender,
            StrategyResidentLifeStage lifeStage,
            int visualVariant,
            int ageYears,
            int fatherId,
            int motherId,
            string familyName,
            Vector3 deathWorld,
            Vector2Int deathCell,
            Vector2Int homeOrigin,
            string finalProfession,
            string familyRole,
            int[] householdResidentIds,
            int[] childIds)
        {
            ResidentId = residentId;
            FullName = string.IsNullOrWhiteSpace(fullName) ? "Unknown Settler" : fullName;
            Gender = gender;
            LifeStage = lifeStage;
            VisualVariant = visualVariant;
            AgeYears = ageYears;
            FatherId = fatherId;
            MotherId = motherId;
            FamilyName = string.IsNullOrWhiteSpace(familyName) ? "Unknown" : familyName;
            DeathWorld = deathWorld;
            DeathCell = deathCell;
            HomeOrigin = homeOrigin;
            FinalProfession = string.IsNullOrWhiteSpace(finalProfession) ? "settler" : finalProfession;
            FamilyRole = string.IsNullOrWhiteSpace(familyRole) ? "settler" : familyRole;
            HouseholdResidentIds = householdResidentIds ?? System.Array.Empty<int>();
            ChildIds = childIds ?? System.Array.Empty<int>();
        }

        public int ResidentId { get; }
        public string FullName { get; }
        public StrategyResidentGender Gender { get; }
        public StrategyResidentLifeStage LifeStage { get; }
        public int VisualVariant { get; }
        public int AgeYears { get; }
        public int FatherId { get; }
        public int MotherId { get; }
        public string FamilyName { get; }
        public Vector3 DeathWorld { get; }
        public Vector2Int DeathCell { get; }
        public Vector2Int HomeOrigin { get; }
        public string FinalProfession { get; }
        public string FamilyRole { get; }
        public int[] HouseholdResidentIds { get; }
        public int[] ChildIds { get; }
    }
}
