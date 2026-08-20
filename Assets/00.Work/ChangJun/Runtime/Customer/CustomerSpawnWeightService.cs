using ChangJun.Data;
using ChangJun.News;
using ChangJun.Social;
using UnityEngine;

namespace ChangJun.Customer
{
    /// <summary>
    /// 뉴스·평판·보이콧·축제 이벤트를 반영한 스폰 가중치.
    /// </summary>
    public static class CustomerSpawnWeightService
    {
        public static float GetWeight(CraftCustomerSO customer)
        {
            if (customer == null) return 0f;

            float weight = 1f;
            var news = NewsManager.Instance?.TodayNews;
            if (news != null && customer.cultureGroup == news.cultureGroup)
            {
                weight *= news.spawnWeight;
                if (news.sentiment == NewsSentiment.Discrimination)
                    weight *= Mathf.Max(0.2f, 1f - news.boycottWeight);
            }

            if (NewsManager.Instance?.TodaySideStories != null)
            {
                foreach (var side in NewsManager.Instance.TodaySideStories)
                {
                    if (side != null && side.cultureGroup == customer.cultureGroup)
                        weight *= 1.05f;
                }
            }

            if (CulturalEventManager.Instance != null
                && CulturalEventManager.Instance.TodayEvent == ActiveCulturalEvent.CultureFestival
                && customer.cultureGroup == CulturalEventManager.Instance.FestivalCulture)
                weight *= CulturalEventManager.Instance.FestivalSpawnMultiplier;

            if (StoreReputationService.Instance != null)
                weight *= StoreReputationService.Instance.GetSpawnBoost();

            if (ShopUpgradeManager.Instance != null)
                weight *= ShopUpgradeManager.Instance.GetSpawnMultiplier(customer.cultureGroup);

            return Mathf.Max(0.05f, weight);
        }
    }
}
