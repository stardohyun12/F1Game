# 목조 외벽 원본 — 2026-10-24

- 채택: [Wood Plank Wall](https://polyhaven.com/a/wood_plank_wall), Dimitrios Savva, [CC0](https://polyhaven.com/license).
- 직접 받은 원본: `wood_plank_wall_diff_1k.png` (1024×1024, 제공자 실측 1000×1000mm). 변환하지 않은 다운로드 바이트 그대로다.
- `wood_plank_wall_info.json`, `wood_plank_wall_files.json`은 공식 API 응답. 제공자 MD5와 받은 파일 MD5 일치; SHA256은 `../../source_wood.json` 참조.
- 제작 출처: [공식 FAQ](https://docs.polyhaven.com/en/faq#-do-you-use-photogrammetryphotoscanning)는 모든 질감이 사진 기반이며 절차적 생성 및 생성형 AI를 사용하지 않는다고 설명한다. [제작 규격](https://docs.polyhaven.com/en/technical-standards/textures)은 photogrammetry / photometric stereo를 허용하며 실측 치수와 보정된 diffuse를 요구한다. 개별 자산이 두 스캔 방식 중 무엇인지는 API에 없으므로 단정하지 않는다.
- 직접 시각 확인: 짙게 바랜 목재, 판재 경계, 못, 옹이가 원본에 있다. 현재 가로결이다. 세로 판재 벽 용도로 90° 회전하면 전체 1m를 64px로 축소할 수 있다. 새 무늬·노이즈 생성은 필요 없다.
- 원본 촬영 장소는 확인되지 않았다. '교토에서 촬영'이 아니라 '교토 목조 외벽을 위한 재료 선택'이다.
- 기존 LofiAtlas `wood` 4번 후보. 단 이 칸은 문틀·의자·전봇대에도 공유되므로 전역 교체 시 판재 줄눈이 작은 소품에 생긴다. 실제 목조 외벽 적용은 독립 머티리얼 시험 뒤 결정해야 한다.

## 밀도 주의

`tools/asset-gen/lofi_pilot.py`의 64px 칸은 UV 2..62, 즉 실사용 60px다. 64px/m이면 최대 물리 폭은 `60/64 = 0.9375m`; 현재 2m 단위 분할은 2m 면에서 30px/m다. 새 PNG만 덮어써도 기존 UV의 밀도는 바뀌지 않는다. 전용 64px Repeat 텍스처는 1m 반복, 기존 아틀라스는 UV/분할을 프로그래밍팀이 별도 조정해야 한다.
