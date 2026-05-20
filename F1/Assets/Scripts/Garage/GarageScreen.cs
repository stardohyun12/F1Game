using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PitCrew.Garage
{
    public sealed class GarageScreen : MonoBehaviour
    {
        // Day 2 UI 톤 통일 helper
        static void ApplyTextTheme(TMP_Text t, Color color, float size)
        {
            var th = UITheme.Active;
            if (th != null && th.fontPrimary != null) t.font = th.fontPrimary;
            t.color = color;
            t.fontSize = size;
        }
        static Color ThemePanel => UITheme.Active != null ? UITheme.Active.colorPanel : new Color(0.12f,0.14f,0.18f,0.92f);
        static Color ThemeText => UITheme.Active != null ? UITheme.Active.colorText : Color.white;
        static Color ThemeAccent => UITheme.Active != null ? UITheme.Active.colorAccent : new Color(1f,0.82f,0.25f,1f);
        static Color ThemeMuted => UITheme.Active != null ? UITheme.Active.colorTextMuted : new Color(0.72f,0.74f,0.78f,1f);
        static Color ThemeOk => UITheme.Active != null ? UITheme.Active.colorOkText : new Color(0.55f,0.95f,0.60f,1f);
        static Color ThemeCrit => UITheme.Active != null ? UITheme.Active.colorCritText : new Color(1f,0.45f,0.42f,1f);

        [SerializeField] private GarageRestoreCutscene cutscene;
        [Header("머신 디스플레이 (비우면 텍스트 placeholder로 폴백)")]
        [SerializeField] private GameObject machineModelPrefab;
        [SerializeField] private float machineSpinSpeed = 18f;

        Canvas _canvas;
        CanvasGroup _cg;
        TMP_Text _walletText;
        Transform _cardRow;
        TMP_Text _detailTitle, _detailDesc, _detailCost;
        Button _restoreBtn, _closeBtn;
        F1Part _selected;
        readonly List<GameObject> _cardGos = new List<GameObject>();

        // 머신 스테이지 (3D 차 → RenderTexture)
        Transform _stageRoot, _machineModel;
        Camera _stageCam;
        RenderTexture _machineRT;

        public bool IsOpen { get; private set; }

        int _lastWallet = -1;

        void Awake()
        {
            EnsureBuilt();
            _canvas.enabled = false;
        }

        void Start()
        {
            if (Services.Restore != null) Services.Restore.OnRestored += _ => { if (IsOpen) RefreshCards(); };
        }

        void Update()
        {
            if (!IsOpen) return;
            if (_machineModel != null) _machineModel.Rotate(0f, machineSpinSpeed * Time.deltaTime, 0f, Space.Self);
            int w = Services.Upgrade?.Wallet ?? 0;
            if (w != _lastWallet) { _lastWallet = w; RefreshWallet(); RefreshCards(); }
        }

        // 3D 머신을 메인 카메라가 못 보는 먼 곳에 세우고 전용 카메라로 RenderTexture에 렌더
        void BuildMachineStage()
        {
            if (machineModelPrefab == null) return;

            _stageRoot = new GameObject("GarageMachineStage").transform;
            _stageRoot.position = new Vector3(10000f, 10000f, 10000f);

            _machineModel = Instantiate(machineModelPrefab, _stageRoot).transform;
            _machineModel.localPosition = Vector3.zero;
            _machineModel.localRotation = Quaternion.Euler(0f, -120f, 0f); // 3/4 사이드뷰 시작

            var lightGo = new GameObject("StageLight");
            lightGo.transform.SetParent(_stageRoot, false);
            var lt = lightGo.AddComponent<Light>();
            lt.type = LightType.Directional;
            lt.transform.rotation = Quaternion.Euler(35f, -140f, 0f);
            lt.intensity = 1.3f; lt.color = new Color(1f, 0.97f, 0.9f);

            _machineRT = new RenderTexture(1024, 640, 16) { name = "MachineRT" };
            var camGo = new GameObject("StageCamera");
            camGo.transform.SetParent(_stageRoot, false);
            _stageCam = camGo.AddComponent<Camera>();
            _stageCam.targetTexture = _machineRT;
            _stageCam.clearFlags = CameraClearFlags.SolidColor;
            _stageCam.backgroundColor = new Color(0.10f, 0.11f, 0.14f, 0f); // 패널 톤, 알파0(투명)
            _stageCam.fieldOfView = 26f;
            _stageCam.nearClipPlane = 0.05f; _stageCam.farClipPlane = 50f;
            _stageCam.transform.localPosition = new Vector3(0f, 1.1f, -5f);
            _stageCam.transform.LookAt(_stageRoot.position + Vector3.up * 0.35f);
            _stageCam.enabled = false; // 차고 열릴 때만 렌더
        }

        void OnDestroy()
        {
            if (_stageCam != null) _stageCam.targetTexture = null;
            if (_machineRT != null) { _machineRT.Release(); Destroy(_machineRT); _machineRT = null; }
            if (_stageRoot != null) Destroy(_stageRoot.gameObject);
        }

        void EnsureBuilt()
        {
            if (_canvas != null) return;
            var go = new GameObject("GarageCanvas");
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 4000;
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            go.AddComponent<GraphicRaycaster>();
            _cg = go.AddComponent<CanvasGroup>();

            // BG dim
            var bgGo = new GameObject("BG", typeof(RectTransform));
            bgGo.transform.SetParent(go.transform, false);
            var bg = bgGo.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.07f, 1f); // 불투명 — 뒤 화면(보드 등) 비침 방지
            var bgrt = bg.rectTransform; bgrt.anchorMin = Vector2.zero; bgrt.anchorMax = Vector2.one; bgrt.offsetMin = Vector2.zero; bgrt.offsetMax = Vector2.zero;

            // Wallet
            var walletGo = new GameObject("Wallet", typeof(RectTransform));
            walletGo.transform.SetParent(go.transform, false);
            _walletText = walletGo.AddComponent<TextMeshProUGUI>();
            ApplyTextTheme(_walletText, ThemeAccent, 30); _walletText.alignment = TextAlignmentOptions.TopLeft;
            var wrt = _walletText.rectTransform; wrt.anchorMin = new Vector2(0.02f,0.92f); wrt.anchorMax = new Vector2(0.32f,0.98f); wrt.offsetMin = Vector2.zero; wrt.offsetMax = Vector2.zero;

            // Close
            var closeGo = new GameObject("Close", typeof(RectTransform));
            closeGo.transform.SetParent(go.transform, false);
            var closeImg = closeGo.AddComponent<Image>(); closeImg.color = new Color(0.2f,0.2f,0.25f);
            _closeBtn = closeGo.AddComponent<Button>();
            var closeRt = closeImg.rectTransform; closeRt.anchorMin = new Vector2(0.905f,0.905f); closeRt.anchorMax = new Vector2(0.985f,0.965f); closeRt.offsetMin = Vector2.zero; closeRt.offsetMax = Vector2.zero;
            var closeTxtGo = new GameObject("Txt", typeof(RectTransform));
            closeTxtGo.transform.SetParent(closeGo.transform, false);
            var closeTxt = closeTxtGo.AddComponent<TextMeshProUGUI>();
            closeTxt.text = "닫기"; closeTxt.alignment = TextAlignmentOptions.Center; ApplyTextTheme(closeTxt, ThemeText, 22);
            var ctxRt = closeTxt.rectTransform; ctxRt.anchorMin = Vector2.zero; ctxRt.anchorMax = Vector2.one; ctxRt.offsetMin = Vector2.zero; ctxRt.offsetMax = Vector2.zero;
            _closeBtn.onClick.AddListener(Hide);

            // 머신 디스플레이 — 3D 차를 RenderTexture에 렌더해서 패널에 표시
            var carPanelGo = new GameObject("CarPanel", typeof(RectTransform));
            carPanelGo.transform.SetParent(go.transform, false);
            var carImg = carPanelGo.AddComponent<Image>(); carImg.color = new Color(0.10f,0.11f,0.14f);
            var crt = carImg.rectTransform; crt.anchorMin = new Vector2(0.15f,0.35f); crt.anchorMax = new Vector2(0.85f,0.85f); crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

            BuildMachineStage();
            if (_machineRT != null)
            {
                var viewGo = new GameObject("MachineView", typeof(RectTransform));
                viewGo.transform.SetParent(carPanelGo.transform, false);
                var raw = viewGo.AddComponent<RawImage>();
                raw.texture = _machineRT;
                var vrt = raw.rectTransform; vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one; vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;
            }
            else
            {
                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(carPanelGo.transform, false);
                var label = labelGo.AddComponent<TextMeshProUGUI>();
                label.text = "[ F1 머신 ]"; label.alignment = TextAlignmentOptions.Center; ApplyTextTheme(label, ThemeMuted, 32);
                var lrt = label.rectTransform; lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            }

            // Detail panel
            var detailGo = new GameObject("Detail", typeof(RectTransform));
            detailGo.transform.SetParent(go.transform, false);
            var detailImg = detailGo.AddComponent<Image>(); detailImg.color = new Color(0.08f,0.08f,0.10f,0.9f);
            var drt = detailImg.rectTransform; drt.anchorMin = new Vector2(0.34f,0.85f); drt.anchorMax = new Vector2(0.88f,0.94f); drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;

            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(detailGo.transform, false);
            _detailTitle = titleGo.AddComponent<TextMeshProUGUI>();
            ApplyTextTheme(_detailTitle, ThemeText, 28); _detailTitle.alignment = TextAlignmentOptions.TopLeft;
            var dtrt = _detailTitle.rectTransform; dtrt.anchorMin = new Vector2(0.02f, 0.5f); dtrt.anchorMax = new Vector2(0.6f, 1f); dtrt.offsetMin = Vector2.zero; dtrt.offsetMax = Vector2.zero;

            var descGo = new GameObject("Desc", typeof(RectTransform));
            descGo.transform.SetParent(detailGo.transform, false);
            _detailDesc = descGo.AddComponent<TextMeshProUGUI>();
            ApplyTextTheme(_detailDesc, ThemeMuted, 20); _detailDesc.alignment = TextAlignmentOptions.TopLeft;
            var ddrt = _detailDesc.rectTransform; ddrt.anchorMin = new Vector2(0.02f, 0f); ddrt.anchorMax = new Vector2(0.6f, 0.5f); ddrt.offsetMin = Vector2.zero; ddrt.offsetMax = Vector2.zero;

            var costGo = new GameObject("Cost", typeof(RectTransform));
            costGo.transform.SetParent(detailGo.transform, false);
            _detailCost = costGo.AddComponent<TextMeshProUGUI>();
            ApplyTextTheme(_detailCost, ThemeAccent, 24); _detailCost.alignment = TextAlignmentOptions.TopRight;
            var dcrt = _detailCost.rectTransform; dcrt.anchorMin = new Vector2(0.6f, 0.5f); dcrt.anchorMax = new Vector2(0.78f, 1f); dcrt.offsetMin = Vector2.zero; dcrt.offsetMax = Vector2.zero;

            var restoreGo = new GameObject("Restore", typeof(RectTransform));
            restoreGo.transform.SetParent(detailGo.transform, false);
            var restoreImg = restoreGo.AddComponent<Image>(); restoreImg.color = new Color(0.30f,0.55f,0.30f);
            _restoreBtn = restoreGo.AddComponent<Button>();
            var rbrt = restoreImg.rectTransform; rbrt.anchorMin = new Vector2(0.80f, 0.10f); rbrt.anchorMax = new Vector2(0.98f, 0.90f); rbrt.offsetMin = Vector2.zero; rbrt.offsetMax = Vector2.zero;
            var rbTxtGo = new GameObject("Txt", typeof(RectTransform));
            rbTxtGo.transform.SetParent(restoreGo.transform, false);
            var rbTxt = rbTxtGo.AddComponent<TextMeshProUGUI>();
            rbTxt.text = "복원"; rbTxt.alignment = TextAlignmentOptions.Center; ApplyTextTheme(rbTxt, ThemeText, 28);
            var rbtRt = rbTxt.rectTransform; rbtRt.anchorMin = Vector2.zero; rbtRt.anchorMax = Vector2.one; rbtRt.offsetMin = Vector2.zero; rbtRt.offsetMax = Vector2.zero;
            _restoreBtn.onClick.AddListener(OnRestoreClicked);

            // Cards row
            var rowGo = new GameObject("Cards", typeof(RectTransform));
            rowGo.transform.SetParent(go.transform, false);
            var rowRt = (RectTransform)rowGo.transform;
            rowRt.anchorMin = new Vector2(0.05f, 0.05f); rowRt.anchorMax = new Vector2(0.95f, 0.30f); rowRt.offsetMin = Vector2.zero; rowRt.offsetMax = Vector2.zero;
            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12; hlg.padding = new RectOffset(8,8,8,8);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true; hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            _cardRow = rowGo.transform;
        }

        public void Show()
        {
            EnsureBuilt();
            _canvas.enabled = true;
            IsOpen = true;
            if (_stageCam != null) _stageCam.enabled = true;
            RefreshWallet();
            RefreshCards();
            _selected = null;
            RefreshDetail();
        }

        public void Hide()
        {
            if (_canvas != null) _canvas.enabled = false;
            if (_stageCam != null) _stageCam.enabled = false;
            IsOpen = false;
            // D-085: 차고 닫으면 다음 잡 선택 화면(OrderBoard)로. 빈 맵 방지.
            var run = Services.Run as RunController;
            if (run != null) run.RestartFromBoard();
        }

        void RefreshWallet()
        {
            if (_walletText == null) return;
            _walletText.text = $"지갑: ₩{(Services.Upgrade?.Wallet ?? 0)}";
        }

        void RefreshCards()
        {
            foreach (var go in _cardGos) if (go != null) Destroy(go);
            _cardGos.Clear();
            var catalog = Services.Restore?.Catalog;
            if (catalog == null) return;
            foreach (var part in catalog.parts)
            {
                if (part == null) continue;
                var cardGo = new GameObject($"Card_{part.id}", typeof(RectTransform));
                cardGo.transform.SetParent(_cardRow, false);
                var cardImg = cardGo.AddComponent<Image>();
                bool owned = Services.Restore.IsRestored(part.id);
                bool afford = Services.Restore.CanAfford(part);
                cardImg.color = owned ? new Color(0.30f,0.55f,0.30f)
                                : (afford ? new Color(0.18f,0.18f,0.22f)
                                          : new Color(0.12f,0.12f,0.14f));
                var btn = cardGo.AddComponent<Button>();
                var captured = part;
                btn.onClick.AddListener(() => { _selected = captured; RefreshDetail(); });

                var nameGo = new GameObject("Name", typeof(RectTransform));
                nameGo.transform.SetParent(cardGo.transform, false);
                var name = nameGo.AddComponent<TextMeshProUGUI>();
                name.text = part.displayName;
                name.alignment = TextAlignmentOptions.Center;
                ApplyTextTheme(name, owned ? ThemeText : (afford ? ThemeText : ThemeMuted), 24);
                var nrt = name.rectTransform; nrt.anchorMin = new Vector2(0f, 0.55f); nrt.anchorMax = new Vector2(1f, 0.95f); nrt.offsetMin = Vector2.zero; nrt.offsetMax = Vector2.zero;

                var costGo = new GameObject("Cost", typeof(RectTransform));
                costGo.transform.SetParent(cardGo.transform, false);
                var cost = costGo.AddComponent<TextMeshProUGUI>();
                Color costCol;
                if (owned) { cost.text = "✓ 복원됨"; costCol = ThemeOk; }
                else if (afford) { cost.text = $"₩{part.restoreCost}"; costCol = ThemeAccent; }
                else { int shortAmt = part.restoreCost - (Services.Upgrade?.Wallet ?? 0); cost.text = $"₩{shortAmt} 부족"; costCol = ThemeCrit; }
                cost.alignment = TextAlignmentOptions.Center;
                ApplyTextTheme(cost, costCol, 18);
                var crt = cost.rectTransform; crt.anchorMin = new Vector2(0f, 0.10f); crt.anchorMax = new Vector2(1f, 0.50f); crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

                _cardGos.Add(cardGo);
            }
        }

        void RefreshDetail()
        {
            if (_selected == null)
            {
                _detailTitle.text = "부품을 선택하세요";
                _detailDesc.text = "";
                _detailCost.text = "";
                _restoreBtn.interactable = false;
                return;
            }
            _detailTitle.text = _selected.displayName;
            _detailDesc.text = _selected.mechanicDescription;
            bool owned = Services.Restore.IsRestored(_selected.id);
            bool afford = Services.Restore.CanAfford(_selected);
            _detailCost.text = owned ? "✓" : $"₩{_selected.restoreCost}";
            _restoreBtn.interactable = !owned && afford;
        }

        void OnRestoreClicked()
        {
            if (_selected == null) return;
            var part = _selected;
            if (Services.Restore.TryRestore(part))
            {
                if (cutscene != null) StartCoroutine(cutscene.Play(part));
                RefreshCards();
                RefreshDetail();
                RefreshWallet();
            }
        }
    }
}
