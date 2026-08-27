using ChangJun.Social;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 헤더 왼쪽 — 켜 둔 인증을 칩으로 보여 준다.
    /// </summary>
    public sealed class UpgradeChipHud
    {
        private readonly RectTransform _row;

        public UpgradeChipHud(Transform header)
        {
            _row = UiFactory.CreatePanel(header, "UpgradeChips",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(88f, -16f), new Vector2(520f, 16f));
            var hlg = _row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            if (ShopUpgradeManager.Instance != null)
                ShopUpgradeManager.Instance.Changed += Refresh;
            Refresh();
        }

        public void Refresh()
        {
            foreach (Transform child in _row)
                Object.Destroy(child.gameObject);

            if (ShopUpgradeManager.Instance == null) return;

            foreach (var upgrade in ShopUpgradeManager.Instance.Catalog)
            {
                if (upgrade == null || !ShopUpgradeManager.Instance.IsEquipped(upgrade.upgradeType))
                    continue;

                var chip = new GameObject(upgrade.upgradeType.ToString(), typeof(RectTransform));
                chip.transform.SetParent(_row, false);
                var le = chip.AddComponent<LayoutElement>();
                le.preferredWidth = 118f;
                var img = chip.AddComponent<Image>();
                img.color = UiTheme.Success;
                var tmp = UiFactory.CreateText(chip.transform, "T", upgrade.displayName,
                    Vector2.zero, Vector2.one, new Vector2(4f, 0f), new Vector2(-4f, 0f),
                    TextAlignmentOptions.Center, 12, UiTheme.CardWhite);
                tmp.overflowMode = TextOverflowModes.Ellipsis;
            }
        }
    }
}
