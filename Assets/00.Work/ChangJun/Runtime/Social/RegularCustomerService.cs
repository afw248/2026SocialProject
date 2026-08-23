using System;
using System.Collections.Generic;
using ChangJun.Data;
using ChangJun.Judge;
using UnityEngine;

namespace ChangJun.Social
{
    /// <summary>
    /// 방문 횟수·호감도로 단골 관계를 쌓고, 임계치에서 짧은 서사를 연다.
    /// </summary>
    public sealed class RegularCustomerService : MonoBehaviour, ICustomerSpawnModifier
    {
        public static RegularCustomerService Instance { get; private set; }

        private readonly Dictionary<string, int> _visits = new();
        private readonly Dictionary<string, int> _affinity = new();
        private readonly HashSet<string> _playedBeats = new();

        public event Action<CraftCustomerSO, string> OnStoryTriggered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public int GetVisits(CraftCustomerSO customer) =>
            customer != null && _visits.TryGetValue(Key(customer), out var v) ? v : 0;

        public int GetAffinity(CraftCustomerSO customer) =>
            customer != null && _affinity.TryGetValue(Key(customer), out var v) ? v : 0;

        public void RecordVisit(CraftCustomerSO customer)
        {
            if (customer == null) return;
            string key = Key(customer);
            _visits[key] = GetVisits(customer) + 1;
        }

        public void RecordResult(CraftCustomerSO customer, CraftResult result)
        {
            if (customer == null) return;
            int delta = result switch
            {
                CraftResult.Success => 2,
                CraftResult.WrongOrder => -1,
                CraftResult.TabooViolation => -3,
                _ => 0,
            };
            if (delta == 0) return;
            string key = Key(customer);
            _affinity[key] = Mathf.Clamp(GetAffinity(customer) + delta, -20, 40);
        }

        public string PeekStoryLine(CraftCustomerSO customer)
        {
            if (customer == null) return null;
            int visits = GetVisits(customer);
            int affinity = GetAffinity(customer);

            if (customer.storyBeats != null)
            {
                RegularStoryBeat best = null;
                foreach (var beat in customer.storyBeats)
                {
                    if (beat == null || string.IsNullOrWhiteSpace(beat.line)) continue;
                    if (visits < beat.requiredVisits || affinity < beat.requiredAffinity) continue;
                    if (best == null || beat.requiredVisits >= best.requiredVisits)
                        best = beat;
                }

                if (best != null)
                {
                    string beatKey = Key(customer) + ":" + best.requiredVisits;
                    if (_playedBeats.Add(beatKey))
                        OnStoryTriggered?.Invoke(customer, best.line);
                    return best.line;
                }
            }

            return FallbackLine(customer, visits, affinity);
        }

        public float GetSpawnWeight(CraftCustomerSO customer)
        {
            if (customer == null) return 1f;
            int visits = GetVisits(customer);
            int affinity = GetAffinity(customer);
            if (visits <= 0 && affinity <= 0) return 1f;
            return 1f + visits * 0.08f + Mathf.Max(0, affinity) * 0.03f;
        }

        private static string FallbackLine(CraftCustomerSO customer, int visits, int affinity)
        {
            if (affinity < 0 && visits >= 2)
                return "지난번에 좀 실망해서… 오늘은 제대로 부탁해요.";
            if (visits < 2) return null;
            if (!string.IsNullOrWhiteSpace(customer.originNote) && visits >= 4)
                return customer.originNote;
            if (visits >= 2)
                return "어제 그 맛 생각나서 또 왔어요.";
            return null;
        }

        private static string Key(CraftCustomerSO customer) =>
            string.IsNullOrEmpty(customer.customerName) ? customer.name : customer.customerName;
    }
}
