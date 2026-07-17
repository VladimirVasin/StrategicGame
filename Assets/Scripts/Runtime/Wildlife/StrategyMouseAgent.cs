using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyMouseAgent : MonoBehaviour
    {
        private StrategySettlementFaunaController owner;
        private CityMapController map;
        private SpriteRenderer renderer;
        private Vector2Int homeCell;
        private Vector2Int targetCell;
        private Object reservationOwner;
        private float decisionTimer;
        private float hiddenTimer;
        private bool hidden;

        public int FaunaId { get; private set; }
        public bool IsCaught { get; private set; }
        public Vector2Int CurrentCell => map != null && map.TryWorldToCell(transform.position, out Vector2Int cell) ? cell : homeCell;

        public void Configure(StrategySettlementFaunaController controller, CityMapController cityMap, int id, Vector2Int cell, SpriteRenderer spriteRenderer)
        {
            owner = controller; map = cityMap; FaunaId = id; homeCell = cell; targetCell = cell; renderer = spriteRenderer;
            transform.position = World(cell); transform.localScale = Vector3.one * StrategySettlementFaunaSpriteFactory.MouseWorldScale;
            decisionTimer = Random.Range(0.4f, 1.6f); hiddenTimer = Random.Range(4f, 10f);
            StrategyWorldSorting.Apply(renderer, transform.position, 0);
        }

        private void Update()
        {
            if (IsCaught || map == null || Time.timeScale <= 0f) return;
            hiddenTimer -= Time.deltaTime;
            if (hiddenTimer <= 0f)
            {
                hidden = !hidden;
                hiddenTimer = hidden ? Random.Range(1.2f, 3.8f) : Random.Range(5f, 12f);
                renderer.enabled = !hidden;
            }
            if (hidden) return;

            decisionTimer -= Time.deltaTime;
            if (decisionTimer <= 0f)
            {
                PickTarget();
                decisionTimer = Random.Range(0.8f, 2.4f);
            }
            Move(1.65f);
        }

        public bool IsReservedByOther(Object candidate) => reservationOwner != null && reservationOwner != candidate;
        public bool TryReserve(Object candidate)
        {
            if (candidate == null || IsCaught || reservationOwner != null && reservationOwner != candidate) return false;
            reservationOwner = candidate; hidden = false; renderer.enabled = true; return true;
        }

        public void Release(Object candidate) { if (reservationOwner == candidate) reservationOwner = null; }

        public void Catch(StrategyCatAgent cat)
        {
            if (IsCaught) return;
            IsCaught = true; owner?.NotifyMouseCaught(this, cat); Destroy(gameObject);
        }

        private void PickTarget()
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2Int candidate = homeCell + new Vector2Int(Random.Range(-5, 6), Random.Range(-5, 6));
                if (map.IsCellWalkable(candidate)) { targetCell = candidate; return; }
            }
            targetCell = homeCell;
        }

        private void Move(float speed)
        {
            Vector3 target = World(targetCell); Vector3 before = transform.position;
            transform.position = Vector3.MoveTowards(before, target, speed * Time.deltaTime);
            if (renderer != null && Mathf.Abs(transform.position.x - before.x) > 0.001f) renderer.flipX = transform.position.x < before.x;
            float bob = Mathf.Sin((Time.time + FaunaId) * 16f) * 0.025f;
            transform.localScale = new Vector3(
                StrategySettlementFaunaSpriteFactory.MouseWorldScale,
                StrategySettlementFaunaSpriteFactory.MouseWorldScale + bob,
                1f);
            StrategyWorldSorting.Apply(renderer, transform.position, 0);
        }

        private Vector3 World(Vector2Int cell)
        {
            Vector3 world = map.GetCellCenterWorld(cell.x, cell.y); world.z = -0.071f; return world;
        }
    }
}
