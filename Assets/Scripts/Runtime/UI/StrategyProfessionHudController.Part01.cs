using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyProfessionHudController
    {

        private ProfessionSnapshot BuildSnapshot(StrategyProfessionType type, int freeWorkers)
        {
            ProfessionSnapshot snapshot = CreateBaseSnapshot(type);
            snapshot.FreeWorkers = freeWorkers;

            switch (type)
            {
                case StrategyProfessionType.Lumberjack:
                    StrategyLumberjackCamp[] lumberCamps = FindSorted<StrategyLumberjackCamp>();
                    snapshot.Assigned = CountAssigned(lumberCamps, camp => camp.WorkerCount);
                    snapshot.Capacity = lumberCamps.Length * StrategyLumberjackCamp.MaxWorkers;
                    break;
                case StrategyProfessionType.Stonecutter:
                    StrategyStonecutterCamp[] stoneCamps = FindSorted<StrategyStonecutterCamp>();
                    snapshot.Assigned = CountAssigned(stoneCamps, camp => camp.WorkerCount);
                    snapshot.Capacity = stoneCamps.Length * StrategyStonecutterCamp.MaxWorkers;
                    break;
                case StrategyProfessionType.Hunter:
                    StrategyHunterCamp[] hunterCamps = FindSorted<StrategyHunterCamp>();
                    snapshot.Assigned = CountAssigned(hunterCamps, camp => camp.WorkerCount);
                    snapshot.Capacity = hunterCamps.Length * StrategyHunterCamp.MaxWorkers;
                    break;
                case StrategyProfessionType.Fisher:
                    StrategyFisherHut[] fisherHuts = FindSorted<StrategyFisherHut>();
                    snapshot.Assigned = CountAssigned(fisherHuts, hut => hut.WorkerCount);
                    snapshot.Capacity = fisherHuts.Length * StrategyFisherHut.MaxWorkers;
                    break;
                case StrategyProfessionType.StorageWorker:
                    StrategyStorageYard[] storageYards = FindSorted<StrategyStorageYard>();
                    snapshot.Assigned = CountAssigned(storageYards, yard => yard.WorkerCount);
                    snapshot.Capacity = storageYards.Length > 0 ? int.MaxValue : 0;
                    snapshot.IsUnlimited = storageYards.Length > 0;
                    break;
                case StrategyProfessionType.Builder:
                    StrategyStorageYard[] builderYards = FindSorted<StrategyStorageYard>();
                    snapshot.Assigned = CountAssigned(builderYards, yard => yard.BuilderCount);
                    snapshot.Capacity = builderYards.Length > 0 ? int.MaxValue : 0;
                    snapshot.IsUnlimited = builderYards.Length > 0;
                    break;
                case StrategyProfessionType.GranaryWorker:
                    StrategyGranary[] granaries = FindSorted<StrategyGranary>();
                    snapshot.Assigned = CountAssigned(granaries, granary => granary.WorkerCount);
                    snapshot.Capacity = granaries.Length * StrategyGranary.MaxWorkers;
                    break;
            }

            return snapshot;
        }

        private ProfessionSnapshot CreateBaseSnapshot(StrategyProfessionType type)
        {
            return type switch
            {
                StrategyProfessionType.Lumberjack => new ProfessionSnapshot(type, "Lumberjacks", "chop trees and stockpile Logs", new Color(0.45f, 0.62f, 0.32f)),
                StrategyProfessionType.Stonecutter => new ProfessionSnapshot(type, "Stonecutters", "mine Stone with pickaxes", new Color(0.47f, 0.53f, 0.55f)),
                StrategyProfessionType.Hunter => new ProfessionSnapshot(type, "Hunters", "hunt rabbits", new Color(0.56f, 0.43f, 0.26f)),
                StrategyProfessionType.Fisher => new ProfessionSnapshot(type, "Fishers", "catch fish near water", new Color(0.32f, 0.54f, 0.63f)),
                StrategyProfessionType.StorageWorker => new ProfessionSnapshot(type, "Storekeepers", "haul Logs and Stone", new Color(0.58f, 0.49f, 0.37f)),
                StrategyProfessionType.Builder => new ProfessionSnapshot(type, "Builders", "build structures", new Color(0.75f, 0.55f, 0.27f)),
                StrategyProfessionType.GranaryWorker => new ProfessionSnapshot(type, "Granary Workers", "haul food to the granary", new Color(0.62f, 0.51f, 0.28f)),
                _ => new ProfessionSnapshot(type, "Profession", string.Empty, Color.white)
            };
        }

        private void ChangeProfession(StrategyProfessionType type, bool assign)
        {
            bool success = assign
                ? TryAssign(type, out StrategyResidentAgent worker)
                : TryRemove(type, out worker);

            actionStatusText.text = GetActionMessage(type, assign, success, worker);
            StrategyDebugLogger.Info(
                "ProfessionHud",
                "ProfessionChanged",
                StrategyDebugLogger.F("profession", type),
                StrategyDebugLogger.F("action", assign ? "assign" : "remove"),
                StrategyDebugLogger.F("success", success),
                StrategyDebugLogger.F("worker", worker != null ? worker.FullName : string.Empty));
            isDirty = true;
            RefreshUi();
        }

        private bool TryAssign(StrategyProfessionType type, out StrategyResidentAgent worker)
        {
            worker = null;
            switch (type)
            {
                case StrategyProfessionType.Lumberjack:
                    foreach (StrategyLumberjackCamp camp in FindSorted<StrategyLumberjackCamp>())
                    {
                        if (camp != null && camp.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Stonecutter:
                    foreach (StrategyStonecutterCamp camp in FindSorted<StrategyStonecutterCamp>())
                    {
                        if (camp != null && camp.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Hunter:
                    foreach (StrategyHunterCamp camp in FindSorted<StrategyHunterCamp>())
                    {
                        if (camp != null && camp.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Fisher:
                    foreach (StrategyFisherHut hut in FindSorted<StrategyFisherHut>())
                    {
                        if (hut != null && hut.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.StorageWorker:
                    foreach (StrategyStorageYard yard in FindSorted<StrategyStorageYard>())
                    {
                        if (yard != null && yard.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Builder:
                    foreach (StrategyStorageYard yard in FindSorted<StrategyStorageYard>())
                    {
                        if (yard != null && yard.TryAssignNextAvailableBuilder(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.GranaryWorker:
                    foreach (StrategyGranary granary in FindSorted<StrategyGranary>())
                    {
                        if (granary != null && granary.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                default:
                    return false;
            }
        }

        private bool TryRemove(StrategyProfessionType type, out StrategyResidentAgent worker)
        {
            worker = null;
            switch (type)
            {
                case StrategyProfessionType.Lumberjack:
                    StrategyLumberjackCamp[] lumberCamps = FindSorted<StrategyLumberjackCamp>();
                    for (int i = lumberCamps.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(lumberCamps[i], lumberCamps[i].WorkerCount, out worker, index => lumberCamps[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Stonecutter:
                    StrategyStonecutterCamp[] stoneCamps = FindSorted<StrategyStonecutterCamp>();
                    for (int i = stoneCamps.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(stoneCamps[i], stoneCamps[i].WorkerCount, out worker, index => stoneCamps[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Hunter:
                    StrategyHunterCamp[] hunterCamps = FindSorted<StrategyHunterCamp>();
                    for (int i = hunterCamps.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(hunterCamps[i], hunterCamps[i].WorkerCount, out worker, index => hunterCamps[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Fisher:
                    StrategyFisherHut[] fisherHuts = FindSorted<StrategyFisherHut>();
                    for (int i = fisherHuts.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(fisherHuts[i], fisherHuts[i].WorkerCount, out worker, index => fisherHuts[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.StorageWorker:
                    StrategyStorageYard[] storageYards = FindSorted<StrategyStorageYard>();
                    for (int i = storageYards.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(storageYards[i], storageYards[i].WorkerCount, out worker, index => storageYards[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Builder:
                    StrategyStorageYard[] builderYards = FindSorted<StrategyStorageYard>();
                    for (int i = builderYards.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveBuilder(builderYards[i], out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.GranaryWorker:
                    StrategyGranary[] granaries = FindSorted<StrategyGranary>();
                    for (int i = granaries.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(granaries[i], granaries[i].WorkerCount, out worker, index => granaries[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                default:
                    return false;
            }
        }

        private static bool TryRemoveWorker<T>(T site, int workerCount, out StrategyResidentAgent worker, Action<int> unassignAt)
            where T : Component
        {
            worker = null;
            if (site == null || workerCount <= 0)
            {
                return false;
            }

            int index = workerCount - 1;
            switch (site)
            {
                case StrategyLumberjackCamp camp:
                    camp.TryGetWorker(index, out worker);
                    break;
                case StrategyStonecutterCamp camp:
                    camp.TryGetWorker(index, out worker);
                    break;
                case StrategyHunterCamp camp:
                    camp.TryGetWorker(index, out worker);
                    break;
                case StrategyFisherHut hut:
                    hut.TryGetWorker(index, out worker);
                    break;
                case StrategyStorageYard yard:
                    yard.TryGetWorker(index, out worker);
                    break;
                case StrategyGranary granary:
                    granary.TryGetWorker(index, out worker);
                    break;
            }

            unassignAt(index);
            return true;
        }

        private static bool TryRemoveBuilder(StrategyStorageYard yard, out StrategyResidentAgent worker)
        {
            worker = null;
            if (yard == null || yard.BuilderCount <= 0)
            {
                return false;
            }

            int index = yard.BuilderCount - 1;
            yard.TryGetBuilder(index, out worker);
            yard.UnassignBuilderAt(index);
            return true;
        }

        private string GetActionMessage(StrategyProfessionType type, bool assign, bool success, StrategyResidentAgent worker)
        {
            string title = CreateBaseSnapshot(type).Title;
            if (!success)
            {
                return assign
                    ? title + ": no free residents or workplaces"
                    : title + ": nobody to remove";
            }

            return worker != null
                ? worker.FullName
                : assign
                    ? title + ": assigned"
                    : title + ": removed";
        }

        private int CountFreeWorkers()
        {
            if (population == null)
            {
                return 0;
            }

            int count = 0;
            foreach (StrategyResidentAgent resident in population.Residents)
            {
                if (resident != null
                    && resident.CanAcceptWorkAssignment
                    && !resident.HasWorkplace
                    && !resident.HasConstructionAssignment)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountAssigned<T>(T[] items, Func<T, int> getCount)
            where T : Component
        {
            int total = 0;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                {
                    total += getCount(items[i]);
                }
            }

            return total;
        }

        private static T[] FindSorted<T>()
            where T : Component
        {
            T[] items = UnityEngine.Object.FindObjectsByType<T>();
            Array.Sort(items, (left, right) => CompareOrigin(GetOrigin(left), GetOrigin(right)));
            return items;
        }

        private static int CompareOrigin(Vector2Int left, Vector2Int right)
        {
            int y = left.y.CompareTo(right.y);
            return y != 0 ? y : left.x.CompareTo(right.x);
        }

        private static Vector2Int GetOrigin(Component component)
        {
            return component switch
            {
                StrategyLumberjackCamp camp => camp.Origin,
                StrategyStonecutterCamp camp => camp.Origin,
                StrategyHunterCamp camp => camp.Origin,
                StrategyFisherHut hut => hut.Origin,
                StrategyStorageYard yard => yard.Origin,
                StrategyGranary granary => granary.Origin,
                _ => Vector2Int.zero
            };
        }

        private void UpdateAnimation(bool instant = false)
        {
            float target = isOpen ? 1f : 0f;
            panelT = instant
                ? target
                : Mathf.MoveTowards(panelT, target, Time.unscaledDeltaTime * AnimationSpeed);

            if (panelGroup == null || panelRoot == null)
            {
                return;
            }

            float smooth = Smooth01(panelT);
            panelGroup.alpha = smooth;
            panelGroup.interactable = isOpen;
            panelGroup.blocksRaycasts = isOpen;
            panelRoot.anchoredPosition = new Vector2(0f, -76f - (1f - smooth) * 18f);
            panelRoot.gameObject.SetActive(panelT > 0.001f || isOpen);
        }

        private void HandleManualScroll()
        {
            if (!isOpen
                || professionScroll == null
                || panelRoot == null
                || viewportRoot == null
                || contentRoot == null
                || Mouse.current == null)
            {
                return;
            }

            Vector2 pointer = Mouse.current.position.ReadValue();
            if (!RectTransformUtility.RectangleContainsScreenPoint(panelRoot, pointer))
            {
                return;
            }

            float wheel = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) <= 0.01f)
            {
                return;
            }

            float overflow = Mathf.Max(0f, contentRoot.rect.height - viewportRoot.rect.height);
            if (overflow <= 0.01f)
            {
                return;
            }

            float normalizedDelta = wheel * professionScroll.scrollSensitivity / overflow;
            professionScroll.verticalNormalizedPosition = Mathf.Clamp01(
                professionScroll.verticalNormalizedPosition + normalizedDelta);
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }

            StandaloneInputModule standalone = eventSystem.GetComponent<StandaloneInputModule>();
            if (standalone != null)
            {
                UnityEngine.Object.Destroy(standalone);
            }

            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            if (inputModule.actionsAsset == null)
            {
                inputModule.AssignDefaultActions();
            }
        }

        private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor anchor, Color color)
        {
            RectTransform rect = CreateUiObject(name, parent).GetComponent<RectTransform>();
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
