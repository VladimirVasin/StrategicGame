using System.Collections;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyBootstrapRunner : MonoBehaviour
    {
        public void Run(IEnumerator routine)
        {
            if (routine == null)
            {
                Destroy(gameObject);
                return;
            }

            StartCoroutine(RunAndRelease(routine));
        }

        private IEnumerator RunAndRelease(IEnumerator routine)
        {
            yield return routine;
            Destroy(gameObject);
        }
    }
}
