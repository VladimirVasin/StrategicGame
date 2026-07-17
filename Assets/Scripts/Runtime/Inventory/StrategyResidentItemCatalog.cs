using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectUnknown.Strategy
{
    public sealed class StrategyResidentItemCatalog
    {
        private readonly Dictionary<string, StrategyResidentItemDefinition> definitionsById;
        private readonly ReadOnlyCollection<StrategyResidentItemDefinition> definitions;

        public StrategyResidentItemCatalog(IEnumerable<StrategyResidentItemDefinition> itemDefinitions)
        {
            if (itemDefinitions == null)
            {
                throw new ArgumentNullException(nameof(itemDefinitions));
            }

            definitionsById = new Dictionary<string, StrategyResidentItemDefinition>(StringComparer.Ordinal);
            List<StrategyResidentItemDefinition> orderedDefinitions = new();
            foreach (StrategyResidentItemDefinition definition in itemDefinitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException(
                        "Catalog definitions cannot contain null entries.",
                        nameof(itemDefinitions));
                }

                if (!definitionsById.TryAdd(definition.Id, definition))
                {
                    throw new ArgumentException(
                        $"The item ID '{definition.Id}' occurs more than once.",
                        nameof(itemDefinitions));
                }

                orderedDefinitions.Add(definition);
            }

            orderedDefinitions.Sort(CompareDefinitions);
            definitions = orderedDefinitions.AsReadOnly();
        }

        public static StrategyResidentItemCatalog Production { get; } =
            new(Array.Empty<StrategyResidentItemDefinition>());

        public IReadOnlyList<StrategyResidentItemDefinition> Definitions => definitions;
        public int Count => definitions.Count;

        public bool TryGet(string itemId, out StrategyResidentItemDefinition definition)
        {
            if (itemId == null)
            {
                definition = null;
                return false;
            }

            return definitionsById.TryGetValue(itemId, out definition);
        }

        private static int CompareDefinitions(
            StrategyResidentItemDefinition left,
            StrategyResidentItemDefinition right)
        {
            int orderComparison = left.SortOrder.CompareTo(right.SortOrder);
            return orderComparison != 0
                ? orderComparison
                : StringComparer.Ordinal.Compare(left.Id, right.Id);
        }
    }
}
