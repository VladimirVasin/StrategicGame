using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectUnknown.Strategy
{
    public sealed class StrategyCityItemCatalog
    {
        private readonly Dictionary<string, StrategyCityItemDefinition> definitionsById;
        private readonly ReadOnlyCollection<StrategyCityItemDefinition> definitions;

        public StrategyCityItemCatalog(IEnumerable<StrategyCityItemDefinition> itemDefinitions)
        {
            if (itemDefinitions == null)
            {
                throw new ArgumentNullException(nameof(itemDefinitions));
            }

            definitionsById = new Dictionary<string, StrategyCityItemDefinition>(StringComparer.Ordinal);
            List<StrategyCityItemDefinition> orderedDefinitions = new();
            foreach (StrategyCityItemDefinition definition in itemDefinitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException("Catalog definitions cannot contain null entries.", nameof(itemDefinitions));
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

        public static StrategyCityItemCatalog Production { get; } =
            new(Array.Empty<StrategyCityItemDefinition>());

        public IReadOnlyList<StrategyCityItemDefinition> Definitions => definitions;
        public int Count => definitions.Count;

        public bool TryGet(string itemId, out StrategyCityItemDefinition definition)
        {
            if (itemId == null)
            {
                definition = null;
                return false;
            }

            return definitionsById.TryGetValue(itemId, out definition);
        }

        private static int CompareDefinitions(
            StrategyCityItemDefinition left,
            StrategyCityItemDefinition right)
        {
            int orderComparison = left.SortOrder.CompareTo(right.SortOrder);
            return orderComparison != 0
                ? orderComparison
                : StringComparer.Ordinal.Compare(left.Id, right.Id);
        }
    }
}
