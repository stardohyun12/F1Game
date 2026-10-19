# D-103 축척 비교 시트

`index.html`을 브라우저에서 연다. 3개 시점 선택, 1×/4× 확대, 흑실루엣 전환. 1×는 창 너비에 맞춘 표시 배율이며 원본 픽셀 1:1을 뜻하지 않는다.

## 산출물과 상태

- `index.html`: FINAL — 디렉터 검토용 HTML. 축척 선택·게임 적용은 미승인.
- `drive_064.png`, `drive_100.png`, `side_064.png`, `side_100.png`, `overview_064.png`, `overview_100.png`: FINAL — 비교용 Unity 캡처.
- 위 6개 이름에 `_silhouette`가 붙은 PNG 6개: FINAL — 검정 재질로 재촬영한 비교 캡처.
- 사람 도형: PLACEHOLDER — 임시 씬 내 1.75m 기준체. 엔진 투입 에셋을 생성하지 않았으므로 별도 에셋 파일 없음.
- `validation.txt`: Unity가 기록한 실제 차량·건물 경계 측정 및 고정 설계 치수.
- `lint.cjs`, `lint-results.txt`: PNG 규격·링크·UI 이벤트 로직 검사.

## 참고·재사용·변경

내부 선행 사례 `art/source/street-furniture/capture-real-scale.cs.txt`, `REAL_SCALE.md`, `art/source/coastal-town/scale_064_vs_100.png`를 읽었다. 기존 캡처에서 Unity의 별도 additive scene + RenderTexture + finally 정리 방식을 재사용했다. 외부 자산 다운로드나 새 라이선스 도입 없음.

원본 4.885m 검토를 그대로 재현했다고 주장하지 않는다. 차량 메시 연장을 제거하여 실제 3.785m 경계를 검증했고, 사람은 1.70→1.75m로 변경했다. 건물 앞면을 고정하여 0.64/1.0을 비교하고, 벤치와 사람이 측면에서 차에 가리지 않게 배치했다. 도로는 6m, 양쪽 보도는 2.2/1.8m 설계값이다. 시각적 비율 검토이지 도로·건축 규정 검증이 아니다.

메인 맵 동일 위치 촬영이 아니라 실제 프리팹을 쓴 동일 임시 비교 거리다. 메인 맵 기존 비교 자료를 HTML에 별도 연결했다. 건물 옆에 붙어 있는 진열대 등 프리팹 자식은 건물과 함께 축소되며 독립된 벤치·쓰레기통·차·사람은 고정된다. 건물 중심 x 간격 7m, 앞면 z=-0.1m. 변경안에서 깊이가 바뀌므로 중심 z는 달라진다.

D-102의 정지 운전 offset·lookHeight, FollowCamera의 baseFov=50을 재현했다. Play/HUD/속도별 FOV·흔들림·후처리 비교는 하지 않았다. 4.885m 검토안은 링크로 구분했다.

## 재현

에디터 토큰 보유자만 저장된 ComplexTrack 편집 모드에서 실행한다.

```powershell
py art/_compare/2026-10-19-scale/prepare.py
py -X utf8 art/_compare/2026-10-19-scale/invoke.py art/_compare/2026-10-19-scale/capture.cs.txt
node art/_compare/2026-10-19-scale/lint.cjs
```

`invoke.py`는 기존 `scripts/ucli-http.sh`와 같은 Unity connector exec API를 사용하며 코드를 파일에서 읽어 셸 인용 손실을 피한다. PowerShell Invoke-RestMethod 경로는 응답 대기로 중단했고, Python 표준 HTTP 경로로 성공했다. 실패한 임시 쿼리의 컴파일 오류는 게임 C# 컴파일 오류가 아니며 최종 콘솔 error/warning은 별도로 확인한다.

원본 씬·프리팹·재질 에셋·BuildingScale 상수를 저장하거나 수정하지 않는다. 임시 씬·재질·렌더 타깃을 정리하며 최종 dirty=False, Play=False를 확인한다. 웹 브라우저 도구가 제공되지 않아 HTML 실제 브라우저 렌더·클릭은 미검증이다. PNG는 직접 열어 형태·배치를 확인했고 JS 이벤트는 Node DOM 대역으로 검사했다.

## 남은 판단

도현이 축척 A/B를 선택해야 한다. 실제 마을에 적용할 경우 건물 간격·배달 지점·도로 연결·운전 중 시야는 별도 검증해야 한다.
