namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPopulationController
    {
        private static void UnassignFromClayPit(StrategyResidentAgent resident)
        {
            StrategyClayPit pit = resident.ClayPitWorkplace;
            if (pit == null)
            {
                return;
            }

            for (int i = pit.Workers.Count - 1; i >= 0; i--)
            {
                if (pit.Workers[i] == resident)
                {
                    pit.UnassignWorkerAt(i);
                    return;
                }
            }

            resident.ClearClayPitWorkplace(pit);
        }

        private static void UnassignFromKiln(StrategyResidentAgent resident)
        {
            StrategyKiln kiln = resident.KilnWorkplace;
            if (kiln == null)
            {
                return;
            }

            for (int i = kiln.Workers.Count - 1; i >= 0; i--)
            {
                if (kiln.Workers[i] == resident)
                {
                    kiln.UnassignWorkerAt(i);
                    return;
                }
            }

            resident.ClearKilnWorkplace(kiln);
        }

        private static void UnassignFromForge(StrategyResidentAgent resident)
        {
            StrategyForge forge = resident.ForgeWorkplace;
            if (forge == null)
            {
                return;
            }

            for (int i = forge.Workers.Count - 1; i >= 0; i--)
            {
                if (forge.Workers[i] == resident)
                {
                    forge.UnassignWorkerAt(i);
                    return;
                }
            }

            resident.ClearForgeWorkplace(forge);
        }

        private static void UnassignFromForagerCamp(StrategyResidentAgent resident)
        {
            StrategyForagerCamp camp = resident.ForagerWorkplace;
            if (camp == null)
            {
                return;
            }

            for (int i = camp.Workers.Count - 1; i >= 0; i--)
            {
                if (camp.Workers[i] == resident)
                {
                    camp.UnassignWorkerAt(i);
                    return;
                }
            }

            resident.ClearForagerWorkplace(camp);
        }
    }
}
