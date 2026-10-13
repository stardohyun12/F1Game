# asset-gen — 로컬 AI + Blender 로 게임 메시 만들기 (2026-10-12 시작)

> 한 줄: **ComfyUI(모양) → Blender(마감) → Unity**. 각진 물건은 ComfyUI 없이 Blender 스크립트로 바로.

## 왜 이 구조인가

세션 28(2026-05) 결론 그대로다 — "각진 것 = 프로시저럴/Kenney, 유기적 hero 만 AI". 달라진 건
둘 다 **로컬·무료**가 됐다는 것(Meshy 크레딧 불필요):

- ComfyUI 에 **Hunyuan3D-2 노드가 기본 내장** → 이미지 1장 → GLB. (RTX 4070 8GB 에서 돈다)
- **Blender 5.2** 가 `D:\Tools\Blender` 에 있다 → 헤드리스로 깎기·UV·FBX.

## 도구

| 파일 | 하는 일 | 실행 |
|---|---|---|
| `hy3d_run.py` | 이미지 → ComfyUI Hunyuan3D-2 → `<name>_raw.glb` | `python -X utf8 tools/asset-gen/hy3d_run.py --image ref.png --name cake --out out/ --comfy-output C:/Users/rtx40/ComfyUI/output` |
| `blender_lowpoly.py` | 조밀한 메시 → 리메시 → 깎기(목표 tris) → 플랫 셰이딩 → Smart UV → 높이(m) 맞춤 → 바닥 원점 → FBX+GLB | `blender -b -P tools/asset-gen/blender_lowpoly.py -- --in cake_raw.glb --out cake_low.fbx --tris 1500 --height 0.30` |
| `proc_cake.py` | Blender 프리미티브로 직접 짠 케이크 (ComfyUI 없이) | `blender -b -P tools/asset-gen/proc_cake.py -- --out cake_proc.fbx --height 0.30` |
| `blender_render.py` | 모델 N개를 같은 카메라로 렌더 (3/4·측면·흑실루엣) — 도현 판정용 | `blender -b -P tools/asset-gen/blender_render.py -- --out out/compare a.glb b.fbx` |
| `sheet.py` | PNG 들을 라벨 달아 한 장으로 | `python -X utf8 tools/asset-gen/sheet.py --out sheet.png "라벨=a.png" ...` |

`blender` = `D:\Tools\Blender\blender.exe`. ComfyUI 는 `C:\Users\rtx40\ComfyUI` 의 `.venv\Scripts\python.exe main.py --port 8188`
(D:\ComfyUI\Imag_Generate 쪽은 torch 가 없어 못 뜬다). Hunyuan3D 체크포인트는 C 가 꽉 차서
`D:\ComfyUI\Imag_Generate\ComfyUI\models\checkpoints\hunyuan3d-dit-v2_fp16.safetensors` 에 두고
`extra_model_paths.yaml` 의 `f1_hunyuan3d` 항목으로 잇는다.

## 참고 이미지는 Codex 로

`codex exec -s workspace-write -C <빈 폴더> --skip-git-repo-check -` 에 영어 프롬프트를 stdin 으로.
한 장 약 2분. Hunyuan 입력용 조건: **물체 하나 · 흰 배경 · 3/4 위에서 본 각도 · 여백 · 그림자 없음**.
저장한 프롬프트는 `art/_prompts/` 에.

## 실측으로 알게 된 것 (2026-10-12)

- **Meshy 메시가 "깎으면 깨지던" 진짜 이유**: `cake_01.fbx` 는 shell 1개지만 **boundary edge 1,249 · non-manifold 2,723 짜리 열린 표면**이다.
  복셀 리메시는 닫힌 표면을 전제로 해서, 이걸 넣으면 **782 조각**으로 터지고 그걸 깎으니 산산조각.
  (진단 수치: import tris 54,364 → remesh 36,340/782 shells → collapse 1,900 → 재임포트 1,129, 면 40% 증발)
  → `blender_lowpoly.py` 는 입력 열림 여부를 경고하고, 리메시 뒤 부스러기 shell 을 지운다.
  Hunyuan3D 출력(surface net)은 닫힌 표면이라 이 문제가 없을 것으로 예상 — 실측 후 여기 갱신.
- Blender 5.2 헤드리스: FBX import/export, glTF import/export, Workbench 렌더 모두 동작 확인.
