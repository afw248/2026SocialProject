using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ChangJun.Economy
{
    /// <summary>
    /// 보유 자산 랭킹 — UGS Leaderboards 우선, 실패 시 로컬 폴백.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class MoneyRankingService : MonoBehaviour
    {
        public const string DefaultLeaderboardId = "cupbap-money";
        public const int DisplayRows = LocalMoneyRankingStore.DisplayRows;

        public static MoneyRankingService Instance { get; private set; }

        [SerializeField] string _leaderboardId = DefaultLeaderboardId;

        UnityServicesSession _session;
        UgsMoneyLeaderboardClient _client;
        readonly List<MoneyRankingEntry> _onlineTop = new();
        MoneyRankingEntry _onlineSelf;
        int _onlineSelfRank;
        bool _hasOnlineSelf;
        bool _isOnline;
        string _statusText = "순위 준비 중…";
        string _lastError = string.Empty;
        CancellationTokenSource _opsCts;

        public event Action Changed;

        public bool IsOnline => _isOnline;
        public string StatusText => _statusText;
        public string DisplayName => LocalMoneyRankingStore.Instance.DisplayName;

        public string SelfPlayerId =>
            _isOnline && _session != null && _session.IsSignedIn
                ? _session.PlayerId
                : LocalMoneyRankingStore.Instance.PlayerId;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _session = GetComponent<UnityServicesSession>();
            if (_session == null)
                _session = gameObject.AddComponent<UnityServicesSession>();

            _client = new UgsMoneyLeaderboardClient(_leaderboardId);
        }

        void OnDestroy()
        {
            _opsCts?.Cancel();
            _opsCts?.Dispose();
            if (Instance == this) Instance = null;
        }

        public static MoneyRankingService EnsureCreated()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("MoneyRankingService");
            return go.AddComponent<MoneyRankingService>();
        }

        public async Task SubmitAndRefreshAsync(int money, CancellationToken token = default)
        {
            money = EconomyClamp.ClampMoney(money);
            LocalMoneyRankingStore.Instance.SubmitCurrentMoney(money);

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                token, destroyCancellationToken);
            var ct = linked.Token;

            try
            {
                SetStatus("UGS 연결 중…", online: false);
                await _session.EnsureSignedInAsync(ct);

                string name = LocalMoneyRankingStore.Instance.DisplayName;
                await _session.TryUpdatePlayerNameAsync(name, ct);
                await _client.SubmitAsync(money, name, ct);
                await PullOnlineAsync(ct);

                _isOnline = true;
                _lastError = string.Empty;
                SetStatus("온라인 · UGS 보유 자산 순위", online: true);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                _isOnline = false;
                _lastError = ShortError(e);
                SetStatus("오프라인 · 로컬 저장 · " + _lastError, online: false);
                Debug.LogWarning("[Ranking] UGS 실패, 로컬 폴백: " + e.Message);
            }

            Changed?.Invoke();
        }

        public async Task RefreshAsync(CancellationToken token = default)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                token, destroyCancellationToken);
            var ct = linked.Token;

            try
            {
                SetStatus("순위 불러오는 중…", _isOnline);
                await _session.EnsureSignedInAsync(ct);
                await PullOnlineAsync(ct);
                _isOnline = true;
                _lastError = string.Empty;
                SetStatus("온라인 · UGS 보유 자산 순위", online: true);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                _isOnline = false;
                _lastError = ShortError(e);
                SetStatus("오프라인 · 로컬 저장 · " + _lastError, online: false);
            }

            Changed?.Invoke();
        }

        public async Task SubmitQuietAsync(int money)
        {
            money = EconomyClamp.ClampMoney(money);
            LocalMoneyRankingStore.Instance.SubmitCurrentMoney(money);

            _opsCts?.Cancel();
            _opsCts?.Dispose();
            _opsCts = new CancellationTokenSource();
            var ct = _opsCts.Token;

            try
            {
                await _session.EnsureSignedInAsync(ct);
                await _client.SubmitAsync(money, LocalMoneyRankingStore.Instance.DisplayName, ct);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogWarning("[Ranking] 조용한 제출 실패: " + e.Message);
            }
        }

        public bool TrySetDisplayName(string name)
        {
            if (!LocalMoneyRankingStore.Instance.TrySetDisplayName(name))
                return false;

            if (_session != null && _session.IsSignedIn)
                _ = _session.TryUpdatePlayerNameAsync(LocalMoneyRankingStore.Instance.DisplayName);

            Changed?.Invoke();
            return true;
        }

        public IReadOnlyList<MoneyRankingEntry> GetTop(int count)
        {
            if (_isOnline)
            {
                if (count >= _onlineTop.Count) return _onlineTop;
                return _onlineTop.GetRange(0, Mathf.Min(count, _onlineTop.Count));
            }

            return LocalMoneyRankingStore.Instance.GetTop(count);
        }

        public bool TryGetSelf(out MoneyRankingEntry entry, out int rank)
        {
            if (_isOnline && _hasOnlineSelf)
            {
                entry = _onlineSelf;
                rank = _onlineSelfRank;
                return true;
            }

            return LocalMoneyRankingStore.Instance.TryGetSelf(out entry, out rank);
        }

        async Task PullOnlineAsync(CancellationToken token)
        {
            var top = await _client.GetTopAsync(DisplayRows, _session.PlayerId, token);
            _onlineTop.Clear();
            if (top != null)
                _onlineTop.AddRange(top);

            var self = await _client.GetSelfAsync(token);
            _hasOnlineSelf = self.ok;
            _onlineSelf = self.entry;
            _onlineSelfRank = self.rank;
        }

        void SetStatus(string text, bool online)
        {
            _statusText = text;
            _isOnline = online;
        }

        static string ShortError(Exception e)
        {
            string msg = e.Message ?? e.GetType().Name;
            if (msg.Length > 48) msg = msg.Substring(0, 45) + "…";
            return msg;
        }
    }
}
