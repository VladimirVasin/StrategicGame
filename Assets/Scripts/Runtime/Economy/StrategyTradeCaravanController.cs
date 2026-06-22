using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyTradeCaravanController : MonoBehaviour
    {
        private const float FirstArrivalDelay = 45f;
        private const float RepeatArrivalDelay = 300f;
        private const float FailedArrivalRetryDelay = 90f;
        private const float DwellDuration = 150f;
        private const int MaxEdgePathAttempts = 18;

        private readonly StrategyTradeOffer[] emptyOffers = new StrategyTradeOffer[0];
        private StrategyTradeOffer[] currentOffers;
        private CityMapController map;
        private StrategyTradeCaravanAgent activeAgent;
        private StrategyTradingPost activePost;
        private StrategyTradingPost cachedPost;
        private TradeState state;
        private float arrivalTimer;
        private float dwellTimer;
        private float postSearchCooldown;
        private string lastMessage = string.Empty;

        public static StrategyTradeCaravanController Active { get; private set; }
        public IReadOnlyList<StrategyTradeOffer> CurrentOffers => currentOffers ?? emptyOffers;

        public void Configure(CityMapController mapController)
        {
            Active = this;
            map = mapController;
            currentOffers = emptyOffers;
            state = TradeState.Waiting;
            arrivalTimer = FirstArrivalDelay;
            dwellTimer = 0f;
            postSearchCooldown = 0f;
            StrategyDebugLogger.Info("Trade", "CaravanControllerConfigured");
        }

        public bool IsTradingAt(StrategyTradingPost post)
        {
            return post != null && post == activePost && state == TradeState.Trading;
        }

        public string GetPostStatusText(StrategyTradingPost post)
        {
            int coins = StrategySettlementTreasury.Active != null ? StrategySettlementTreasury.Active.Coins : 0;
            if (post == null)
            {
                return "Trading post unavailable.";
            }

            if (state == TradeState.Arriving && post == activePost)
            {
                return "Caravan arriving"
                    + "\nETA: "
                    + Mathf.CeilToInt(activeAgent != null ? activeAgent.EstimatedRemainingSeconds : 0f)
                    + "s\nCoins: "
                    + coins;
            }

            if (state == TradeState.Trading && post == activePost)
            {
                string message = string.IsNullOrEmpty(lastMessage) ? "Trade goods while the caravan waits." : lastMessage;
                return "Caravan trading"
                    + "\nLeaves in: "
                    + Mathf.CeilToInt(dwellTimer)
                    + "s\nCoins: "
                    + coins
                    + "\n"
                    + message;
            }

            if (state == TradeState.Departing && post == activePost)
            {
                return "Caravan leaving"
                    + "\nNext visit after departure"
                    + "\nCoins: "
                    + coins;
            }

            return "Next caravan in "
                + Mathf.CeilToInt(arrivalTimer)
                + "s\nCoins: "
                + coins;
        }

        public bool TryExecuteOffer(
            StrategyTradingPost post,
            int index,
            out string result)
        {
            result = string.Empty;
            if (!IsTradingAt(post) || currentOffers == null || index < 0 || index >= currentOffers.Length)
            {
                result = "No active trade.";
                return false;
            }

            StrategyTradeOffer offer = currentOffers[index];
            bool success = StrategyTradeTransactionService.TryExecute(offer, post.FootprintBounds.center, out result);
            lastMessage = result;
            return success;
        }

        private void Update()
        {
            if (map == null)
            {
                return;
            }

            if (state == TradeState.Arriving)
            {
                UpdateArriving();
                return;
            }

            if (state == TradeState.Trading)
            {
                UpdateTrading();
                return;
            }

            if (state == TradeState.Departing)
            {
                UpdateDeparting();
                return;
            }

            UpdateWaiting();
        }

        private void UpdateWaiting()
        {
            if (!TryFindTradingPost(out StrategyTradingPost post))
            {
                arrivalTimer = FirstArrivalDelay;
                return;
            }

            arrivalTimer -= Time.deltaTime;
            if (arrivalTimer > 0f)
            {
                return;
            }

            TrySpawnCaravan(post);
        }

        private void UpdateArriving()
        {
            if (activeAgent == null || activePost == null)
            {
                ScheduleNext(FailedArrivalRetryDelay);
                return;
            }

            if (!activeAgent.HasArrived)
            {
                return;
            }

            state = TradeState.Trading;
            dwellTimer = DwellDuration;
            currentOffers = StrategyTradeOfferCatalog.CreateDefaultOffers();
            lastMessage = "Caravan offers are open.";
            StrategyDebugLogger.Info(
                "Trade",
                "CaravanArrived",
                StrategyDebugLogger.F("postOrigin", activePost.Origin),
                StrategyDebugLogger.F("offers", currentOffers.Length));
        }

        private void UpdateTrading()
        {
            dwellTimer -= Time.deltaTime;
            if (dwellTimer > 0f || activeAgent == null || activePost == null)
            {
                if (dwellTimer <= 0f && activeAgent == null)
                {
                    ScheduleNext(RepeatArrivalDelay);
                }

                return;
            }

            BeginDeparture();
        }

        private void UpdateDeparting()
        {
            if (activeAgent == null || !activeAgent.HasDeparted)
            {
                if (activeAgent == null)
                {
                    ScheduleNext(RepeatArrivalDelay);
                }

                return;
            }

            Destroy(activeAgent.gameObject);
            activeAgent = null;
            activePost = null;
            currentOffers = emptyOffers;
            ScheduleNext(RepeatArrivalDelay);
            StrategyDebugLogger.Info("Trade", "CaravanDeparted");
        }

        private void TrySpawnCaravan(StrategyTradingPost post)
        {
            if (post == null || !post.TryFindCaravanStopCell(out Vector2Int stopCell))
            {
                StrategyDebugLogger.Warn("Trade", "CaravanSpawnFailed", StrategyDebugLogger.F("reason", "no_post_stop_cell"));
                ScheduleNext(FailedArrivalRetryDelay);
                return;
            }

            if (!TryFindEdgePathTo(stopCell, out List<Vector3> worldPath))
            {
                StrategyDebugLogger.Warn(
                    "Trade",
                    "CaravanSpawnFailed",
                    StrategyDebugLogger.F("reason", "no_edge_path"),
                    StrategyDebugLogger.F("stopCell", stopCell));
                ScheduleNext(FailedArrivalRetryDelay);
                return;
            }

            GameObject caravanObject = new GameObject("Trade Caravan");
            caravanObject.transform.SetParent(transform, false);
            activeAgent = caravanObject.AddComponent<StrategyTradeCaravanAgent>();
            activeAgent.Configure(worldPath);
            activePost = post;
            state = TradeState.Arriving;
            lastMessage = string.Empty;
            StrategyDebugLogger.Info(
                "Trade",
                "CaravanSpawned",
                StrategyDebugLogger.F("postOrigin", post.Origin),
                StrategyDebugLogger.F("pathPoints", worldPath.Count));
        }

        private void BeginDeparture()
        {
            if (activeAgent == null)
            {
                ScheduleNext(RepeatArrivalDelay);
                return;
            }

            List<Vector3> exitPath = new();
            if (TryBuildExitPath(activeAgent.transform.position, out List<Vector3> path))
            {
                exitPath.AddRange(path);
            }
            else
            {
                exitPath.Add(activeAgent.transform.position);
                exitPath.Add(activeAgent.transform.position + new Vector3(6f, 0f, 0f));
            }

            activeAgent.BeginDeparture(exitPath);
            state = TradeState.Departing;
            currentOffers = emptyOffers;
            StrategyDebugLogger.Info("Trade", "CaravanLeaving");
        }

        private bool TryFindTradingPost(out StrategyTradingPost post)
        {
            post = null;
            if (cachedPost != null)
            {
                post = cachedPost;
                return true;
            }

            postSearchCooldown -= Time.deltaTime;
            if (postSearchCooldown > 0f)
            {
                return false;
            }

            postSearchCooldown = 2f;
            StrategyTradingPost[] posts = Object.FindObjectsByType<StrategyTradingPost>();
            for (int i = 0; i < posts.Length; i++)
            {
                if (posts[i] != null)
                {
                    post = posts[i];
                    cachedPost = post;
                    return true;
                }
            }

            return false;
        }

        private bool TryFindEdgePathTo(Vector2Int target, out List<Vector3> worldPath)
        {
            worldPath = null;
            List<Vector2Int> edges = CollectEdgeCandidates(target);
            int attempts = Mathf.Min(MaxEdgePathAttempts, edges.Count);
            for (int i = 0; i < attempts; i++)
            {
                if (TryBuildCellPath(edges[i], target, out List<Vector2Int> cellPath))
                {
                    worldPath = ToWorldPath(cellPath);
                    return true;
                }
            }

            return false;
        }

        private bool TryBuildExitPath(Vector3 startWorld, out List<Vector3> worldPath)
        {
            worldPath = null;
            if (!map.TryWorldToCell(startWorld, out Vector2Int start))
            {
                return false;
            }

            List<Vector2Int> edges = CollectEdgeCandidates(start);
            int attempts = Mathf.Min(MaxEdgePathAttempts, edges.Count);
            for (int i = 0; i < attempts; i++)
            {
                if (TryBuildCellPath(start, edges[i], out List<Vector2Int> cellPath))
                {
                    worldPath = ToWorldPath(cellPath);
                    return true;
                }
            }

            return false;
        }

        private void ScheduleNext(float delay)
        {
            state = TradeState.Waiting;
            arrivalTimer = Mathf.Max(10f, delay);
            dwellTimer = 0f;
            activePost = null;
            currentOffers = emptyOffers;
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        private enum TradeState
        {
            Waiting,
            Arriving,
            Trading,
            Departing
        }
    }
}
