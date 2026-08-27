using System;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 인내심 게이지 — fillAmount로 부드럽게 줄어들고, 남은 양에 따라 초록→노랑→빨강으로 변한다.
    /// </summary>
    public sealed class PatienceMeter : MonoBehaviour
    {
        private Image _fill;
        private float _duration = 16f;
        private float _remaining;
        private bool _running;
        private Action _onEmpty;

        public float Normalized => _duration <= 0f ? 0f : Mathf.Clamp01(_remaining / _duration);

        public void Bind(Image fill)
        {
            _fill = fill;
            if (_fill == null) return;
            _fill.sprite = UiTheme.WhiteSprite;
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _fill.fillAmount = 1f;
        }

        public void Begin(float seconds, Action onEmpty)
        {
            _duration = Mathf.Max(4f, seconds);
            _remaining = _duration;
            _onEmpty = onEmpty;
            _running = true;
            ApplyVisual();
            enabled = true;
        }

        public void Stop()
        {
            _running = false;
            _onEmpty = null;
        }

        private void Update()
        {
            if (!_running) return;
            _remaining -= UnityEngine.Time.deltaTime;
            if (_remaining <= 0f)
            {
                _remaining = 0f;
                ApplyVisual();
                _running = false;
                var cb = _onEmpty;
                _onEmpty = null;
                cb?.Invoke();
                return;
            }
            ApplyVisual();
        }

        private void ApplyVisual()
        {
            if (_fill == null) return;
            float t = Normalized;
            _fill.color = t > 0.5f
                ? Color.Lerp(UiTheme.Gold, UiTheme.Success, (t - 0.5f) * 2f)
                : Color.Lerp(UiTheme.Danger, UiTheme.Gold, t * 2f);
            _fill.fillAmount = t;
            _fill.transform.localScale = Vector3.one;
        }
    }
}
