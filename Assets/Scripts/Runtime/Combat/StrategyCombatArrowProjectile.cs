using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyCombatArrowProjectile : MonoBehaviour
    {
        private const float Speed = 8.2f;

        private IStrategyCombatant target;
        private IStrategyCombatant owner;
        private SpriteRenderer spriteRenderer;
        private Vector3 startWorld;
        private float duration;
        private float elapsed;
        private int damage;

        public static void Launch(
            Vector3 fromWorld,
            IStrategyCombatant target,
            IStrategyCombatant owner,
            int damage)
        {
            if (!IsAvailable(target) || !IsAvailable(owner) || damage <= 0)
            {
                return;
            }

            GameObject arrowObject = new GameObject("Combat Arrow");
            StrategyCombatArrowProjectile projectile =
                arrowObject.AddComponent<StrategyCombatArrowProjectile>();
            projectile.Configure(fromWorld, target, owner, damage);
        }

        private void Configure(
            Vector3 fromWorld,
            IStrategyCombatant combatTarget,
            IStrategyCombatant projectileOwner,
            int amount)
        {
            target = combatTarget;
            owner = projectileOwner;
            damage = amount;
            startWorld = new Vector3(fromWorld.x, fromWorld.y, -0.11f);
            float distance = Vector2.Distance(startWorld, GetTargetWorld());
            duration = Mathf.Clamp(distance / Speed, 0.18f, 0.55f);

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = StrategyHuntingArrowProjectile.GetArrowSprite();
            spriteRenderer.color = Color.white;
            transform.position = startWorld;
            UpdateRotation(startWorld, GetTargetWorld());
            StrategyWorldSorting.Apply(spriteRenderer, transform.position, 4);
        }

        private void Update()
        {
            if (!IsAvailable(target) || !IsAvailable(owner))
            {
                Destroy(gameObject);
                return;
            }

            elapsed += Time.deltaTime;
            Vector3 targetWorld = GetTargetWorld();
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(startWorld, targetWorld, Smooth01(t));
            transform.position = new Vector3(transform.position.x, transform.position.y, -0.11f);
            UpdateRotation(transform.position, targetWorld);
            StrategyWorldSorting.Apply(spriteRenderer, transform.position, 4);
            if (t < 1f)
            {
                return;
            }

            StrategyCombatDamage hit = new StrategyCombatDamage(
                owner,
                owner.CombatFaction,
                damage,
                StrategyCombatDamageKind.Piercing,
                transform.position);
            target.ReceiveCombatDamage(hit);
            StrategyWorldEffectAnimator.Spawn(
                StrategyWorldEffectKind.Dust,
                transform.position,
                StrategyWorldSorting.ForPosition(transform.position, 5),
                Mathf.RoundToInt(Time.time * 23f),
                0.62f);
            Destroy(gameObject);
        }

        private Vector3 GetTargetWorld()
        {
            Vector3 world = target.CombatWorldPosition;
            world.y += 0.16f;
            world.z = -0.11f;
            return world;
        }

        private void UpdateRotation(Vector3 from, Vector3 to)
        {
            Vector2 direction = to - from;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private static bool IsAvailable(IStrategyCombatant combatant)
        {
            return combatant != null
                && (!(combatant is Object unityObject) || unityObject != null)
                && combatant.IsCombatAlive
                && combatant.CanBeCombatTargeted;
        }

        private static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
