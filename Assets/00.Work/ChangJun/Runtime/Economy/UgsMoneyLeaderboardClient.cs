using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using UnityEngine;

namespace ChangJun.Economy
{
    /// <summary>UGS Leaderboards — 보유 자산 단일 보드.</summary>
    public sealed class UgsMoneyLeaderboardClient
    {
        [Serializable]
        sealed class ScoreMeta
        {
            public string d;
            public string n;
        }

        readonly string _leaderboardId;

        public UgsMoneyLeaderboardClient(string leaderboardId)
        {
            _leaderboardId = string.IsNullOrWhiteSpace(leaderboardId) ? "cupbap-money" : leaderboardId;
        }

        public string LeaderboardId => _leaderboardId;

        public async Task SubmitAsync(int money, string displayName, CancellationToken token)
        {
            money = EconomyClamp.ClampMoney(money);
            await LeaderboardsService.Instance.AddPlayerScoreAsync(
                _leaderboardId,
                money,
                new AddPlayerScoreOptions
                {
                    Metadata = new ScoreMeta
                    {
                        d = $"{money:N0}원",
                        n = displayName ?? string.Empty
                    }
                });
            token.ThrowIfCancellationRequested();
        }

        public async Task<IReadOnlyList<MoneyRankingEntry>> GetTopAsync(
            int limit, string selfPlayerId, CancellationToken token)
        {
            var page = await LeaderboardsService.Instance.GetScoresAsync(
                _leaderboardId,
                new GetScoresOptions
                {
                    Offset = 0,
                    Limit = Mathf.Max(1, limit),
                    IncludeMetadata = true
                });
            token.ThrowIfCancellationRequested();

            var results = page?.Results;
            var rows = new List<MoneyRankingEntry>();
            if (results == null) return rows;

            foreach (var entry in results)
            {
                if (entry == null) continue;
                rows.Add(ToEntry(entry));
            }

            return rows;
        }

        public async Task<(bool ok, MoneyRankingEntry entry, int rank)> GetSelfAsync(CancellationToken token)
        {
            try
            {
                var entry = await LeaderboardsService.Instance.GetPlayerScoreAsync(
                    _leaderboardId,
                    new GetPlayerScoreOptions { IncludeMetadata = true });
                token.ThrowIfCancellationRequested();
                if (entry == null) return (false, default, 0);
                return (true, ToEntry(entry), entry.Rank + 1);
            }
            catch (LeaderboardsException e) when (
                e.Reason == LeaderboardsExceptionReason.EntryNotFound ||
                e.Reason == LeaderboardsExceptionReason.LeaderboardNotFound ||
                e.Reason == LeaderboardsExceptionReason.NotFound)
            {
                return (false, default, 0);
            }
        }

        static MoneyRankingEntry ToEntry(Unity.Services.Leaderboards.Models.LeaderboardEntry entry)
        {
            var meta = ReadMeta(entry.Metadata);
            string name = !string.IsNullOrEmpty(meta?.n)
                ? meta.n
                : (string.IsNullOrEmpty(entry.PlayerName) ? "사장" : entry.PlayerName);
            int money = EconomyClamp.ClampMoney((int)Math.Round(Math.Min(entry.Score, EconomyClamp.MaxMoney)));
            return new MoneyRankingEntry(entry.PlayerId ?? string.Empty, name, money);
        }

        static ScoreMeta ReadMeta(object metadata)
        {
            if (metadata is not string json || string.IsNullOrEmpty(json))
                return null;
            try { return JsonUtility.FromJson<ScoreMeta>(json); }
            catch { return null; }
        }
    }
}
