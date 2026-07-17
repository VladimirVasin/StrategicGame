using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectUnknown.Strategy
{
    public sealed class StrategyStoryPointOfInterestCatalog
    {
        private readonly Dictionary<string, StrategyStoryPointOfInterestDefinition> byId;
        private readonly ReadOnlyCollection<StrategyStoryPointOfInterestDefinition> ordered;

        public StrategyStoryPointOfInterestCatalog(
            IEnumerable<StrategyStoryPointOfInterestDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            byId = new Dictionary<string, StrategyStoryPointOfInterestDefinition>(StringComparer.Ordinal);
            HashSet<int> sequenceOrders = new();
            List<StrategyStoryPointOfInterestDefinition> sorted = new();
            foreach (StrategyStoryPointOfInterestDefinition definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException("Story point definitions cannot contain null entries.", nameof(definitions));
                }

                if (!byId.TryAdd(definition.Id, definition))
                {
                    throw new ArgumentException($"The story point ID '{definition.Id}' occurs more than once.", nameof(definitions));
                }

                if (!sequenceOrders.Add(definition.SequenceOrder))
                {
                    throw new ArgumentException(
                        $"The story sequence order '{definition.SequenceOrder}' occurs more than once.",
                        nameof(definitions));
                }

                sorted.Add(definition);
            }

            sorted.Sort(CompareDefinitions);
            ordered = sorted.AsReadOnly();
        }

        public static StrategyStoryPointOfInterestCatalog Production { get; } =
            new(Array.Empty<StrategyStoryPointOfInterestDefinition>());

        public IReadOnlyList<StrategyStoryPointOfInterestDefinition> Definitions => ordered;
        public int Count => ordered.Count;

        public bool TryGet(string definitionId, out StrategyStoryPointOfInterestDefinition definition)
        {
            if (definitionId == null)
            {
                definition = null;
                return false;
            }

            return byId.TryGetValue(definitionId, out definition);
        }

        private static int CompareDefinitions(
            StrategyStoryPointOfInterestDefinition left,
            StrategyStoryPointOfInterestDefinition right)
        {
            int order = left.SequenceOrder.CompareTo(right.SequenceOrder);
            return order != 0 ? order : StringComparer.Ordinal.Compare(left.Id, right.Id);
        }
    }
}
