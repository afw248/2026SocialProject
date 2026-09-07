using System;
using System.Threading;
using ChangJun.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>보유 자산 랭킹 전체 화면 (UGS + 로컬 폴백).</summary>
    public sealed class RankingOverlay
    {
        const int RowCount = MoneyRankingService.DisplayRows;

        private readonly GameObject _root;
        private readonly UiTheme.HeaderMeta _headerMeta;
        private readonly TextMeshProUGUI _statusText;
        private readonly TMP_InputField _nameField;
        private readonly TextMeshProUGUI _hintText;
        private readonly RectTransform _listContent;
        private readonly TextMeshProUGUI _selfRank;
        private readonly TextMeshProUGUI _selfName;
        private readonly TextMeshProUGUI _selfScore;
        private readonly Image _selfBg;
        private readonly GameObject _emptyHint;

        private readonly TextMeshProUGUI[] _ranks = new TextMeshProUGUI[RowCount];
        private readonly TextMeshProUGUI[] _names = new TextMeshProUGUI[RowCount];
        private readonly TextMeshProUGUI[] _scores = new TextMeshProUGUI[RowCount];
        private readonly Image[] _rowBgs = new Image[RowCount];
        private readonly GameObject[] _rowRoots = new GameObject[RowCount];

        private CancellationTokenSource _showCts;
        private int _showVersion;

        public event Action OnBack;

        public RankingOverlay()
        {
            _root = UiFactory.CreateOverlayRoot("RankingOverlay", 60);
            _root.SetActive(false);

            var bg = UiFactory.CreateStretchChild(_root.transform, "Bg");
            bg.gameObject.AddComponent<Image>().color = UiTheme.Background;

            var header = UiTheme.CreateHeaderBar(_root.transform, "순위", 72f, 78f);
            UiTheme.CreateBackButton(header, () => OnBack?.Invoke());
            _headerMeta = UiTheme.CreateHeaderMeta(header);

            var body = UiTheme.CreateScreenBody(_root.transform, 72f, 24f);

            var title = UiFactory.CreateText(body, "Title", "보유 자산 순위",
                new Vector2(0.02f, 0.93f), new Vector2(0.98f, 1f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 26, UiTheme.TextDark);
            title.fontStyle = FontStyles.Bold;

            _statusText = UiFactory.CreateText(body, "Status",
                "순위 준비 중…",
                new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.93f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 16, UiTheme.TextMuted);

            var nameRow = UiTheme.CreateBorderedPanel(body, "NameRow",
                new Vector2(0.02f, 0.78f), new Vector2(0.98f, 0.87f),
                Vector2.zero, Vector2.zero, UiTheme.CardWhite, 3f);
            _nameField = CreateNameField(nameRow,
                new Vector2(0.02f, 0.15f), new Vector2(0.72f, 0.85f));
            UiTheme.CreateFlatButton(
                UiFactory.CreatePanel(nameRow, "Apply",
                    new Vector2(0.75f, 0.15f), new Vector2(0.98f, 0.85f), Vector2.zero, Vector2.zero),
                "변경", UiTheme.Accent, ApplyName, 20);

            _hintText = UiFactory.CreateText(body, "Hint", "",
                new Vector2(0.02f, 0.73f), new Vector2(0.98f, 0.78f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 14, UiTheme.TextMuted);

            var listBox = UiTheme.CreateBorderedPanel(body, "List",
                new Vector2(0.02f, 0.16f), new Vector2(0.98f, 0.72f),
                Vector2.zero, Vector2.zero, UiTheme.TanRow, 3f);

            var scrollRt = UiFactory.CreatePanel(listBox, "Scroll",
                Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            UiFactory.ConfigureScroll(scroll);

            var viewport = UiFactory.CreateStretchChild(scrollRt, "Viewport");
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);

            _listContent = UiFactory.CreateStretchChild(viewport, "Content");
            _listContent.pivot = new Vector2(0.5f, 1f);
            _listContent.anchorMin = new Vector2(0, 1);
            _listContent.anchorMax = new Vector2(1, 1);

            var vlg = _listContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            _listContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = _listContent;

            for (int i = 0; i < RowCount; i++)
            {
                CreateRow(_listContent, i, out _rowRoots[i], out _rowBgs[i],
                    out _ranks[i], out _names[i], out _scores[i]);
                _rowRoots[i].SetActive(false);
            }

            _emptyHint = new GameObject("Empty", typeof(RectTransform));
            _emptyHint.transform.SetParent(_listContent, false);
            _emptyHint.AddComponent<LayoutElement>().preferredHeight = 80;
            var emptyTmp = _emptyHint.AddComponent<TextMeshProUGUI>();
            emptyTmp.text = "아직 기록이 없습니다. 영업하며 자산을 모으면 여기에 표시됩니다.";
            emptyTmp.fontSize = 18;
            emptyTmp.color = UiTheme.TextFaint;
            emptyTmp.alignment = TextAlignmentOptions.Center;
            KoreanUiFont.Apply(emptyTmp);

            var selfWrap = UiTheme.CreateBorderedPanel(body, "Self",
                new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.14f),
                Vector2.zero, Vector2.zero, new Color32(0xFF, 0xE8, 0xA8, 0xFF), 3f);
            CreateInlineRow(selfWrap, true, out _selfBg, out _selfRank, out _selfName, out _selfScore);
            UiFactory.CreateText(selfWrap, "SelfLabel", "나",
                new Vector2(0.02f, 0.55f), new Vector2(0.12f, 0.95f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, 14, UiTheme.TextMuted);
        }

        public async void Show()
        {
            MoneyRankingService.EnsureCreated();
            var service = MoneyRankingService.Instance;

            _showCts?.Cancel();
            _showCts?.Dispose();
            _showCts = new CancellationTokenSource();
            var token = _showCts.Token;
            int version = ++_showVersion;

            if (_nameField != null)
                _nameField.SetTextWithoutNotify(service.DisplayName);
            _hintText.text = "";
            _statusText.text = "순위 불러오는 중…";

            UiTheme.RefreshHeaderMeta(_headerMeta);
            RefreshList();
            _root.SetActive(true);

            int money = MoneyManager.Instance != null ? MoneyManager.Instance.Money : 0;
            try
            {
                await service.SubmitAndRefreshAsync(money, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Ranking] 패널 갱신 실패: " + e.Message);
            }

            if (version != _showVersion || !_root.activeSelf) return;
            _statusText.text = service.StatusText;
            RefreshList();
        }

        public void Hide()
        {
            _showCts?.Cancel();
            _root.SetActive(false);
        }

        private async void ApplyName()
        {
            var service = MoneyRankingService.EnsureCreated();
            string next = _nameField != null ? _nameField.text : service.DisplayName;
            if (!service.TrySetDisplayName(next))
            {
                _hintText.text = "이름은 1~12자로 입력하세요.";
                _hintText.color = UiTheme.Danger;
                return;
            }

            _hintText.text = "이름이 저장되었습니다.";
            _hintText.color = UiTheme.Success;

            int money = MoneyManager.Instance != null ? MoneyManager.Instance.Money : 0;
            try
            {
                await service.SubmitAndRefreshAsync(money, _showCts?.Token ?? default);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogWarning("[Ranking] 이름 반영 제출 실패: " + e.Message);
            }

            if (_root.activeSelf)
            {
                _statusText.text = service.StatusText;
                RefreshList();
            }
        }

        private void RefreshList()
        {
            var service = MoneyRankingService.EnsureCreated();
            var top = service.GetTop(RowCount);
            string selfId = service.SelfPlayerId;
            bool any = top.Count > 0;
            _emptyHint.SetActive(!any);

            for (int i = 0; i < RowCount; i++)
            {
                if (i < top.Count)
                {
                    bool isSelf = top[i].playerId == selfId;
                    _rowRoots[i].SetActive(true);
                    _ranks[i].text = (i + 1).ToString();
                    _names[i].text = top[i].displayName;
                    _scores[i].text = $"{top[i].money:N0}원";
                    _rowBgs[i].color = isSelf ? new Color32(0xFF, 0xE8, 0xA8, 0xFF) : UiTheme.CardWhite;
                }
                else
                {
                    _rowRoots[i].SetActive(false);
                }
            }

            if (service.TryGetSelf(out var self, out int rank))
            {
                _selfRank.text = rank.ToString();
                _selfName.text = self.displayName;
                _selfScore.text = $"{self.money:N0}원";
            }
            else
            {
                _selfRank.text = "-";
                _selfName.text = service.DisplayName;
                int money = MoneyManager.Instance != null ? MoneyManager.Instance.Money : 0;
                _selfScore.text = $"{money:N0}원";
            }
        }

        private static void CreateRow(Transform parent, int index, out GameObject root, out Image bg,
            out TextMeshProUGUI rank, out TextMeshProUGUI name, out TextMeshProUGUI score)
        {
            root = new GameObject($"Row_{index}", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            root.AddComponent<LayoutElement>().preferredHeight = 64;
            CreateInlineRow(root.transform, false, out bg, out rank, out name, out score);
        }

        private static void CreateInlineRow(Transform parent, bool isSelf, out Image bg,
            out TextMeshProUGUI rank, out TextMeshProUGUI name, out TextMeshProUGUI score)
        {
            bg = parent.GetComponent<Image>();
            if (bg == null)
                bg = parent.gameObject.AddComponent<Image>();
            bg.color = isSelf ? new Color32(0xFF, 0xE8, 0xA8, 0xFF) : UiTheme.CardWhite;

            rank = UiFactory.CreateText(parent, "Rank", "-",
                new Vector2(0f, 0f), new Vector2(0.12f, 1f), new Vector2(12f, 0f), Vector2.zero,
                TextAlignmentOptions.Center, 24, UiTheme.TextDark);
            rank.fontStyle = FontStyles.Bold;

            name = UiFactory.CreateText(parent, "Name", "-",
                new Vector2(0.14f, 0f), new Vector2(0.58f, 1f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft, 22, UiTheme.TextDark);

            score = UiFactory.CreateText(parent, "Score", "-",
                new Vector2(0.58f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-16f, 0f),
                TextAlignmentOptions.MidlineRight, 22, UiTheme.TextDark);
        }

        private static TMP_InputField CreateNameField(Transform parent, Vector2 min, Vector2 max)
        {
            var fieldRt = UiFactory.CreatePanel(parent, "NameField", min, max, Vector2.zero, Vector2.zero);
            fieldRt.gameObject.AddComponent<Image>().color = UiTheme.TanRow;

            var textArea = new GameObject("Text Area", typeof(RectTransform));
            textArea.transform.SetParent(fieldRt, false);
            UiFactory.Stretch(textArea.GetComponent<RectTransform>());
            var areaRt = textArea.GetComponent<RectTransform>();
            areaRt.offsetMin = new Vector2(14f, 6f);
            areaRt.offsetMax = new Vector2(-14f, -6f);

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(textArea.transform, false);
            UiFactory.Stretch(textGo.GetComponent<RectTransform>());
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.fontSize = 20;
            text.color = UiTheme.TextDark;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            KoreanUiFont.Apply(text);

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
            placeholderGo.transform.SetParent(textArea.transform, false);
            UiFactory.Stretch(placeholderGo.GetComponent<RectTransform>());
            var placeholder = placeholderGo.AddComponent<TextMeshProUGUI>();
            placeholder.text = "이름";
            placeholder.fontSize = 20;
            placeholder.color = UiTheme.TextFaint;
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            placeholder.fontStyle = FontStyles.Italic;
            KoreanUiFont.Apply(placeholder);

            var input = fieldRt.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = areaRt;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = 12;
            input.lineType = TMP_InputField.LineType.SingleLine;
            return input;
        }
    }
}
