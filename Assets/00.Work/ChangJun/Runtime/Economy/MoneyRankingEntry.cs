using System;

namespace ChangJun.Economy
{
    /// <summary>보유 자산 랭킹 한 줄.</summary>
    [Serializable]
    public struct MoneyRankingEntry
    {
        public string playerId;
        public string displayName;
        public int money;

        public MoneyRankingEntry(string playerId, string displayName, int money)
        {
            this.playerId = playerId ?? string.Empty;
            this.displayName = string.IsNullOrWhiteSpace(displayName) ? "사장" : displayName.Trim();
            this.money = money;
        }
    }
}
