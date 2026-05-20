using System;
using UnityEngine;

/// <summary>
/// CargoType → 원물 모델 매핑 (B안, 2026-05-20). 코드 하드코드 금지 (asset.md).
/// modelPrefab=null 이면 기존 큐브+카드보드 폴백 (Box 등).
/// </summary>
[CreateAssetMenu(menuName = "PitCrew/Cargo Model Catalog", fileName = "CargoModelCatalog")]
public sealed class CargoModelCatalog : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public CargoType cargoType;
        public GameObject modelPrefab; // 원물 fbx 프리팹. null → 큐브 폴백.
        public float uniformScale;     // 포장 작업대에서 보기 좋은 균일 스케일.
    }

    [SerializeField] private Entry[] entries;

    /// <summary>해당 타입의 모델 프리팹 (없으면 null → 호출부가 큐브 폴백).</summary>
    public bool TryGet(CargoType type, out GameObject prefab, out float scale)
    {
        prefab = null; scale = 1f;
        if (entries == null) return false;
        foreach (var e in entries)
            if (e.cargoType == type && e.modelPrefab != null)
            {
                prefab = e.modelPrefab;
                scale = e.uniformScale > 0f ? e.uniformScale : 1f;
                return true;
            }
        return false;
    }
}
