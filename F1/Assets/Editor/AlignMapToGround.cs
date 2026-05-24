using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 맵 수직 기준 단일화 — "땅 = y 0". 차 묻힘·선 공중부양의 근본 해결.
///
/// 배경(세션32 진단): 이 맵의 건물·가구·차량·프롭·보도는 전부 바닥 y=0에 놓여 있다
/// (= 원래 땅=0으로 저작됨). 그런데 시간이 지나며 땜빵 스크립트(RaiseRoadTiles 등)가
/// 도로만 위로 올리고(표면 0.11) 잔디는 -0.05로 내려가, 서로 어긋났다. 결과:
///  - 도로 타일엔 콜라이더가 없어 차가 잔디 평면(-0.05)에 떨어짐 → 도로(0.11)에 0.25 묻힘
///  - 도로선이 도로 표면(0.11)이 아니라 연석 꼭대기(0.17)를 기준 잡아 0.19에 떠 있음
///
/// 이 스크립트는 어긋난 셋(잔디·도로·도로선)만 땅=0 기준으로 정렬한다. 이미 0에 맞는
/// 나머지(건물/가구/차량/프롭/보도)는 건드리지 않는다. 멱등(재실행 안전).
/// 룩 재적용 체인의 **마지막**에 둘 것(모든 배치 후 최종 정렬).
/// 메뉴: PitCrew/Map/Align Map To Ground
/// </summary>
public static class AlignMapToGround
{
    const float GroundTopTarget = 0f;       // 잔디 표면 = 땅 기준
    const float RoadSurfaceTarget = 0.01f;  // 도로 표면 = 땅 위 1cm (z-fight 회피, 사실상 flush)
    const float RoadLineTarget = 0.02f;     // 도로선 = 도로 위 1cm

    // 도로 타일: 표면(drivable) = pos.y + 0.06 (메시 local 0.010 × 스케일 6).
    const float RoadSurfaceAbovePivot = 0.06f;

    [MenuItem("PitCrew/Map/Align Map To Ground")]
    public static void Execute()
    {
        // 1) 잔디(Ground): 렌더러 top을 0으로
        var ground = GameObject.Find("Ground");
        if (ground != null)
        {
            var mr = ground.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                float delta = GroundTopTarget - mr.bounds.max.y;
                if (Mathf.Abs(delta) > 0.001f)
                    ground.transform.position += new Vector3(0, delta, 0);
            }
        }
        else Debug.LogWarning("[Align] Ground 없음.");

        // 2) 도로(Track): 표면을 RoadSurfaceTarget로 (절대 pos.y로 멱등)
        var track = GameObject.Find("Track");
        int tiles = 0;
        if (track != null)
        {
            float targetPivotY = RoadSurfaceTarget - RoadSurfaceAbovePivot; // = -0.05
            // 슬로프(의도된 높이차) 보호: pivot이 표준에서 0.02 넘게 벗어난 타일은 경고만
            foreach (Transform t in track.transform)
            {
                t.position = new Vector3(t.position.x, targetPivotY, t.position.z);
                tiles++;
            }
        }
        else Debug.LogWarning("[Align] Track 없음.");

        // 3) 도로선(RoadLines): Y를 RoadLineTarget로 (절대, 멱등)
        var lines = GameObject.Find("RoadLines");
        int nLines = 0;
        if (lines != null)
        {
            foreach (Transform t in lines.transform)
            {
                t.position = new Vector3(t.position.x, RoadLineTarget, t.position.z);
                nLines++;
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log($"[Align] ✓ 땅=0 기준 정렬 — 잔디 top→0, 도로 표면→{RoadSurfaceTarget} (타일 {tiles}), 도로선→{RoadLineTarget} (선 {nLines}). 나머지(건물/가구/프롭/보도)는 이미 0이라 안 건드림.");
    }
}
