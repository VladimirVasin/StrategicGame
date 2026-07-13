using NUnit.Framework;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyVerificationTests
    {
        [Test] public void Calendar() => StrategyVerificationRunner.VerifyCalendar();
        [Test] public void ResourceStore() => StrategyVerificationRunner.VerifyResourceStore();
        [Test] public void ColdState() => StrategyVerificationRunner.VerifyColdState();
        [Test] public void SaveRoundTrip() => StrategyVerificationRunner.VerifySaveRoundTrip();
        [Test] public void RefugeeBalance() => StrategyVerificationRunner.VerifyRefugeeBalance();
        [Test] public void BuildScenes() => StrategyVerificationRunner.VerifyBuildScenes();
        [Test] public void NavigationPriorities() => StrategyVerificationRunner.VerifyNavigationPriorities();
        [Test] public void VisualCatalog() => StrategyVerificationRunner.VerifyVisualCatalog();
        [Test] public void TerrainPainterCharacterization() => StrategyVerificationRunner.VerifyTerrainPainterCharacterization();
        [Test] public void TerrainNoiseThroughput() => StrategyVerificationRunner.VerifyTerrainNoiseThroughput();
        [Test] public void ExplicitMapSeed() => StrategyVerificationRunner.VerifyExplicitMapSeed();
        [Test] public void AudioImportProfiles() => StrategyVerificationRunner.VerifyAudioImportProfiles();
        [Test] public void AudioArchitecture() => StrategyVerificationRunner.VerifyAudioArchitecture();
        [Test] public void SourceQuality() => StrategyVerificationRunner.VerifySourceQuality();
    }
}
