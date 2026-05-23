# 스튜디오 대시보드 (로컬, 읽기 전용)

AI 팀(에이전트)이 무슨 일을 하는지 **원할 때 브라우저로 여는** 작은 뷰어. 데스크탑 앱 없이.

## 여는 법
```
py tools/studio-dashboard/server.py     # 백그라운드로 띄움
```
→ 브라우저에서 **http://localhost:7878**. 3초마다 자동 갱신. 언제든 새로고침/닫기 OK.

## 무엇을 보여주나
- **PM + 3부서(기획/프로그래밍/아트)** 도면, 각 책상에 팀원·역할·작업·상태(🟢working/✅done/💤idle).
- **세션 라이트**: `~/.claude/sessions/*.json`의 status를 읽어 "🟢 작업 중 / 💤 유휴" 표시(라이브, 단 세션 파일 갱신 주기 따라 약간 지연).

## 데이터 소스 / 한계
- 팀원 명단·상태 = `state.json` (Claude가 에이전트 배정/완료 시 갱신). 즉 "마지막으로 PM이 쓴 상태"가 보임.
- CC의 서브에이전트 출력 파일은 끝나면 비워져서(transient) 직접 못 읽음 → 그래서 state.json 방식.
- 읽기 전용·로컬·단일 사용자. 에이전트 제어·계정·DB 없음(그건 parking된 풀 앱 영역).

## 추적 여부
- 추적: `server.py`, `state.json`, `README.md`
- 무시(.gitignore): `server.log`
