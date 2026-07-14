using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJun.Bootstrap
{
    /// <summary>
    /// 전체 화면 페이드 인/아웃. 날이 지나가는 연출에 사용한다.
    /// </summary>
    public sealed class ScreenFadeController
    {
        private readonly GameObject _root;
        private readonly CanvasGroup _group;
        private readonly Image _image;

        public ScreenFadeController(MonoBehaviour host)
        {
            _root = UiFactory.CreateOverlayRoot("ScreenFade", 300);
            _group = _root.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            var dim = UiFactory.CreateStretchChild(_root.transform, "Dim");
            _image = dim.gameObject.AddComponent<Image>();
            _image.color = Color.black;
            _image.raycastTarget = true;

            _root.SetActive(true);
        }

        public IEnumerator FadeOut(float duration, Action onComplete = null)
        {
            yield return RunFade(1f, duration, blockInput: true);
            onComplete?.Invoke();
        }

        public IEnumerator FadeIn(float duration, Action onComplete = null)
        {
            yield return RunFade(0f, duration, blockInput: false);
            onComplete?.Invoke();
        }

        public IEnumerator Transition(float holdSeconds, float fadeOut, float fadeIn, Action midAction = null)
        {
            yield return RunFade(1f, fadeOut, blockInput: true);
            midAction?.Invoke();
            if (holdSeconds > 0f)
                yield return new WaitForSeconds(holdSeconds);
            yield return RunFade(0f, fadeIn, blockInput: false);
        }

        private IEnumerator RunFade(float targetAlpha, float duration, bool blockInput)
        {
            _group.blocksRaycasts = blockInput;
            _group.interactable = blockInput;

            float start = _group.alpha;
            if (duration <= 0f)
            {
                _group.alpha = targetAlpha;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += UnityEngine.Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _group.alpha = Mathf.Lerp(start, targetAlpha, t);
                yield return null;
            }

            _group.alpha = targetAlpha;
            _group.blocksRaycasts = blockInput;
            _group.interactable = blockInput;
        }
    }
}
