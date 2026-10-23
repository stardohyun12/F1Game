# D-123 픽셀 글꼴 출처 / 2026-10-23

시안 채택: **Galmuri11 Regular v2.40.4**, 제작 Lee Minseo(quiple). 원본을 수정하지 않았다.

| 항목 | 확인 |
|---|---|
| 공식 배포 | https://github.com/quiple/galmuri/releases/tag/v2.40.4 |
| 다운로드 | https://github.com/quiple/galmuri/releases/download/v2.40.4/Galmuri-v2.40.4.zip |
| 원본 보존 | `Galmuri-v2.40.4.zip` |
| 시안 사용 파일 | `Galmuri11.ttf`, 웹 미리보기용 `Galmuri11.woff2` |
| 라이선스 | 같은 ZIP의 `LICENSE.txt` 원문: SIL Open Font License 1.1, Copyright (c) 2019–2025 Lee Minseo |
| TTF SHA256 | `E24256F42E43713D2EA086A1E1669D78B968F5B3CC547E5C157F0606FFA5DEF1` |

OFL 원문 확인: 폰트 단독 판매 금지, 번들/재배포 시 저작권 고지와 라이선스 동봉, 수정본 이름은 Reserved Font Name 조건 확인. 폰트로 만든 PNG 자체에는 OFL 적용을 요구하지 않는다. 이번에는 시안 폴더에만 저장했고 Unity/게임 적용은 하지 않았다.

## 후보 비교 / 실제 확인과 판단

- [Galmuri 공식 README](https://github.com/quiple/galmuri/blob/main/README.md): Galmuri11의 표기 기준은 **12px(9pt)**이고 완성형 한글을 지원한다. 이름의 11을 CSS 11px로 해석하지 않는다. 해당 표는 제작자 안내이며, 실제 파일은 위 공식 ZIP에서 확보했다.
- [둥근모꼴+Fixedsys 제작자 배포](https://cactus.tistory.com/193): 제작자는 퍼블릭 도메인이라고 명시하며 16px에 맞춰 도안 위치를 조정했다고 설명한다. **DungGeunMo는 OFL이라고 부르지 않는다.** 파일은 이번 시안에 내려받지 않았다.
- [Neo둥근모 공식 저장소](https://github.com/neodgm/neodgm/blob/main/README.en.md): 별도 파생 프로젝트이며 OFL 1.1 배포다. 원래 DungGeunMo와 라이선스를 섞지 않는다.

판단: 작은 주행 HUD에는 기본 단위가 작은 Galmuri11을 우선 사용한다. 기준 12px, 두 배 24px, 세 배 36px로 배치하고 정수 좌표를 사용한다. 정수배 확대는 본 시안의 픽셀 보존 방안이며, 공식 README가 모든 엔진에서 렌더 결과를 보증한다는 뜻은 아니다. 11/22px는 채택하지 않는다. 텍스트만 낮은 해상도에 그린 뒤 nearest-neighbor로 확대하면 윤곽을 유지할 수 있다.

실제 검증: Pillow의 `ImageFont.truetype`으로 TTF 12/24px 로드 성공. 게임 UI 문장 전체 글리프 커버리지, Unity TMP 아틀라스/필터링, 실제 주행 가독성은 후속 QA/프로그래밍 검증 필요.
