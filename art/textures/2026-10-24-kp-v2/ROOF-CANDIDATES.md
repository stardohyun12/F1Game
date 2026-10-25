# A1 2차 기와 사진 후보 비교

확인일: 2026-10-24. **Powered by Poly Haven**.

기존 `clay_roof_tiles`를 재착색하지 않고 다른 사진 재료 3종을 공식 API의 직접 diffuse JPG 주소에서 내려받았다. 3종 모두 공급자 MD5 일치, SHA-256을 `source_roof_candidates.json`에 기록했다. AI 생성·이미지 편집 모델 사용 0.

![3종 비교](sources/roof-candidates/roof-candidates-compare.png)

| 사진 후보 | 공급자 실측 범위 | 64px/m에서 본 구조 | 판단 |
|---|---:|---|---|
| [Ceramic Roof 01](https://polyhaven.com/a/ceramic_roof_01), Rob Tuytel | 3.5 × 3.5m | 1m 크롭 안 3단가량의 가로 겹침, 검은 하단 접합부, 기와마다 굽은 밝은 중심과 어두운 측면이 남음 | **우선 추천**. 벽 판재와 구별되는 가로 겹침이 가장 안정적. 다만 갈색 도기이며 일본 전통 기와로 촬영한 사진이라는 증거는 없음. |
| [Clay Roof Tiles 02](https://polyhaven.com/a/clay_roof_tiles_02), Amal Kumar | 2.5 × 2.5m | 세로로 긴 조각과 짧은 경계가 남으나, 공용 팔레트 축소 후 세로 판재·벽돌처럼 읽힐 우려 | 탈락. 색을 여러 번 수정해 구조 문제를 숨기지 않음. |
| [Grey Roof Tiles](https://polyhaven.com/a/grey_roof_tiles), Rob Tuytel | 3 × 3m | 1m 크롭 안 넓은 가로 겹침 3단과 작은 모서리 탈락이 남음 | 회색 지붕 대안. Ceramic보다 곡면 단서가 약해 평평한 슬레이트로 읽힐 수 있음. |

## 동일 조건 시험

공식 diffuse 1024² JPG → 공급자 mm 실측으로 **정중앙 1m² 크롭** → BOX 64² 축소 → 기존 `photo_to_pixel.py`의 공용 `pitcrew_d123_32` 정확 최근접 RGB 양자화, 디더 없음 → 4배 Nearest 표시. 화이트밸런스·색조·노출 보정 **없음**. 원본 전체 256² 문맥, 실제 1m 크롭 축소, 공용 팔레트 결과를 나란히 놓았다.

정수 크롭 오차는 Ceramic 293px(실제 약 1.0015m), Clay02 410px(약 1.0010m), Grey 341px(약 0.9990m)로 0.15% 이내다. 64px/m 판정 허용오차 안이다. 이 후보 파일은 구조 선택용이며 임의 1m 크롭 자체의 좌우·상하 연결을 무조건 seamless라고 주장하지 않는다. 최종 타일은 반복 경계 확인이 별도로 필요하다.

## 출처와 확인 한계

- [공식 라이선스](https://polyhaven.com/license): 다운로드한 텍스처 자산은 CC0. 웹사이트 미리보기 렌더를 복사한 것이 아니다.
- [공식 FAQ](https://docs.polyhaven.com/en/faq): texture material은 사진 기반 작업으로 만들며 절차 생성·생성 AI를 쓰지 않는다고 공급자가 명시한다. 대부분 photogrammetry 또는 photometric stereo라고 설명하지만 **이 세 자산 개별 방식은 공개하지 않아 단정하지 않는다**.
- [공식 texture 기술 규격](https://docs.polyhaven.com/en/technical-standards/textures): 사진 기반 소스와 실측 크기를 요구한다. 공급자 실측의 ±5% 정확도 정책은 전체 제작 기준이며 이번에 현물을 재측정한 것은 아니다.
- [API 이용 조건](https://github.com/Poly-Haven/Public-API/blob/master/README.md): API 사용 크레딧 표기. 각 `info.json`·`files.json`, 직접 JPG와 해시를 함께 보관했다.

다운로드·검증·동일 조건 비교는 실행했다. Unity 임포트·실제 이동 화면 검사·새 기와 게임 배치는 실행하지 않았다.
