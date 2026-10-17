# Claude 4차(건물 벽 단면) C# 변경 — 3차 스냅샷 상태의 파일에 적용한다.
import sys, shutil
root = sys.argv[1]; snap = sys.argv[2]; mine = sys.argv[3]
def patch(path, reps):
    s = open(path, encoding='utf-8').read()
    for a, b in reps:
        assert s.count(a) == 1, (path, a[:60])
        s = s.replace(a, b)
    open(path, 'w', encoding='utf-8', newline='\n').write(s)

# 0) 3차 스냅샷으로 되돌린 뒤 내 lofi_pilot.py
for f in ['CoastalTownInstaller.cs', 'CoastalTownDensity.cs', 'CoastalPropsBuilder.cs']:
    shutil.copy(f'{snap}/{f}', f'{root}/F1/Assets/Editor/{f}')
shutil.copy(f'{mine}/lofi_pilot.py', f'{root}/tools/asset-gen/lofi_pilot.py')

# 1) 프리팹 빌더: 건물 변형 5종 (MeshCollider 기본)
patch(f'{root}/F1/Assets/Editor/CoastalPropsBuilder.cs', [
    ('"QuaySegment", "Mooring", "Pier", "Boat" };',
     '"QuaySegment", "Mooring", "Pier", "Boat",\n                                       // 4차(Claude) 건물 변형 — MeshCollider\n                                       "CornerShopB", "CornerShopC", "RepairGarageB", "CoastalHouseB", "CoastalHouseC" };'),
])
# 2) 배치 규칙: 변형 이름(…B/…C)을 기본 이름으로 정규화
patch(f'{root}/F1/Assets/Editor/CoastalTownDensity.cs', [
    ('    static (float w, float d) Body(string asset) => asset == "CornerShop" ? (6f, 5.2f) : asset == "RepairGarage" ? (6.4f, 5.6f) : (5f, 4.8f);',
     '    /// <summary>4차: 건물 변형(CoastalHouseB 등)은 몸통이 같다 — 기본 이름으로.</summary>\n    public static string BaseName(string asset) => asset.StartsWith("CornerShop") ? "CornerShop" : asset.StartsWith("RepairGarage") ? "RepairGarage" : asset.StartsWith("CoastalHouse") ? "CoastalHouse" : asset;\n    static (float w, float d) Body(string asset) => asset == "CornerShop" ? (6f, 5.2f) : asset == "RepairGarage" ? (6.4f, 5.6f) : (5f, 4.8f);'),
    ('    public static void ForBuilding(Adder add, string asset, Vector3 p, Vector3 face, Vector3 right, float s, int hash, char zone, bool rightOpen, bool leftOpen)\n    {\n        var (w, d) = Body(asset);',
     '    public static void ForBuilding(Adder add, string asset, Vector3 p, Vector3 face, Vector3 right, float s, int hash, char zone, bool rightOpen, bool leftOpen)\n    {\n        asset = BaseName(asset);\n        var (w, d) = Body(asset);'),
])
# 3) 설치기: hash 로 변형 고르기 (집 3 / 상점 3 / 정비소 2)
patch(f'{root}/F1/Assets/Editor/CoastalTownInstaller.cs', [
    ('   float scale=asset=="RepairGarage"?.64f:(asset=="CornerShop"?.64f:.62f+hash%3*.035f);\n   Add(asset,p,yaw,scale,hash%3);',
     '   float scale=asset=="RepairGarage"?.64f:(asset=="CornerShop"?.64f:.62f+hash%3*.035f);\n   // 4차(Claude): 건물 변형 — 집 A/B/C(덧문·발코니·빨간 띠), 상점 MART/ICE/TEA, 정비소 열림/셔터. 몸통 치수 동일.\n   string variant=asset=="CoastalHouse"?new[]{"","B","C"}[(hash/7)%3]:asset=="CornerShop"?new[]{"","B","C"}[(hash/11)%3]:new[]{"","B"}[(hash/13)%2];\n   Add(asset+variant,p,yaw,scale,hash%3);'),
])
print('claude4 applied')
