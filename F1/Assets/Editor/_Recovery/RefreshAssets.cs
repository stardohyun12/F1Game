using UnityEditor;

// 1회용: AssetDatabase 강제 새로고침. 신규 파일 import 안 된 경우 사용.
public static class RefreshAssets
{
    public static void Execute()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        UnityEngine.Debug.Log("[RefreshAssets] AssetDatabase.Refresh 완료.");
    }
}
