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
                case StrategyProfessionType.Miner:
                    StrategyMine[] mines = FindSorted<StrategyMine>();
                    snapshot.Assigned = CountAssigned(mines, mine => mine.WorkerCount);
                    snapshot.Capacity = mines.Length * StrategyMine.MaxWorkers;
                    break;
                case StrategyProfessionType.CoalMiner:
                    StrategyCoalPit[] coalPits = FindSorted<StrategyCoalPit>();
                    snapshot.Assigned = CountAssigned(coalPits, pit => pit.WorkerCount);
                    snapshot.Capacity = coalPits.Length * StrategyCoalPit.MaxWorkers;
                    break;
                case StrategyProfessionType.ClayDigger:
                    StrategyClayPit[] clayPits = FindSorted<StrategyClayPit>();
                    snapshot.Assigned = CountAssigned(clayPits, pit => pit.WorkerCount);
                    snapshot.Capacity = clayPits.Length * StrategyClayPit.MaxWorkers;
                    break;
                case StrategyProfessionType.Sawyer:
                    StrategySawmill[] sawmills = FindSorted<StrategySawmill>();
                    snapshot.Assigned = CountAssigned(sawmills, sawmill => sawmill.WorkerCount);
                    snapshot.Capacity = sawmills.Length * StrategySawmill.MaxWorkers;
                    break;
                case StrategyProfessionType.Potter:
                    StrategyKiln[] kilns = FindSorted<StrategyKiln>();
                    snapshot.Assigned = CountAssigned(kilns, kiln => kiln.WorkerCount);
                    snapshot.Capacity = kilns.Length * StrategyKiln.MaxWorkers;
                    break;
                case StrategyProfessionType.Blacksmith:
                    StrategyForge[] forges = FindSorted<StrategyForge>();
                    snapshot.Assigned = CountAssigned(forges, forge => forge.WorkerCount);
                    snapshot.Capacity = forges.Length * StrategyForge.MaxWorkers;
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
                case StrategyProfessionType.Forager:
                    StrategyForagerCamp[] foragerCamps = FindSorted<StrategyForagerCamp>();
                    snapshot.Assigned = CountAssigned(foragerCamps, camp => camp.WorkerCount);
                    snapshot.Capacity = foragerCamps.Length * StrategyForagerCamp.MaxWorkers;
                    break;
                case StrategyProfessionType.StorageWorker:
                    snapshot.Assigned = population != null ? population.CountSettlementHaulers() : 0;
                    snapshot.Capacity = int.MaxValue;
                    snapshot.IsUnlimited = true;
                    break;
                case StrategyProfessionType.Builder:
                    snapshot.Assigned = population != null ? population.CountSettlementBuilders() : 0;
                    snapshot.Capacity = int.MaxValue;
                    snapshot.IsUnlimited = true;
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
                StrategyProfessionType.Miner => new ProfessionSnapshot(type, "Miners", "work underground for Iron", new Color(0.61f, 0.42f, 0.30f)),
                StrategyProfessionType.CoalMiner => new ProfessionSnapshot(type, "Coal Miners", "dig Coal inside pits", new Color(0.33f, 0.37f, 0.38f)),
                StrategyProfessionType.ClayDigger => new ProfessionSnapshot(type, "Clay Diggers", "dig wet Clay near water", new Color(0.66f, 0.40f, 0.27f)),
                StrategyProfessionType.Sawyer => new ProfessionSnapshot(type, "Sawyers", "saw Logs into Planks", new Color(0.63f, 0.43f, 0.25f)),
                StrategyProfessionType.Potter => new ProfessionSnapshot(type, "Potters", "fire Clay and Coal into Pottery", new Color(0.74f, 0.36f, 0.22f)),
                StrategyProfessionType.Blacksmith => new ProfessionSnapshot(type, "Blacksmiths", "forge Iron, Coal and Logs into Tools", new Color(0.72f, 0.31f, 0.20f)),
                StrategyProfessionType.Hunter => new ProfessionSnapshot(type, "Hunters", "hunt rabbits", new Color(0.56f, 0.43f, 0.26f)),
                StrategyProfessionType.Fisher => new ProfessionSnapshot(type, "Fishers", "catch fish near water", new Color(0.32f, 0.54f, 0.63f)),
                StrategyProfessionType.Forager => new ProfessionSnapshot(type, "Foragers", "gather wild food", new Color(0.41f, 0.55f, 0.30f)),
                StrategyProfessionType.StorageWorker => new ProfessionSnapshot(type, "Haulers", "haul all resources and food", new Color(0.58f, 0.49f, 0.37f)),
                StrategyProfessionType.Builder => new ProfessionSnapshot(type, "Builders", "build structures", new Color(0.75f, 0.55f, 0.27f)),
                _ => new ProfessionSnapshot(type, "Profession", string.Empty, Color.white)
            };
        }

        private void ChangeProfession(StrategyProfessionType type, bool assign)
        {
            bool success = assign
                ? TryAssign(type, out StrategyResidentAgent worker)
                : TryRemove(type, out worker);

            RegisterManualProfessionChange(type, assign, success);
            actionStatusText.text = GetActionMessage(type, assign, success, worker);
            StrategyHudSfxAudio.Play(success ? StrategyHudSfxKind.Step : StrategyHudSfxKind.Deny);
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
                case StrategyProfessionType.Miner:
                    foreach (StrategyMine mine in FindSorted<StrategyMine>())
                    {
                        if (mine != null && mine.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.CoalMiner:
                    foreach (StrategyCoalPit pit in FindSorted<StrategyCoalPit>())
                    {
                        if (pit != null && pit.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.ClayDigger:
                    foreach (StrategyClayPit pit in FindSorted<StrategyClayPit>())
                    {
                        if (pit != null && pit.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Sawyer:
                    foreach (StrategySawmill sawmill in FindSorted<StrategySawmill>())
                    {
                        if (sawmill != null && sawmill.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Potter:
                    foreach (StrategyKiln kiln in FindSorted<StrategyKiln>())
                    {
                        if (kiln != null && kiln.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Blacksmith:
                    foreach (StrategyForge forge in FindSorted<StrategyForge>())
                    {
                        if (forge != null && forge.TryAssignNextAvailableWorker(out worker))
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
                case StrategyProfessionType.Forager:
                    foreach (StrategyForagerCamp camp in FindSorted<StrategyForagerCamp>())
                    {
                        if (camp != null && camp.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.StorageWorker:
                    return population != null && population.TryAssignSettlementHauler(out worker);
                case StrategyProfessionType.Builder:
                    if (population != null && population.TryAssignSettlementBuilder(out worker))
                    {
                        population.TryDispatchSettlementBuildersToSite(null, false);
                        return true;
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
                case StrategyProfessionType.Miner:
                    StrategyMine[] mines = FindSorted<StrategyMine>();
                    for (int i = mines.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(mines[i], mines[i].WorkerCount, out worker, index => mines[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.CoalMiner:
                    StrategyCoalPit[] coalPits = FindSorted<StrategyCoalPit>();
                    for (int i = coalPits.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(coalPits[i], coalPits[i].WorkerCount, out worker, index => coalPits[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.ClayDigger:
                    StrategyClayPit[] clayPits = FindSorted<StrategyClayPit>();
                    for (int i = clayPits.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(clayPits[i], clayPits[i].WorkerCount, out worker, index => clayPits[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Sawyer:
                    StrategySawmill[] sawmills = FindSorted<StrategySawmill>();
                    for (int i = sawmills.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(sawmills[i], sawmills[i].WorkerCount, out worker, index => sawmills[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Potter:
                    StrategyKiln[] kilns = FindSorted<StrategyKiln>();
                    for (int i = kilns.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(kilns[i], kilns[i].WorkerCount, out worker, index => kilns[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Blacksmith:
                    StrategyForge[] forges = FindSorted<StrategyForge>();
                    for (int i = forges.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(forges[i], forges[i].WorkerCount, out worker, index => forges[i].UnassignWorkerAt(index)))
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
                case StrategyProfessionType.Forager:
                    StrategyForagerCamp[] foragerCamps = FindSorted<StrategyForagerCamp>();
                    for (int i = foragerCamps.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(foragerCamps[i], foragerCamps[i].WorkerCount, out worker, index => foragerCamps[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.StorageWorker:
                    return population != null && population.TryRemoveSettlementHauler(out worker);
                case StrategyProfessionType.Builder:
                    return population != null && population.TryRemoveSettlementBuilder(out worker);
                default:
                    return false;
            }
        }

    }
}
