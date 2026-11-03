using UnityEngine;

/// <summary>
/// D-289 World 층: World*.asset 불러오기. 에디터는 Assets/World/ 에서, 플레이어는 Resources 에서(지금은 에셋을 Resources 에 안 넣었다 →
/// 플레이어 빌드에선 null, 호출하는 쪽이 옛 기본값으로 버틴다).
/// </summary>
public static class WorldData
{
    public const string Folder = "Assets/World/";

    public static WorldPlaces Places => Load<WorldPlaces>("WorldPlaces");
    public static WorldPeople People => Load<WorldPeople>("WorldPeople");
    public static WorldOrders Orders => Load<WorldOrders>("WorldOrders");
    public static WorldNumbers Numbers => Load<WorldNumbers>("WorldNumbers");

    static T Load<T>(string name) where T : Object
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(Folder + name + ".asset");
#else
        return Resources.Load<T>(name);
#endif
    }
}
