using UnityEditor;
using UnityEngine;
using System.IO;
using PitCrew.Packing;

namespace PitCrew.Editor
{
    public static class PackingDevHarness
    {
        [MenuItem("PitCrew/Packing/Create WrapMaterial Assets")]
        public static void CreateWrapMaterials()
        {
            Directory.CreateDirectory("Assets/Resources/WrapMaterials");
            Make("Bubble",   0, costPerPixel: 0.03f, absorption: 0.25f, color: new Color(0.42f, 0.72f, 0.91f, 0.85f), key: KeyCode.Alpha1);
            Make("Foam",     1, costPerPixel: 0.07f, absorption: 0.55f, color: new Color(0.91f, 0.84f, 0.42f, 0.95f), key: KeyCode.Alpha2);
            Make("Hardcase", 2, costPerPixel: 0.18f, absorption: 0.85f, color: new Color(0.33f, 0.33f, 0.36f, 1.0f),  key: KeyCode.Alpha3);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void Make(string name, int idx, float costPerPixel, float absorption, Color color, KeyCode key)
        {
            var so = ScriptableObject.CreateInstance<WrapMaterialDef>();
            so.displayName = name;
            so.channelIndex = idx;
            so.costPerPixel = costPerPixel;
            so.absorption = absorption;
            so.displayColor = color;
            so.hotkey = key;
            AssetDatabase.CreateAsset(so, $"Assets/Resources/WrapMaterials/{name}.asset");
        }

        [MenuItem("PitCrew/Packing/Spawn Test Cube With WrapState")]
        public static void SpawnTestCube()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "WrapTestCube";
            go.transform.position = new Vector3(0, 1, 0);
            go.AddComponent<PitCrew.Packing.WrapState>();
            Selection.activeGameObject = go;
        }

        [MenuItem("PitCrew/Packing/Apply Overlay Material To Cube")]
        public static void ApplyOverlay()
        {
            var go = GameObject.Find("WrapTestCube");
            if (go == null) { Debug.LogError("Cube 없음"); return; }
            var state = go.GetComponent<PitCrew.Packing.WrapState>();
            var rend = go.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("PitCrew/WrapOverlay"));
            mat.SetTexture("_WrapTex", state.WrapRT);
            rend.sharedMaterial = mat;
        }

        [MenuItem("PitCrew/Packing/Paint Test Strip on Cube")]
        public static void PaintStrip()
        {
            var go = GameObject.Find("WrapTestCube");
            if (go == null) { Debug.LogError("WrapTestCube 없음. Spawn 먼저."); return; }
            var col = go.GetComponent<MeshCollider>() ?? go.AddComponent<MeshCollider>();
            var state = go.GetComponent<PitCrew.Packing.WrapState>();
            if (state == null) { Debug.LogError("WrapState 없음. Spawn 먼저."); return; }
            // WrapState.Awake가 exec 컨텍스트에서 AddComponent 시 즉시 호출 안 됨 — 강제 실행.
            if (state.WrapRT == null)
                typeof(PitCrew.Packing.WrapState).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(state, null);
            var painterType = typeof(PitCrew.Packing.WrapPainter);
            var painter = go.GetComponent<PitCrew.Packing.WrapPainter>() ?? go.AddComponent<PitCrew.Packing.WrapPainter>();
            // Init()으로 명시적 초기화 (targetState null 안전).
            painter.Init(state);
            var bubble = UnityEngine.Resources.Load<PitCrew.Packing.WrapMaterialDef>("WrapMaterials/Bubble");
            if (bubble == null) { Debug.LogError("WrapMaterials/Bubble 로드 실패. Create WrapMaterial Assets 먼저."); return; }
            // 직접 strip 그리기 (마우스 입력 없이) — 셰이더 동작 확인용
            painterType
                .GetMethod("DrawStrip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(painter, new object[] { new Vector2(0.2f, 0.5f), new Vector2(0.8f, 0.5f), bubble });
            Debug.Log("strip drawn");
        }
    }
}
