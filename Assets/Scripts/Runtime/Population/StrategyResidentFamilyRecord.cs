using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    internal sealed class StrategyResidentFamilyRecord
    {
        private readonly List<int> childIds = new();

        public int ResidentId { get; private set; }
        public StrategyResidentGender Gender { get; private set; }
        public StrategyResidentLifeStage LifeStage { get; private set; }
        public int FatherId { get; private set; }
        public int MotherId { get; private set; }
        public int VisualVariant { get; private set; }
        public int DisplayAgeYears { get; private set; }
        public string FullName { get; private set; }
        public string FamilyName { get; private set; }
        public bool IsAlive { get; private set; }
        public IReadOnlyList<int> ChildIds => childIds;

        public void Configure(StrategyResidentAgent resident, bool isAlive)
        {
            if (resident == null)
            {
                return;
            }

            ResidentId = resident.ResidentId;
            Gender = resident.Gender;
            LifeStage = resident.LifeStage;
            FatherId = resident.FatherId;
            MotherId = resident.MotherId;
            VisualVariant = resident.VisualVariant;
            DisplayAgeYears = resident.DisplayAgeYears;
            FullName = resident.FullName;
            FamilyName = resident.FamilyName;
            IsAlive = isAlive;
            childIds.Clear();

            IReadOnlyList<int> residentChildren = resident.ChildIds;
            for (int i = 0; i < residentChildren.Count; i++)
            {
                int childId = residentChildren[i];
                if (childId > 0 && !childIds.Contains(childId))
                {
                    childIds.Add(childId);
                }
            }
        }
    }
}
