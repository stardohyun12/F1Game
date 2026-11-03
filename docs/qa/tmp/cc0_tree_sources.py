import json, io
P = "D:/Github/F1Game/art/source/photo-picks/"
prov = json.load(open(P + "T/prov_T.json", encoding="utf-8"))
L = ["", "## T — 나무 후보 (artlive 20261031-0142)", "",
     "나무 텍스처 비교용 종별 A/B 후보 4종×(잎+껍질). Commons 는 파일 페이지 API(extmetadata LicenseShortName)로 CC0/Public domain 재확인, Poly Haven 은 전 에셋 CC0. 긴 변 1600px 이하. 대체: 곰솔 B 잎=Pinus nigra(유럽곰솔), 팽나무 B 잎=Zelkova carpinifolia(느티나무속), 동백 B=올리브(Olea europaea) 잎, 동백 A 잎은 꽃 제외 상단 크롭(Camellia japonica), 사이프러스 B 잎은 곤충 제외 하단 크롭. 껍질은 전부 Poly Haven(곰솔 껍질 2종은 Pinus 일반, 팽나무=Chinese/Japanese hackberry bark, 동백=매끈한 대체 sakura/platanus, 사이프러스=cedar 섬유질 대체). `T/sheet_T.png`·`T/prov_T.json`.", "",
     "| 번호 | 파일 | 출처 | 라이선스 원문 | 촬영자/작성자 | 저장 크기 |", "|---|---|---|---|---|---|"]
for r in prov:
    n = "T:" + r["file"][2:-4]; note = (" (" + r["note"] + ")") if r.get("note") else ""
    L.append(f'| {n} | `T/{r["file"]}`{note} | {r["source_site"]}: {r["page_url"]} | {r["license"]} | {r["author"]} | {r["size"].replace("x", "×")} |')
open(P + "SOURCES.md", "a", encoding="utf-8").write("\n".join(L) + "\n")
print(len(prov))
