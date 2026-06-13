using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyGraveMarker : MonoBehaviour
    {
        private StrategyResidentDeathSnapshot snapshot;
        private SpriteRenderer spriteRenderer;
        private string epitaph;

        public StrategyResidentDeathSnapshot Snapshot => snapshot;
        public string DeceasedName => snapshot.FullName;
        public string Epitaph => epitaph;
        public string FinalProfession => snapshot.FinalProfession;
        public string FamilyRole => snapshot.FamilyRole;
        public int AgeYears => snapshot.AgeYears;
        public Sprite PreviewSprite => spriteRenderer != null ? spriteRenderer.sprite : null;
        public Bounds SelectionBounds => spriteRenderer != null
            ? spriteRenderer.bounds
            : new Bounds(transform.position, new Vector3(0.82f, 0.72f, 0f));

        public void Configure(StrategyResidentDeathSnapshot deathSnapshot, SpriteRenderer renderer, int graveIndex)
        {
            snapshot = deathSnapshot;
            spriteRenderer = renderer;
            epitaph = BuildEpitaph(deathSnapshot, graveIndex);
            EnsureClickCollider();
        }

        public string GetLifeText()
        {
            string ageText = AgeYears <= 0
                ? "Age unknown"
                : "Age " + AgeYears;
            return ageText
                + "\n"
                + "Known as " + FinalProfession
                + "\n"
                + "Family role: " + FamilyRole;
        }

        public string GetMemoryText()
        {
            if (snapshot.LifeStage == StrategyResidentLifeStage.Child)
            {
                return "A child of House " + snapshot.FamilyName + ".";
            }

            if (snapshot.ChildIds != null && snapshot.ChildIds.Length > 0)
            {
                string parentTitle = snapshot.Gender == StrategyResidentGender.Male ? "father" : "mother";
                return "Remembered as " + parentTitle + " to "
                    + snapshot.ChildIds.Length
                    + " child"
                    + (snapshot.ChildIds.Length == 1 ? "." : "ren.");
            }

            if (snapshot.HouseholdResidentIds != null && snapshot.HouseholdResidentIds.Length > 0)
            {
                return "Remembered by the household of House " + snapshot.FamilyName + ".";
            }

            return "Remembered by the settlement.";
        }

        private static string BuildEpitaph(StrategyResidentDeathSnapshot deathSnapshot, int graveIndex)
        {
            bool wasChild = deathSnapshot.LifeStage == StrategyResidentLifeStage.Child;
            bool hadChildren = deathSnapshot.ChildIds != null && deathSnapshot.ChildIds.Length > 0;
            string name = deathSnapshot.FullName;
            string profession = deathSnapshot.FinalProfession;
            string familyName = deathSnapshot.FamilyName;

            if (wasChild)
            {
                string[] childLines =
                {
                    "Here rests " + name + ", child of House " + familyName + ".",
                    name + " sleeps beneath this stone, held in the memory of kin.",
                    "Here rests young " + name + ", whose name remains with House " + familyName + "."
                };
                return childLines[Mathf.Abs(graveIndex) % childLines.Length];
            }

            if (hadChildren)
            {
                string parentTitle = deathSnapshot.Gender == StrategyResidentGender.Male ? "father" : "mother";
                string[] parentLines =
                {
                    "Here rests " + name + ", beloved " + parentTitle + " and " + profession + ".",
                    "In memory of " + name + ", " + parentTitle + ", " + profession + ", and keeper of the family name.",
                    name + " rests here, a " + parentTitle + " remembered by children and hearth."
                };
                return parentLines[Mathf.Abs(graveIndex) % parentLines.Length];
            }

            if (profession == "householder")
            {
                string[] householdLines =
                {
                    "Here rests " + name + ", keeper of hearth and home.",
                    "In memory of " + name + ", who tended the household with steady hands.",
                    name + " sleeps here, remembered at the hearth."
                };
                return householdLines[Mathf.Abs(graveIndex) % householdLines.Length];
            }

            string[] generalLines =
            {
                "Here rests " + name + ".",
                "In memory of " + name + ", " + profession + " of the settlement.",
                name + " sleeps beneath this stone.",
                "Here rests " + name + ", remembered by the settlement."
            };
            return generalLines[Mathf.Abs(graveIndex) % generalLines.Length];
        }

        private void EnsureClickCollider()
        {
            BoxCollider2D collider = GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider2D>();
            }

            collider.isTrigger = true;
            collider.offset = new Vector2(0f, 0.18f);
            collider.size = new Vector2(0.86f, 0.76f);
        }
    }
}
