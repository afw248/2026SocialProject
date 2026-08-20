using System;
using ChangJun.Data;
using ChangJun.Economy;
using ChangJun.Time;
using UnityEngine;

namespace ChangJun.Social
{
    /// <summary>
    /// 3일간 학교 급식 계약 — 성공 주문 N회 달성 시 일괄 보상.
    /// </summary>
    public sealed class SchoolLunchContractService : MonoBehaviour
    {
        public static SchoolLunchContractService Instance { get; private set; }

        [SerializeField] private int _targetSuccesses = 5;
        [SerializeField] private int _rewardAmount = 800;
        [SerializeField] private int _contractDays = 3;

        private bool _active;
        private int _daysLeft;
        private int _successes;

        public bool IsActive => _active;
        public int DaysLeft => _daysLeft;
        public int Successes => _successes;
        public int Target => _targetSuccesses;

        public event Action OnContractCompleted;
        public event Action OnContractStarted;

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

        public void TryStartContract(int dayIndex)
        {
            if (_active) return;
            if (dayIndex < 3 || dayIndex % 7 != 3) return;

            _active = true;
            _daysLeft = _contractDays;
            _successes = 0;
            OnContractStarted?.Invoke();
            Debug.Log("[SchoolLunch] 급식 계약 시작 — 3일간 성공 5회");
        }

        public void RecordSuccess()
        {
            if (!_active) return;
            _successes++;
            if (_successes >= _targetSuccesses)
                CompleteContract();
        }

        public void AdvanceDay()
        {
            if (!_active) return;
            _daysLeft--;
            if (_daysLeft <= 0 && _successes < _targetSuccesses)
            {
                _active = false;
                Debug.Log("[SchoolLunch] 급식 계약 실패");
            }
        }

        private void CompleteContract()
        {
            _active = false;
            MoneyManager.Instance?.AddMoney(_rewardAmount);
            DayLoopController.Instance?.Ledger.AddSubsidy(_rewardAmount, "학교 급식 계약 완료");
            StoreReputationService.Instance?.RecordOrder(true, false);
            OnContractCompleted?.Invoke();
            Debug.Log($"[SchoolLunch] 급식 계약 완료 +{_rewardAmount}원");
        }
    }
}
