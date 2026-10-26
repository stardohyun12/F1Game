# D-165 Codex 정비소·고카트 v3

원본 모델: garage_v3 / kart_v3 FBX·GLB·blend, 면별 Point PNG. 기존 v1 형상에서 출발하고 부드러운 v2는 사용하지 않았다. v0/v1·Opus 파일 보존. F1/Assets 투입 없음.

## 기준과 변경
제작 전에 ①⑥⑫ 원본과4배 크롭을 직접 관찰했다. 측정표는 검토 폴더 measurement.md. 큰 벽·지붕은 단색에 가깝게, 정보는 허브·그릴·공구·라벨·번호에 집중했다. 기존 반복 줄무늬 제거. 사진·AI·랜덤 노이즈·텍스처 그라데이션 없음. 손픽셀 최대6색/면, Galmuri11(OFL).

## 규격
정비소1,162 tris / 카트796 tris. 1단위=1m. Blender Z-up·앞-Y, 내보내기 Y-up·앞+Z. 바닥 중앙 기준. 카트 축간.95m, 바퀴X±.48m·앞Y-.50/뒤+.45m·Z.17m, 별도 메시. 기존 A와 같은 미터 단위이며 F1 물리/콜라이더를 유지한 외형 대체품은 아니다.
텍셀 밀도 출발값: 정비소16px/m, 카트48px/m(기준 화면 디테일을 위한 D165 적용 가설). 면 크기×밀도를 정수 반올림해1024² 아틀라스에 패킹. 얇은 면 최소1픽셀 예외. 면당1~6색, 큰 단색 면 허용. Closest/GLB Nearest sampler 실제 확인.

## 렌더와 엔진 차이
960×540 실제 렌더 → PIL Nearest 정확히2배1920×1080, 후처리 blur 없음. 근접·6m 컷 별도. 수정판은 단일 평행광과 낮은 표면 emission으로 밝은 오후/유색 그늘을 표현한다. 초기2단 emission과 가짜 평면 그림자는 제거했다. Blender의 실제 투영 그림자를 사용하고 하늘은 카메라에만 보여 표면 샘플 노이즈를 줄였다. 주변 낙엽은 검토 장면 전용이며 모델 FBX에 포함하지 않았다. 실제 Unity 그림자/셰이더 검증은 아니다. 게임과 동일하게 검증된 것은 해상도·확대 조건뿐이며 엔진 머티리얼 재현은 미검증이다.
한글/번호는 작은 손픽셀이므로 원거리 가독성 제한. 내부 기록 판은 정적 질감, 게임 상태 연결 없음.

## 재현·검사
실행: Blender --background --python tools/asset-gen/garage_kart_v3.py, 이후 py -3.13 tools/asset-gen/garage_kart_v3_sheet.py. 전용 check_garage_kart_v3.py로 실제 FBX 재임포트 검사. scale-check.json은8개 렌더의 정확한2배 복원, texture-check.json은면색수·GLB필터 검증. 측정 크롭 좌표 JSON 보존.
참고 스크린샷은 art/reference/dotlp-2026-10-27/SOURCES.md의 공식 Steam 이미지이며 내부 비교판에만 사용. 생성 에셋으로 복사하지 않았다. Unity·Play·충돌·주행·움직임 지글거림은 미실행.
