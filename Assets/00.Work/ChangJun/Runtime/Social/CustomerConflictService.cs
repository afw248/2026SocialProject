using System.Collections.Generic;
using ChangJun.Data;
using UnityEngine;

namespace ChangJun.Social
{
    /// <summary>
    /// 대기줄에서 손님-손님 편견 발언. 개입/방관이 상생 지수와 이후 스폰에 남는다.
    /// </summary>
    public sealed class CustomerConflictService : MonoBehaviour, ICustomerSpawnModifier
    {
        public static CustomerConflictService Instance { get; private set; }

        private readonly List<ConflictEventSO> _pool = new();
        private CultureGroup _penalizedCulture = CultureGroup.None;
        private int _penaltyDays;
        private CultureGroup _boostedCulture = CultureGroup.None;
        private int _boostDays;
        private bool _firedToday;
        private bool _ignoredYesterday;
        private bool _intervenedYesterday;

        public bool IgnoredYesterday => _ignoredYesterday;
        public bool IntervenedYesterday => _intervenedYesterday;

        public event System.Action<ConflictEventSO, CraftCustomerSO, CraftCustomerSO> OnConflictStarted;
        public event System.Action<ConflictEventSO, bool> OnConflictResolved;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _pool.AddRange(Resources.LoadAll<ConflictEventSO>("Craft/Conflicts"));
            if (_pool.Count == 0)
                SeedFallbackPool();
        }

        public void BeginDay()
        {
            _firedToday = false;
            if (_penaltyDays > 0) _penaltyDays--;
            if (_penaltyDays <= 0) _penalizedCulture = CultureGroup.None;
            if (_boostDays > 0) _boostDays--;
            if (_boostDays <= 0) _boostedCulture = CultureGroup.None;
            _ignoredYesterday = false;
            _intervenedYesterday = false;
        }

        public bool TryStart(CraftCustomerSO previous, CraftCustomerSO incoming, out ConflictEventSO evt)
        {
            evt = null;
            if (_firedToday || previous == null || incoming == null) return false;
            if (previous.cultureGroup == incoming.cultureGroup) return false;
            if (Random.value > 0.28f) return false;

            evt = FindMatch(previous.cultureGroup, incoming.cultureGroup);
            if (evt == null) return false;

            _firedToday = true;
            OnConflictStarted?.Invoke(evt, previous, incoming);
            return true;
        }

        public void Resolve(ConflictEventSO evt, bool intervene)
        {
            if (evt == null) return;

            if (intervene)
            {
                _intervenedYesterday = true;
                StoreReputationService.Instance?.ApplyDirectDelta(0.04f);
                UnderstandingManagerSafeAdd(evt.targetCulture, 4);
                UnderstandingManagerSafeAdd(evt.speakerCulture, 2);
                _boostedCulture = evt.targetCulture;
                _boostDays = 2;
            }
            else
            {
                _ignoredYesterday = true;
                StoreReputationService.Instance?.ApplyDirectDelta(-0.06f);
                _penalizedCulture = evt.targetCulture;
                _penaltyDays = 2;
            }

            OnConflictResolved?.Invoke(evt, intervene);
        }

        public float GetSpawnWeight(CraftCustomerSO customer)
        {
            if (customer == null) return 1f;
            if (customer.cultureGroup == _penalizedCulture && _penaltyDays > 0)
                return 0.65f;
            if (customer.cultureGroup == _boostedCulture && _boostDays > 0)
                return 1.25f;
            return 1f;
        }

        private ConflictEventSO FindMatch(CultureGroup a, CultureGroup b)
        {
            foreach (var e in _pool)
            {
                if (e == null) continue;
                if ((e.speakerCulture == a && e.targetCulture == b)
                    || (e.speakerCulture == b && e.targetCulture == a))
                    return e;
            }
            return null;
        }

        private static void UnderstandingManagerSafeAdd(CultureGroup culture, int delta)
        {
            Progression.UnderstandingManager.Instance?.ApplyExternalDelta(culture, delta);
        }

        private void SeedFallbackPool()
        {
            _pool.Add(Make("queue_halal", CultureGroup.Korean, CultureGroup.Muslim,
                "줄에서 한 손님이 다른 손님을 보며 말합니다.",
                "저 사람들 음식은 냄새도 이상하고, 따로 해야 하는 거 아니에요?",
                "가게에서 그런 말은 안 됩니다. 같이 먹는 자리예요.",
                "모른 척하고 주문을 받는다.",
                "개입: 타문화 손님이 안심하고, 이해도가 조금 오릅니다.",
                "방관: 편견이 가게 분위기가 됩니다. 해당 문화권 손님이 줄어듭니다."));
            _pool.Add(Make("queue_sea", CultureGroup.Korean, CultureGroup.SEAsian,
                "대기줄에서 볼멘소리가 들립니다.",
                "이주 노동자들 때문에 줄이 길어진다니까요.",
                "손님은 모두 같은 손님입니다. 줄을 함께 기다려 주세요.",
                "못 들은 척한다.",
                "개입: 동남아 손님의 신뢰가 회복됩니다.",
                "방관: 동남아 손님 방문이 며칠 줄어듭니다."));
            _pool.Add(Make("queue_vegan", CultureGroup.Korean, CultureGroup.Vegan,
                "한 손님이 채식 주문 카드를 보고 코웃음 칩니다.",
                "고기도 안 먹고 유난이네. 밥이 음식이면 됐지.",
                "식단은 신념입니다. 여기서는 존중하고 주문받습니다.",
                "웃고 넘어간다.",
                "개입: 채식 손님이 이 가게를 안전한 곳으로 기억합니다.",
                "방관: 채식 손님 발길이 뜸해집니다."));
        }

        private static ConflictEventSO Make(string id, CultureGroup speaker, CultureGroup target,
            string prompt, string line, string intervene, string ignore, string interveneNote, string ignoreNote)
        {
            var so = ScriptableObject.CreateInstance<ConflictEventSO>();
            so.eventId = id;
            so.speakerCulture = speaker;
            so.targetCulture = target;
            so.prompt = prompt;
            so.prejudiceLine = line;
            so.interveneLabel = "개입한다";
            so.ignoreLabel = "모른 척한다";
            so.interveneNote = interveneNote;
            so.ignoreNote = ignoreNote;
            so.name = id;
            return so;
        }
    }
}
