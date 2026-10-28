# Q2 항구 광장 — 도현 승인 필요

QA/프로그래밍 임포트 필요. Unity 실행·F1/Assets 반영 안 함.

|FBX|재질 슬롯 → PNG|실치수 (폭×깊이×높이 m)|tris|
|---|---|---|---|
|plaza_bench|Q2_Props_Albedo → plaza_props.png|1.80×0.628×1.038 / 좌판 약 0.45|588|
|plaza_lamp|Q2_Props_Albedo → plaza_props.png|0.66×0.66×3.65|434|
|plaza_signpost|Q2_Props_Albedo → plaza_props.png; Q2_Sign_Albedo → plaza_sign.png|2.0×0.30×3.46|62|
|plaza_pots|Q2_Props_Albedo → plaza_props.png|1.183×0.829×1.076|924|
|plaza_curb|Q2_Curb_Albedo → plaza_curb.png|1.0×0.24×0.18|12|
|plaza_paving|Q2_Paving_Albedo → plaza_paving.png|4×4×0 평면|2|

모든 FBX: m, Y-up, 원점 = 바닥 중앙. `.fbm` 폴더는 FBX 텍스처 참조용 동봉 사본. 원본 PNG를 재질 BaseMap/_BaseMap(Base Color)에 연결. 현재 Scene_A7_Light 계단 명암 셰이더에 연결하고, albedo에 AO/구운 그늘을 다시 곱하지 않는다. 노멀·AO·emission 맵 없음. `_Sat` ≤1.2, 금속은 무채 숯색.

텍스처: sRGB On, Filter Point, 원본 해상도 유지, 저해상도 압축 금지. 포석/연석 Wrap Repeat, 소품/표지판 Clamp. MipMap은 실제 거리에서 QA 확인(글자 뭉개짐 주의). 128×128 포석 = 4×4m(32 texel/m), 64×32 연석 = 2×1m(32 texel/m), 256² 소품 아틀라스 = 표면 약 32 texel/m, 128² 표지판 = 64 texel/m. 추가 글자 폰트 설치 불필요: 직접 작성한 5×7 비트맵.

광장 바닥 메시에 4m당 UV 1회. 연석 1m 모듈은 x방향 UV 0~1로 2블록을 표시하는 예외(가로 64 texel/m); 세로는 32 texel/m. 연석은 높이 0.18m, 차량 진입부에는 놓지 않는다. 포석은 평면이라 충돌 두께는 QA/프로그래밍이 별도 콜라이더로 설정.

예시 배치(Blender Z-up 좌표, Unity에서는 높이 Z→Y): 벤치(-3.2,0.3,0), 가로등(-4.15,2.75,0), 화분(-2.1,1.25,0), 표지판(2.5,2.3,0). 벤치 Z회전 -0.3rad. 10×8m 바닥의 가장자리만 사용, 중앙 통행 공간·진입부 개방. 바닥 안내 화살표 없음. 표지판 목적지 방향은 실제 연결 길에 맞춰 QA/프로그래밍이 회전/배치해야 함. TEA ↑는 직진 표시의 직사각 판, HARBOR/MART는 좌/우 화살촉 판.

렌더의 해·하늘빛·바다·바깥 지면은 미리보기 문맥이며 내보낸 에셋에 포함하지 않는다. 가로등은 낮 장면의 불 꺼진 유리(불투명 albedo), 실제 야간 조명 기능은 포함하지 않음.
