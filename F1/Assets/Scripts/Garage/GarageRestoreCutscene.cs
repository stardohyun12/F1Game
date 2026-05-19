using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PitCrew.Garage
{
    public sealed class GarageRestoreCutscene : MonoBehaviour
    {
        Canvas _canvas;
        CanvasGroup _cg;
        Image _bg;
        TMP_Text _flavor;

        public bool IsPlaying { get; private set; }

        void EnsureBuilt()
        {
            if (_canvas != null) return;
            var go = new GameObject("GarageCutsceneCanvas");
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 5000;
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            go.AddComponent<GraphicRaycaster>();
            _cg = go.AddComponent<CanvasGroup>();

            var bgGo = new GameObject("Dim", typeof(RectTransform));
            bgGo.transform.SetParent(go.transform, false);
            _bg = bgGo.AddComponent<Image>();
            _bg.color = new Color(0,0,0,0.55f);
            var rt = (RectTransform)bgGo.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var flavorGo = new GameObject("Flavor", typeof(RectTransform));
            flavorGo.transform.SetParent(go.transform, false);
            _flavor = flavorGo.AddComponent<TextMeshProUGUI>();
            _flavor.alignment = TextAlignmentOptions.Center;
            _flavor.fontSize = 48;
            var th = UITheme.Active;
            if (th != null && th.fontPrimary != null) _flavor.font = th.fontPrimary;
            _flavor.color = th != null ? th.colorText : Color.white;
            var frt = (RectTransform)flavorGo.transform;
            frt.anchorMin = new Vector2(0.1f, 0.4f); frt.anchorMax = new Vector2(0.9f, 0.6f);
            frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;

            _canvas.enabled = false;
        }

        public IEnumerator Play(F1Part part)
        {
            EnsureBuilt();
            IsPlaying = true;
            _canvas.enabled = true;
            _flavor.text = part.flavorLine;

            float dur = part.cutsceneSeconds;
            float t = 0f;
            while (t < 0.3f) { t += Time.unscaledDeltaTime; _cg.alpha = Mathf.Lerp(0f, 1f, t/0.3f); yield return null; }
            _cg.alpha = 1f;
            yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, dur - 0.6f));
            t = 0f;
            while (t < 0.3f) { t += Time.unscaledDeltaTime; _cg.alpha = Mathf.Lerp(1f, 0f, t/0.3f); yield return null; }
            _cg.alpha = 0f;
            _canvas.enabled = false;
            IsPlaying = false;
        }
    }
}
