using System;

namespace ProjectUnknown.Strategy
{
    public sealed class StrategyCombatHealth
    {
        private readonly int maximum;
        private int current;

        public StrategyCombatHealth(int maximum)
            : this(maximum, maximum)
        {
        }

        public StrategyCombatHealth(int maximum, int current)
        {
            if (maximum <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximum), "Maximum combat health must be positive.");
            }

            if (current < 0 || current > maximum)
            {
                throw new ArgumentOutOfRangeException(nameof(current), "Current combat health must be within the maximum.");
            }

            this.maximum = maximum;
            this.current = current;
        }

        public int Maximum => maximum;
        public int Current => current;
        public bool IsAlive => current > 0;

        public StrategyCombatDamageResult ApplyDamage(int amount)
        {
            if (amount <= 0 || !IsAlive)
            {
                return StrategyCombatDamageResult.Rejected(current, maximum);
            }

            int previous = current;
            current = Math.Max(0, current - amount);
            return new StrategyCombatDamageResult(
                true,
                previous,
                current,
                maximum,
                previous > 0 && current == 0);
        }

        public bool TryRestore(int restoredCurrent)
        {
            if (restoredCurrent < 0 || restoredCurrent > maximum)
            {
                return false;
            }

            current = restoredCurrent;
            return true;
        }
    }
}
