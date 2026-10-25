# D141 소품2 독립 기술 검사 계약

- GLB 검사: `tools/asset-gen/check_props2.py`
- FBX 검사: `tools/asset-gen/check_props2_fbx.py`
- 대상: `art/models/bayloop_props2_v01/manifest.json`
- 원본 검사기와 생성기, provenance 원장, Unity 파일은 수정하지 않는다.

## 재사용과 변경

`check_buildings.inspect_asset`를 입력 변경 없이 끝까지 실행한다. 실제 GLB accessor + node world transform, 모든 삼각형의 두 주축 32px/m ±10%, major triangle(면적 ≥0.04m²) 단색 여부, 작은 면 색 표본, 팔레트·불투명·2거듭제곱 텍스처·Point·재질·embedded PNG·bounds·출처 파일 및 다운로드 근거를 그대로 검사한다. `check_asset`의 별도 화물 six-face/64px 정책은 소품에 적용하지 않는다.

원본 `triangle_limit()`의 prop 800 cap과 `prop_budget_declared == 800`만 이번 명시적 정책으로 교체한다. 원본 두 결과와 상한은 보고서 `legacy_800_policy`에 남기고, 실제 측정한 `tris`에 `min(asset.max_tris, manifest.max_tris)`를 적용한다. 두 선언은 반드시 정수 1..1500이어야 한다. manifest를 800으로 위장하거나 타입을 building으로 바꾸지 않으며 monkey patch/동적 source 수정은 없다. 다른 실패는 그대로 실패한다.

실측 GLB 및 FBX bounds에서 x·z 양 끝 합 절댓값 <0.0001m, ymin 절댓값 <0.0001m를 요구한다. `pivot_kind=floor_center`만 허용하므로 벽 부착 소품도 자산 자체 바닥 중심이다. 평면 decal의 높이 0은 허용하지만 삼각형 면적 0이나 UV 퇴화는 기존 밀도 계산에서 통과하지 않는다. bounds·dimensions는 manifest와 대조한다.

FBX는 원본 `check_bayloop_fbx.clear/geometry`와 `vertex_position_audit.compare_positions`를 재사용한다. GLB와 별도 FBX 재임포트, 삼각형 수·전 정점 위치·bounds·미터 크기·루트 원점·바닥 중심·embedded albedo SHA-256·원본 파일 불변을 검사한다. 임시 복사 FBX를 external image search 없이 읽는다. 새 실행기는 FAIL 시 exit 1; Blender 자동화는 `--python-exit-code 1`도 함께 지정한다.

## 실행

```powershell
py tools/asset-gen/check_props2.py --out art/_review/2026-10-25-bayloop-props2/glb-checks.json
& D:/Tools/Blender/blender.exe -b --python-exit-code 1 --python tools/asset-gen/check_props2_fbx.py -- --out art/_review/2026-10-25-bayloop-props2/fbx-checks.json
```

두 실행기 모두 `--manifest`, `--root`, `--ids ID ...` 지원. GLB는 `--provenance` 지원. 중복/없는 ID, 빈 자산 목록, 없는 파일은 실패한다.

## 실제 확인

- Python compile()로 두 파일 문법 확인.
- 경계 시험: 1500 tris 통과, 1501 tris 실패; 다른 밀도 실패 유지; 잘못된 x 중심 실패; zero-height bounds 허용 확인.
- 기존 `bayloop_italy_props_v01/manifest.json` bicycle(764 tris)을 실제 원본 조건으로 검사: 원본 모든 check 값과 wrapper 결과 동일, PASS. 기존 800 조건 기준 결과를 먼저 재현했다.
- Blender 5.2.2에서 동일 bicycle GLB/FBX 실제 재임포트: 764 tris, 13개 검사 PASS, 원본 파일 SHA-256 불변. 초기 공통 모듈 import가 Blender에 없는 Pillow를 요구해 실패했고, GLB 검사 import를 해당 함수 안으로 옮겨 재실행 PASS했다(패키지 설치 없음).
- 이번 소품2 제작 자산의 전체 실측은 파일 생성 후 위 명령으로 수행해야 한다.
- 시각 검수는 `docs/harness/ux-reviewer-prompt.md`의 별도 맥락 없는 스크린샷 판정이다. 이 검사기는 시각 승인이나 Unity 임포트/프리팹·Point 설정 검증을 주장하지 않는다.
