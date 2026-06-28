using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace ProjectUnknown.Strategy
{
    internal sealed partial class StrategyBuildMenuControllerDriver
    {
        private bool IsPointerOverBuildUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
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
                Object.Destroy(standalone);
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

        private static Vector2Int GetFootprint(StrategyBuildTool tool)
        {
            return tool switch
            {
                StrategyBuildTool.House => new Vector2Int(2, 2),
                StrategyBuildTool.LumberjackCamp => new Vector2Int(2, 2),
                StrategyBuildTool.StonecutterCamp => new Vector2Int(2, 2),
                StrategyBuildTool.Sawmill => new Vector2Int(3, 2),
                StrategyBuildTool.Mine => new Vector2Int(2, 2),
                StrategyBuildTool.CoalPit => new Vector2Int(2, 2),
                StrategyBuildTool.ClayPit => new Vector2Int(2, 2),
                StrategyBuildTool.Kiln => new Vector2Int(2, 2),
                StrategyBuildTool.Forge => new Vector2Int(2, 2),
                StrategyBuildTool.HunterCamp => new Vector2Int(2, 2),
                StrategyBuildTool.FisherHut => new Vector2Int(2, 2),
                StrategyBuildTool.ForagerCamp => new Vector2Int(2, 2),
                StrategyBuildTool.ChickenCoop => new Vector2Int(4, 4),
                StrategyBuildTool.TradingPost => new Vector2Int(3, 2),
                StrategyBuildTool.StorageYard => new Vector2Int(3, 2),
                StrategyBuildTool.Granary => new Vector2Int(3, 2),
                StrategyBuildTool.Bridge => Vector2Int.one,
                _ => Vector2Int.one
            };
        }
    }
}
