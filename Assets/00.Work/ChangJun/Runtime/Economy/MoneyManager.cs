using System;
using UnityEngine;

namespace ChangJun.Economy
{
    /// <summary>
    /// 자산(돈) 싱글톤 매니저.
    /// 돈이 변경될 때 OnMoneyChanged 이벤트로 단방향 통지한다 — 역참조 없음 (이벤트 기반 DIP).
    /// API 는 참고 레포 MoneyManager 와 동일하게 유지해 팀 간 통합을 쉽게 한다.
    /// </summary>
    public sealed class MoneyManager : MonoBehaviour
    {
        public static MoneyManager Instance { get; private set; }

        [SerializeField] private int _startMoney = 3000;

        private int _money;

        /// <summary>현재 자산 (읽기 전용)</summary>
        public int Money => _money;

        /// <summary>돈 변경 시 (현재값) 발행</summary>
        public event Action<int> OnMoneyChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _money = _startMoney;
        }

        /// <summary>돈을 더한다 (수입)</summary>
        public void AddMoney(int amount)
        {
            _money += amount;
            OnMoneyChanged?.Invoke(_money);
        }

        /// <summary>돈을 뺀다 (지출·환불)</summary>
        public void SpendMoney(int amount)
        {
            _money -= amount;
            OnMoneyChanged?.Invoke(_money);
        }

        /// <summary>돈을 직접 설정한다</summary>
        public void SetMoney(int value)
        {
            _money = value;
            OnMoneyChanged?.Invoke(_money);
        }
    }
}
