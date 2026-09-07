using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChangJun.Economy
{
    /// <summary>
    /// 보유 자산 로컬 폴백 저장소. PlayerPrefs JSON — UGS 실패 시 사용.
    /// </summary>
    public sealed class LocalMoneyRankingStore
    {
        const string EntriesKey = "cupbap.money_ranking.v2";
        const string NameKey = "cupbap.player_name.v1";
        const string PlayerIdKey = "cupbap.player_id.v1";
        const int MaxEntries = 20;
        public const int DisplayRows = 10;

        static LocalMoneyRankingStore _instance;
        public static LocalMoneyRankingStore Instance => _instance ??= new LocalMoneyRankingStore();

        readonly List<MoneyRankingEntry> _entries = new();
        string _playerId;
        string _displayName;

        public string PlayerId => _playerId;
        public string DisplayName => _displayName;

        public event Action Changed;

        LocalMoneyRankingStore()
        {
            _playerId = PlayerPrefs.GetString(PlayerIdKey, string.Empty);
            if (string.IsNullOrEmpty(_playerId))
            {
                _playerId = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(PlayerIdKey, _playerId);
            }

            _displayName = PlayerPrefs.GetString(NameKey, "사장");
            if (string.IsNullOrWhiteSpace(_displayName))
                _displayName = "사장";

            Load();
        }

        public bool TrySetDisplayName(string name)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length < 1 || name.Length > 12) return false;
            _displayName = name;
            PlayerPrefs.SetString(NameKey, _displayName);
            PlayerPrefs.Save();

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].playerId != _playerId) continue;
                _entries[i] = new MoneyRankingEntry(_playerId, _displayName, _entries[i].money);
            }

            Persist();
            Changed?.Invoke();
            return true;
        }

        /// <summary>현재 보유 자산을 제출한다. 기존보다 낮으면 갱신하지 않는다.</summary>
        public void SubmitCurrentMoney(int money)
        {
            money = EconomyClamp.ClampMoney(money);
            int idx = FindSelfIndex();
            if (idx >= 0)
            {
                if (_entries[idx].money >= money)
                {
                    if (_entries[idx].displayName != _displayName)
                    {
                        _entries[idx] = new MoneyRankingEntry(_playerId, _displayName, _entries[idx].money);
                        Persist();
                        Changed?.Invoke();
                    }
                    return;
                }

                _entries[idx] = new MoneyRankingEntry(_playerId, _displayName, money);
            }
            else
            {
                _entries.Add(new MoneyRankingEntry(_playerId, _displayName, money));
            }

            SortAndTrim();
            Persist();
            Changed?.Invoke();
        }

        public IReadOnlyList<MoneyRankingEntry> GetTop(int count)
        {
            SortAndTrim();
            if (count >= _entries.Count) return _entries;
            return _entries.GetRange(0, Mathf.Min(count, _entries.Count));
        }

        public bool TryGetSelf(out MoneyRankingEntry entry, out int rank)
        {
            SortAndTrim();
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].playerId != _playerId) continue;
                entry = _entries[i];
                rank = i + 1;
                return true;
            }

            entry = default;
            rank = 0;
            return false;
        }

        int FindSelfIndex()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].playerId == _playerId) return i;
            }
            return -1;
        }

        void SortAndTrim()
        {
            _entries.Sort((a, b) => b.money.CompareTo(a.money));
            if (_entries.Count > MaxEntries)
                _entries.RemoveRange(MaxEntries, _entries.Count - MaxEntries);
        }

        void Load()
        {
            _entries.Clear();
            string json = PlayerPrefs.GetString(EntriesKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var wrap = JsonUtility.FromJson<EntryList>(json);
                if (wrap?.items == null) return;
                foreach (var e in wrap.items)
                {
                    if (string.IsNullOrEmpty(e.playerId)) continue;
                    _entries.Add(e);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Ranking] load failed: " + ex.Message);
            }
        }

        void Persist()
        {
            var wrap = new EntryList { items = _entries.ToArray() };
            PlayerPrefs.SetString(EntriesKey, JsonUtility.ToJson(wrap));
            PlayerPrefs.Save();
        }

        [Serializable]
        class EntryList
        {
            public MoneyRankingEntry[] items;
        }
    }
}
