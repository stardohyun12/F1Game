# 03. Asset Opacity

## 증상
AI가 "prefab의 mesh size에 맞춰서 scale 1.5로 설정했어요"라고 자신 있게 짜놨는데 실제 mesh는 0.5×2×0.5라서 비율 다 깨짐. 또는 머티리얼 셰이더가 URP/Lit인 줄 알았는데 Built-in/Standard라서 `_BaseColor` 슬롯이 `_Color`였음.

## 왜 발생 (AI 한계)
메쉬·머티리얼·텍스처·오디오는 binary. AI는 import settings/meta YAML과 파일 이름만 읽음. 실제 bounds, 셰이더 properties, 픽셀, 샘플 데이터 못 봄.

## 회피책
- **즉시**: scale/size 가정 짜기 전에 사용자에게 "mesh local bounds 확인해줘" 또는 코드로 `mesh.bounds.size`를 런타임에 측정.
- **워크플로**: 에셋 의존 코드는 가정 hardcode 금지. `MeshFilter.sharedMesh.bounds.size` 같은 동적 측정 사용.
- **도구**: 엔진 CLI exec로 에셋 props 덤프 (`unity-cli exec "Debug.Log(mesh.bounds.size);"`). 셰이더 property 목록은 `material.GetTexturePropertyNames()`.
- **AI 룰**: 머티리얼 셰이더 property는 추측 금지. `Shader.Find` 한 직후 `shader.GetPropertyCount()` 또는 사용자에게 ShaderLab 파일 보여달라 요청.

## 적용 신호
- "왜 박스가 어긋나죠" / "왜 핑크색이죠" 보고
- 새 에셋 import 직후
- 머티리얼 색·텍스처 코드로 설정 시
- 외부 에셋 팩 (Kenney, Synty 등) 사용 시 — 자체 명명 규칙 다름
