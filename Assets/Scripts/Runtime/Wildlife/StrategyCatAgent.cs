using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyCatAgent : MonoBehaviour
    {
        private const float CatVisualScale = 0.70f;
        private StrategySettlementFaunaController owner;
        private CityMapController map;
        private SpriteRenderer renderer;
        private StrategyMouseAgent prey;
        private Vector2Int homeCell;
        private Vector2Int targetCell;
        private float decisionTimer;
        private float restTimer;
        private float frameTimer;
        private int appliedFrame = -1;
        private StrategyCatSpritePose appliedPose;

        public int FaunaId { get; private set; }
        public StrategyCatCoat Coat { get; private set; }
        public StrategyCatTemperament Temperament { get; private set; }
        public Vector2Int CurrentCell => map != null && map.TryWorldToCell(transform.position, out Vector2Int cell) ? cell : homeCell;

        public void Configure(StrategySettlementFaunaController controller, CityMapController cityMap, int id, Vector2Int cell,
            StrategyCatCoat coat, StrategyCatTemperament temperament, SpriteRenderer spriteRenderer)
        {
            owner = controller; map = cityMap; FaunaId = id; Coat = coat; Temperament = temperament; renderer = spriteRenderer;
            homeCell = cell; targetCell = cell; transform.position = World(cell);
            transform.localScale = Vector3.one * CatVisualScale;
            decisionTimer = Random.Range(0.5f, 2f); restTimer = Random.Range(2f, 6f);
            StrategyWorldSorting.Apply(renderer, transform.position, 1);
        }

        private void Update()
        {
            if (map == null || Time.timeScale <= 0f) return;
            if (prey != null && !prey.IsCaught)
            {
                ChaseMouse(); return;
            }
            prey = null;
            decisionTimer -= Time.deltaTime;
            restTimer -= Time.deltaTime;
            if (decisionTimer <= 0f)
            {
                prey = owner != null ? owner.FindMouseForCat(this, Temperament == StrategyCatTemperament.Hunter ? 12f : 8f) : null;
                if (prey == null) PickPatrolTarget();
                decisionTimer = Random.Range(1.5f, 4f);
            }
            if (restTimer > 0f) AnimateIdle(); else MoveTo(World(targetCell), 1.05f, StrategyCatSpritePose.Walk, 7.5f);
            if (restTimer < -Random.Range(2f, 5f)) restTimer = Random.Range(1f, 4f);
        }

        private void ChaseMouse()
        {
            Vector3 target = prey.transform.position;
            if ((target - transform.position).sqrMagnitude <= 0.16f)
            {
                prey.Catch(this); prey = null; restTimer = Random.Range(2f, 5f); return;
            }
            MoveTo(target, 2.15f, StrategyCatSpritePose.Stalk, 11.5f);
        }

        private void PickPatrolTarget()
        {
            for (int i = 0; i < 12; i++)
            {
                Vector2Int candidate = homeCell + new Vector2Int(Random.Range(-8, 9), Random.Range(-8, 9));
                if (map.IsCellWalkable(candidate)) { targetCell = candidate; return; }
            }
        }

        private void MoveTo(Vector3 target, float speed, StrategyCatSpritePose pose, float frameRate)
        {
            Vector3 before = transform.position;
            transform.position = Vector3.MoveTowards(before, target, speed * Time.deltaTime);
            if (renderer != null && Mathf.Abs(transform.position.x - before.x) > 0.001f) renderer.flipX = transform.position.x < before.x;
            float bob = Mathf.Abs(Mathf.Sin((Time.time + FaunaId) * 8f)) * 0.035f;
            transform.localScale = new Vector3(CatVisualScale, CatVisualScale + bob, 1f);
            ApplyAnimatedSprite(pose, frameRate);
            StrategyWorldSorting.Apply(renderer, transform.position, 1);
        }

        private void AnimateIdle()
        {
            float breathe = Mathf.Sin((Time.time + FaunaId) * 2.8f) * 0.018f;
            transform.localScale = new Vector3(CatVisualScale - breathe * 0.4f, CatVisualScale + breathe, 1f);
            ApplyAnimatedSprite(restTimer > 2.4f ? StrategyCatSpritePose.Rest : StrategyCatSpritePose.Idle, 3.5f);
        }

        private void ApplyAnimatedSprite(StrategyCatSpritePose pose, float frameRate)
        {
            frameTimer += Time.deltaTime * frameRate;
            int frame = Mathf.FloorToInt(frameTimer) % StrategySettlementFaunaSpriteFactory.CatFrameCount;
            if (pose == appliedPose && frame == appliedFrame) return;
            appliedPose = pose; appliedFrame = frame;
            renderer.sprite = StrategySettlementFaunaSpriteFactory.GetCatSprite(Coat, pose, frame);
        }

        private Vector3 World(Vector2Int cell)
        {
            Vector3 world = map.GetCellCenterWorld(cell.x, cell.y); world.z = -0.073f; return world;
        }

        private void OnDestroy() { if (prey != null) prey.Release(this); }
    }
}
