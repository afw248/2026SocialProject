using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 준비·영업·마감 단계용 아날로그 시계. 절차적 원형 스프라이트로 네모가 되지 않게 그린다.
    /// </summary>
    public static class AnalogClockWidget
    {
        public static RectTransform Create(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            int hour, int minute, bool active)
        {
            var face = UiFactory.CreatePanel(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            var border = face.gameObject.AddComponent<Image>();
            border.sprite = UiTheme.CircleSprite;
            border.color = UiTheme.Border;
            border.preserveAspect = true;

            var inner = UiFactory.CreatePanel(face, "Inner",
                Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f));
            var innerImg = inner.gameObject.AddComponent<Image>();
            innerImg.sprite = UiTheme.CircleSprite;
            innerImg.preserveAspect = true;
            innerImg.color = active ? UiTheme.TanRow : new Color(0.35f, 0.22f, 0.14f, 0.92f);

            CreateHand(inner, "Hour", 0.38f, 5f, HourAngle(hour, minute),
                active ? UiTheme.TextDark : new Color(0.85f, 0.75f, 0.6f));
            CreateHand(inner, "Minute", 0.58f, 3f, MinuteAngle(minute),
                active ? UiTheme.Accent : new Color(0.75f, 0.55f, 0.4f));

            var hub = UiFactory.CreatePanel(inner, "Hub",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-6f, -6f), new Vector2(6f, 6f));
            var hubImg = hub.gameObject.AddComponent<Image>();
            hubImg.sprite = UiTheme.CircleSprite;
            hubImg.preserveAspect = true;
            hubImg.color = active ? UiTheme.Gold : UiTheme.TextFaint;

            return face;
        }

        public static void SetHands(RectTransform face, int hour, int minute, bool active)
        {
            var inner = face.Find("Inner") as RectTransform;
            if (inner == null) return;
            var innerImg = inner.GetComponent<Image>();
            if (innerImg != null)
                innerImg.color = active ? UiTheme.TanRow : new Color(0.35f, 0.22f, 0.14f, 0.92f);

            var hub = inner.Find("Hub") as RectTransform;
            if (hub != null)
            {
                var hubImg = hub.GetComponent<Image>();
                if (hubImg != null)
                    hubImg.color = active ? UiTheme.Gold : UiTheme.TextFaint;
            }

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
            var img = hand.AddComponent<Image>();
            img.sprite = UiTheme.WhiteSprite;
            img.color = color;
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
