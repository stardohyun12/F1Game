using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PitCrew.Packing
{
    public sealed class PackingScreen : MonoBehaviour, IPackingScreen
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Camera packingCamera;
        [SerializeField] private Transform cargoSpawnPoint;

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

        OrderJob currentJob;
        List<GameObject> spawnedItems = new();
        int activeItemIndex;

        // UI placeholders (filled in Task 9)
        TMP_Text titleText, totalCostText, coverageText;
        Image coverageBarFill;
        TMP_Text[] costPerMaterialText = new TMP_Text[3];

        public void Show(OrderJob job)
        {
            currentJob = job;
            BuildUIIfNeeded();
            ClearItems();
            int n = Mathf.Clamp(job.itemCount, 1, 3);
            for (int i = 0; i < n; i++) SpawnItem(i);
            ActivateItem(0);
            if (panelRoot != null) panelRoot.SetActive(true);
            if (packingCamera != null) packingCamera.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (packingCamera != null) packingCamera.gameObject.SetActive(false);
            ClearItems();
        }

        void BuildUIIfNeeded()
        {
            if (panelRoot != null) return;
            // 최소 UGUI 트리 — 자세한 위젯은 Task 9에서 채움
            var canvasGO = new GameObject("PackingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            panelRoot = canvasGO;
        }

        void ClearItems()
        {
            foreach (var go in spawnedItems) if (go) Destroy(go);
            spawnedItems.Clear();
        }

        void SpawnItem(int idx)
        {
            // 데모: primitive cube. 추후 OrderJob.cargoPrefab.
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"PackingItem_{idx}";
            if (cargoSpawnPoint != null) go.transform.SetParent(cargoSpawnPoint, false);
            go.transform.localPosition = Vector3.zero;
            var state = go.AddComponent<WrapState>();
            var painter = go.AddComponent<WrapPainter>();
            // TODO Task 9/10: painter.Init(state) + packingCamera + MeshCollider 주입.
            //   현재는 wiring 없어 painter는 동작 안 함 (compile/spawn 검증 용도).
            _ = state; _ = painter;
            go.SetActive(false);
            spawnedItems.Add(go);
        }

        void ActivateItem(int idx)
        {
            for (int i = 0; i < spawnedItems.Count; i++) spawnedItems[i].SetActive(i == idx);
            activeItemIndex = idx;
        }

        public GameObject ActiveItem => spawnedItems.Count == 0 ? null : spawnedItems[activeItemIndex];
    }
}
