# 배경 건물 첫 판 — D-207 / 도현 승인 필요

8채 한정. 위치·선정 이유는 `docs/map/env-first.md`, 기계 판독 정보는 `art/export/env-first/manifest.json`.

| 항목 | 규격 / 상태 |
|---|---|
| 모델 | 마을집 5·연립 2·부속채 1, 각각 FBX + GLB |
| 좌표 | m. FBX Y-up, 원점은 원본 OSM footprint AABB 바닥 중앙. 원본 월드 x/z/y는 manifest에 보존 |
| 층수 | 기존 분류기의 2~4층은 OSM 실측이 아닌 해시값. 마을집 2~3·연립 4·부속채 1로 명시적 조정; 원본 층수도 보존 |
| 예산 | 첫 판 건물당 12,000 tris 상한. 실제 176~5,734, 합계 24,604. 기존 검사기의 참고 한도 6,000도 전부 충족 |
| 재질 | kit-shared-v2 wall/trim/roof/floor 네 슬롯. 흰 재질색, albedo만. 신생 PNG 0. GLB에는 같은 이미지를 내장 |
| UV | 32px/m, 칸별 반 텍셀 안쪽. 벽/지붕 8m보다 작은 타일로 분할. 긴 창틀은 긴 축을 U에 둠 |
| 지붕 | 원본 다각형. 사각 후보만 박공; 비정형·연립은 평지붕+파라펫, 부속채는 외쪽 지붕. 단순 사각으로 바꿔 씌우지 않음 |
| 이름 | env_<zone>_<osm_id>_v01 루트 + BODY/ROOF/TRIM/GLASS/DOOR_SLOT/SIGN_SLOT |
| 문·간판 | 빈 자리만; 출입 애니메이션·글씨·구멍·인테리어 없음 |
| 적용 | 본 F1·F1Studio 프로젝트에 투입 안 함. QA/프로그래밍 임포트 필요 |

`tools/asset-gen/env_auto.py`가 이 묶음의 생성기이며 Blender 구현은 `env_auto_bpy.py`다. 이번 선정에 없는 상점주택·비탈집·항구창고·단독주택의 고유 부품은 아직 구현하지 않았다. 모든 유형을 완성한 생성기로 부르지 않는다.

재현:

```powershell
py -3.13 -X utf8 tools/asset-gen/env_auto.py all
py -3.13 -X utf8 tools/asset-gen/env_verify.py
```

첫 명령의 현재 종료코드 1은 공용 재질 잔결 47건 실패를 그대로 반환한 것이다. 텍셀·UV·팔레트·기하 검사 성공과 전체 아트 게이트 성공은 다르다. 두 번째 명령은 내보낸 FBX 재수입 기하 검사만 PASS다.

## 참고·재사용·변경·검증 구분

- **직접 확인:** berat-racer의 `Facade.cs` 원문(타입별 스타일·창 칸 폭·창 치수·층 높이 정의)과 MIT LICENSE, revision `ff79911c14930a3d099f61dec2254d638bb061b2`. [원본 코드](https://github.com/Acid3croco/berat-racer/blob/ff79911c14930a3d099f61dec2254d638bb061b2/unity/Assets/Scripts/Facade.cs). 외부 구현을 통째로 복사하지 않았고, 해당 Unity 프로젝트 실행은 안 했다.
- **프로젝트 선행 조사:** `docs/design/ENV-PLAN.md`의 C안·맞벽 억제·사각만 박공 원칙. 기존의 단색 Unity 절차 파사드와 Blender 공용 UV 키트를 비교하고 후자를 표면에 사용했다. 기존 조사에 실린 다른 게임 인터뷰를 이번 작업에서 독립 재검증했다고 주장하지 않는다.
- **원본 조건 재현:** `py -3.13 art/source/kit-shared-v2/verify.py` → 원본 크기·팔레트·타일링·결정성 PASS. 최신 `check_rules --only kit --gate`는 원본부터 FAIL 13건. 두 결과는 검사 대상·기준이 달라 함께 기록했다.
- **재사용:** `kit-shared-v2/APPLY.md`의 지정 칸·32px/m·긴 트림 UV 회전 원칙과 원본 PNG를 그대로 사용. 재료 원출처·CC0 표기는 `art/export/kit-shared-v2/provenance.csv` 기록에 근거하며 개별 원사진 라이선스를 새로 전수 검증하지는 않았다.
- **변경:** 실제 다각형을 면 삼각화해 오목한 터 보존, 원형 외곽을 따라 지붕 분할, 파라펫은 polygon-minus-inset으로 생성. 창칸을 층마다 수직 정렬하고 맞벽은 제외. UV 칸 경계를 넘을 때 면을 분할. 공용 텍스처·팔레트·검사 임계값은 변경하지 않았다.
- **실제 검증:** Blender 5.2.2 LTS에서 생성·렌더·FBX 재수입, 기존 아트 검사 실행. 원본 재생성 8/8 메시/UV 해시 동일·GLB 바이트 동일(`reproducibility.json`). FBX 바이트는 시간 메타데이터 때문에 비교 기준으로 삼지 않았다.
- **미검증:** F1Studio A7, Unity 임포트/셰이더/충돌/실시간 성능, D-209 실제 지형 접지, 카트 이동 중 지글거림, 완성된 1층 생활 동선. Blender 카메라는 기준 집/보관소 높이에 대한 카트 규격 참고이며 실맵 카메라 경로 검증이 아니다.

지도 원본: © OpenStreetMap contributors, ODbL 1.0 + SRTM. 기존 `docs/map/ATTRIBUTION.md`의 표기·파생 데이터 배포 조건을 그대로 따른다. 검토 시트에도 출처를 표시한다.

## 다음 사람에게

`art/_review/2026-10-30-env-first/STUDIO-HANDOFF.md`에 스튜디오 담당자 실행 순서가 있다. 미리보기만 검토하고 적용 승인을 받기 전에는 본 맵에 넣지 않는다. 생활 디테일·접지·빈 1층 벽은 다음 과제다.
