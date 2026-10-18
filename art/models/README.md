# art/models — 아트팀(Codex) 3D 모델 소스

모델 하나당 폴더 하나. 엔진 투입물은 여기 두지 않고 `F1/Assets/Model/Custom/<category>/`에 넣는다. 규격·검증은 루트 `PIPELINE.md` §5, 상위 규칙은 `project_rules/asset.md`.

```
art/models/<category>_<name>_v01/
  SPEC.md          용도 · 치수(m) · 폴리 예산 · 피벗 · 레퍼런스 · 팔레트
  generate.py      Blender bpy 스크립트 (또는 generate.cs: Unity Editor 메시 생성)
  preview_top.png  톱다운 프리뷰
  preview_side.png 사이드뷰 프리뷰 (asset.md 두 축 검증)
  NOTES.md         측정값(tris, bounds), 임포트 설정, 알려진 이슈
```

- 생성 스크립트는 **재실행하면 같은 결과**가 나와야 한다. 난수는 시드 고정.
- `.glb`/`.fbx`는 LFS. 소스 스크립트와 SPEC은 일반 텍스트.
- Meshy로 만든 모델은 기존 경로 `F1/Assets/Model/AI/`에 두고, 프롬프트는 `art/_prompts/`에 기록한다.
