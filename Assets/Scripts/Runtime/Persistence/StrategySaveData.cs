using System;
using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    [Serializable]
    public sealed class StrategySaveData
    {
        public const int CurrentVersion = 13;

        public int version = CurrentVersion;
        public long savedUtcTicks;
        public int mapSeed;
        public int mapWidth;
        public int mapHeight;
        public float elapsedSeconds;
        public int weatherKind;
        public bool firstWinterFoodPrepared;
        public bool firstWinterFuelPrepared;
        public bool firstWinterPassed;
        public int firstNightFaunaStage = (int)StrategyFirstNightFaunaStage.Dormant;
        public StrategyFoundingStartSaveData foundingStart = new();
        public List<StrategyBuildingSaveData> buildings = new();
        public List<StrategyConstructionSiteSaveData> constructionSites = new();
        public List<StrategyResidentSaveData> residents = new();
        public List<StrategyLooseResourceSaveData> looseResources = new();
        public List<StrategyPointOfInterestSaveData> pointsOfInterest = new();
        public List<StrategyStoryPointOfInterestSaveData> storyPointsOfInterest = new();
        public int nextStoryPointOfInterestSequenceIndex;
        public List<StrategyCityItemSaveData> cityItems = new();
        public List<StrategyScoutLodgeSaveData> scoutLodges = new();
        public List<int> exploredCells = new();
        public List<int> trailCells = new();
    }

    [Serializable]
    public sealed class StrategyFoundingStartSaveData
    {
        public bool hasStarterCamp;
        public int starterCampX;
        public int starterCampY;
        public bool hasStarterCartOrigin;
        public int starterCartOriginX;
        public int starterCartOriginY;
        public int profileVersion;
        public string profileId = string.Empty;
        public List<StrategyFoundingAnswerSaveData> answers = new();
    }

    [Serializable]
    public sealed class StrategyFoundingAnswerSaveData
    {
        public string questionId = string.Empty;
        public string answerId = string.Empty;
    }

    [Serializable]
    public sealed class StrategyBuildingSaveData
    {
        public string stableId;
        public int tool;
        public int originX;
        public int originY;
        public int footprintX;
        public int footprintY;
        public int visualVariant;
        public int bridgeStartX;
        public int bridgeStartY;
        public int bridgeEndX;
        public int bridgeEndY;
        public List<StrategyCellSaveData> bridgeCells = new();
        public int[] resourceAmounts;
        public List<string> preparedDishIds = new();
        public List<int> preparedDishAmounts = new();
        public float leftoverRations;
    }

    [Serializable]
    public sealed class StrategyConstructionSiteSaveData
    {
        public int tool;
        public string title;
        public int originX;
        public int originY;
        public int footprintX;
        public int footprintY;
        public int visualVariant;
        public int costLogs;
        public int costStone;
        public int costPlanks;
        public int deliveredLogs;
        public int deliveredStone;
        public int deliveredPlanks;
        public float progress;
        public bool hasBridgeSpan;
        public int bridgeStartX;
        public int bridgeStartY;
        public int bridgeEndX;
        public int bridgeEndY;
        public List<StrategyCellSaveData> bridgeCells = new();
    }

    [Serializable]
    public sealed class StrategyResidentSaveData
    {
        public int residentId;
        public string homeStableId;
        public int gender;
        public int lifeStage;
        public int visualVariant;
        public string fullName;
        public string familyName;
        public float ageYears;
        public int fatherId;
        public int motherId;
        public List<int> childIds = new();
        public float worldX;
        public float worldY;
        public float nutritionDebt;
        public int daysHungry;
        public int lastNutritionDayIndex;
        public float coldExposure;
        public int lastColdResolutionDayIndex;
        public int combatHealth = 100;
        public int lastCombatRecoveryDayIndex = -1;
        public List<StrategyResidentItemSaveData> personalItems = new();
    }

    [Serializable]
    public sealed class StrategyResidentItemSaveData
    {
        public string itemId = string.Empty;
        public int quantity;
    }

    [Serializable]
    public sealed class StrategyLooseResourceSaveData
    {
        public bool constructionPile;
        public int originX;
        public int originY;
        public int resource;
        public int amount;
        public int logs;
        public int stone;
        public int planks;
        public bool preparedDishPile;
        public string preparedDishRecipeId = string.Empty;
        public int preparedDishAmount;
        public float preparedDishLeftoverRations;
    }

    [Serializable]
    public sealed class StrategyPointOfInterestSaveData
    {
        public string stableId = string.Empty;
        public int cellX;
        public int cellY;
        public int resourceKind;
        public bool hasMineralSite;
        public int mineralOriginX;
        public int mineralOriginY;
        public int remainingMineralAmount;
        public bool investigated;
    }

    [Serializable]
    public sealed class StrategyStoryPointOfInterestSaveData
    {
        public string stableId = string.Empty;
        public int cellX;
        public int cellY;
        public int state;
        public string definitionId = string.Empty;
        public int sequenceIndex = -1;
        public int committedResidentId;
    }

    [Serializable]
    public sealed class StrategyCityItemSaveData
    {
        public string itemId = string.Empty;
        public int quantity;
    }

    [Serializable]
    public sealed class StrategyScoutLodgeSaveData
    {
        public string lodgeStableId = string.Empty;
        public int residentId;
        public int expeditionState;
        public int plannedDays;
        public float startedElapsedSeconds;
        public float endsElapsedSeconds;
        public float remainingFieldRations;
        public float provisionRationCredit;
        public int lastProvisionedDayIndex = -1;
        public bool returnAfterStoryPoint;
    }

    [Serializable]
    public struct StrategyCellSaveData
    {
        public StrategyCellSaveData(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public int x;
        public int y;
    }
}
