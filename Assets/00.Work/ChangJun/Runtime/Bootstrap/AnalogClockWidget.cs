using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 준비·영업·마감 단계용 간단한 아날로그 시계 아이콘.
    /// </summary>
    public static class AnalogClockWidget
    {
        public static RectTransform Create(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            int hour, int minute, bool active)
        {
            var face = UiFactory.CreatePanel(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            var border = face.gameObject.AddComponent<Image>();
            border.color = UiTheme.Border;
            TryRoundSprite(border);

            var inner = UiFactory.CreatePanel(face, "Inner",
                Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            var innerImg = inner.gameObject.AddComponent<Image>();
            innerImg.color = active ? UiTheme.TanRow : new Color(0.35f, 0.22f, 0.14f, 0.9f);
            TryRoundSprite(innerImg);

            CreateHand(inner, "Hour", 0.42f, 5f, HourAngle(hour, minute),
                active ? UiTheme.TextDark : new Color(0.85f, 0.75f, 0.6f));
            CreateHand(inner, "Minute", 0.62f, 3f, MinuteAngle(minute),
                active ? UiTheme.Accent : new Color(0.75f, 0.55f, 0.4f));

            var hub = UiFactory.CreatePanel(inner, "Hub",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-5f, -5f), new Vector2(5f, 5f));
            hub.gameObject.AddComponent<Image>().color = active ? UiTheme.Gold : UiTheme.TextFaint;

            return face;
        }

        public static void SetHands(RectTransform face, int hour, int minute, bool active)
        {
            var inner = face.Find("Inner") as RectTransform;
            if (inner == null) return;
            var innerImg = inner.GetComponent<Image>();
            if (innerImg != null)
                innerImg.color = active ? UiTheme.TanRow : new Color(0.35f, 0.22f, 0.14f, 0.9f);

            SetHand(inner, "Hour", HourAngle(hour, minute));
            SetHand(inner, "Minute", MinuteAngle(minute));
        }

        private static void CreateHand(RectTransform parent, string name, float length01, float width,
            float zAngle, Color color)
        {
            var hand = new GameObject(name, typeof(RectTransform));
            hand.transform.SetParent(parent, false);
            var rt = hand.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(width, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.localRotation = Quaternion.Euler(0f, 0f, zAngle);
            hand.AddComponent<Image>().color = color;

            // 길이는 부모 높이에 비례하도록 stretch 대신 sizeDelta를 레이아웃 이후 맞춤
            var fitter = hand.AddComponent<ClockHandLength>();
            fitter.length01 = length01;
        }

        private static void SetHand(RectTransform inner, string name, float zAngle)
        {
            var t = inner.Find(name) as RectTransform;
            if (t != null)
                t.localRotation = Quaternion.Euler(0f, 0f, zAngle);
        }

        private static float HourAngle(int hour, int minute) =>
            -((hour % 12) * 30f + minute * 0.5f);

        private static float MinuteAngle(int minute) => -(minute * 6f);

        private static void TryRoundSprite(Image image)
        {
            var knob = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            if (knob == null || image == null) return;
            image.sprite = knob;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
        }
    }

    /// <summary>시계 바늘 길이를 부모 크기에 맞춘다.</summary>
    public sealed class ClockHandLength : MonoBehaviour
    {
        public float length01 = 0.5f;

        private void OnRectTransformDimensionsChange() => Apply();
        private void OnEnable() => Apply();

        private void Apply()
        {
            var rt = transform as RectTransform;
            var parent = rt != null ? rt.parent as RectTransform : null;
            if (rt == null || parent == null) return;
            float h = parent.rect.height;
            if (h <= 1f) return;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, h * length01);
        }
    }
}
