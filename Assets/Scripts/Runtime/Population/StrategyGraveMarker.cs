using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyGraveMarker : MonoBehaviour
    {
        private StrategyResidentDeathSnapshot snapshot;
        private SpriteRenderer spriteRenderer;
        private int epitaphVariant;

        public StrategyResidentDeathSnapshot Snapshot => snapshot;
        public string DeceasedName => DisplayName(snapshot.FullName);
        public string Epitaph => BuildEpitaph(snapshot, epitaphVariant);
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
            epitaphVariant = Mathf.Abs(graveIndex);
            EnsureClickCollider();
        }

        public string GetLifeText()
        {
            string ageText = AgeYears <= 0
                ? L("grave.age_unknown")
                : L("grave.age", AgeYears);
            return L(
                "grave.life_text",
                ageText,
                V(FinalProfession),
                V(FamilyRole));
        }

        public string GetMemoryText()
        {
            if (snapshot.LifeStage == StrategyResidentLifeStage.Child)
            {
                return L("grave.memory.child", DisplayFamilyName(snapshot.FamilyName));
            }

            if (snapshot.ChildIds != null && snapshot.ChildIds.Length > 0)
            {
                string parentTitle = V(
                    snapshot.Gender == StrategyResidentGender.Male ? "father" : "mother");
                return L("grave.memory.parent", parentTitle, snapshot.ChildIds.Length);
            }

            if (snapshot.HouseholdResidentIds != null && snapshot.HouseholdResidentIds.Length > 0)
            {
                return L("grave.memory.household", DisplayFamilyName(snapshot.FamilyName));
            }

            return L("grave.memory.settlement");
        }

        private static string BuildEpitaph(StrategyResidentDeathSnapshot deathSnapshot, int graveIndex)
        {
            bool wasChild = deathSnapshot.LifeStage == StrategyResidentLifeStage.Child;
            bool hadChildren = deathSnapshot.ChildIds != null && deathSnapshot.ChildIds.Length > 0;
            string name = DisplayName(deathSnapshot.FullName);
            string profession = deathSnapshot.FinalProfession;
            string familyName = DisplayFamilyName(deathSnapshot.FamilyName);

            if (wasChild)
            {
                int variant = Mathf.Abs(graveIndex) % 3;
                return L("grave.epitaph.child_" + variant, name, familyName);
            }

            if (hadChildren)
            {
                string parentTitle = V(
                    deathSnapshot.Gender == StrategyResidentGender.Male ? "father" : "mother");
                int variant = Mathf.Abs(graveIndex) % 3;
                return L(
                    "grave.epitaph.parent_" + variant,
                    name,
                    parentTitle,
                    V(profession));
            }

            if (profession == "householder")
            {
                int variant = Mathf.Abs(graveIndex) % 3;
                return L("grave.epitaph.householder_" + variant, name);
            }

            int generalVariant = Mathf.Abs(graveIndex) % 4;
            return L(
                "grave.epitaph.general_" + generalVariant,
                name,
                V(profession));
        }

        private static string L(string key, params object[] arguments) =>
            StrategySelectionLocalization.Text(key, arguments);

        private static string V(string value) =>
            StrategySelectionLocalization.Value(value);

        private static string DisplayName(string value) =>
            value == "Unknown Settler" ? L("grave.unknown_settler") : value;

        private static string DisplayFamilyName(string value) =>
            value == "Unknown" ? L("grave.unknown_family") : value;

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
