# Car v01 질감 출처

확인일 2026-10-23. Powered by Poly Haven. 게임용 텍스처에 생성 AI 사용 없음.

## 재현과 변경

1. 제공 치수(mm)에 맞춰 1.875m 정사각 원본 영역을 구성했다. 작은 천 원본은 실측 크기를 유지하며 반복했다.
2. 기존 photo_to_pixel.py를 그대로 호출해 BOX 120²·64px/m·공용 32색·디더 없음 기준판을 baseline/에 보존했다.
3. R2 적용판은 사진 면적 평균에 float 7x7 박스 저역 필터를 두 번 적용하고 지정 명도 분위수로 소재별 2~3색 사다리에 대응시켰다. 무작위 노이즈를 추가하지 않았다. 원본 사진 재현 결과와 같은 결과라고 부르지 않는다.
4. 의미가 있는 손 픽셀 층(번호, 봉제, 지퍼, 접힘, 접촉 마모)을 넣고 4px 가장자리 확장 후 512²로 묶었다. 마모 위치/이유는 texture-contract.json 각 타일에 기록했다.
5. 사진은 표면 바탕만 사용했다. 원본 노멀/러프니스/PBR 맵은 사용하지 않았다. 차체 곡면/회전축/조명/UV 밀도는 모델 담당 별도 검사다.
6. 초판 rusty_painted_metal 철판의 세로 무늬가 과해 평판 rusty_metal_02로 교체했다. 8비트 저역 평균은 린넨 분위수 동률 때문에 눈 같은 잔점을 만들어 float 필터로 고쳤다. 탈락 원천/기준판은 보존했으며 최종 atlas에는 쓰지 않았다.

## R3 독립 게이트 대응

- R2의 큰 얼룩은 납작한 카모처럼 읽혔다. 차체·가방·고무·헬멧은 실제 사진의 3x3 float 평균에서 11x11 평균을 뺀 detail-band를 색 사다리에 대응시켜 넓은 얼룩만 억제했다. 무작위 수, 합성 노이즈, 가짜 글자는 사용하지 않았다.
- 도장 주색 #8A4631은 유지하고 #795244 명도 변화 면적을 약 8%로 줄였다. 마모·금속 노출은 지정 하단 접촉부에만 남겼다.
- 천 주색 #B89360은 유지하고 사진 변화는 2%로 줄였다. 크림 #C4A687 짧은 접힘, 봉제, 지퍼로 천 표면을 구분했다.
- 고무 측면은 중앙64,64/반지름18px의 끊긴 몰드 압인 호, 접지면은 위치가 정해진 2px 마모 두 곳, 림은 중앙30px 범위의 눌러 찍은 회색 가장자리를 스크립트 픽셀로 그렸다.
- R2 atlas/시트/계약/스크립트는 r2/에 보존했다. R3 시트에서 직접 확인했으며 최종 모델 조명/크롭 판정은 별도다.

## rough_linen

- 원천: https://polyhaven.com/a/rough_linen
- 저자: {"colormass": "Photography", "Rico Cilliers": "Processing"}
- CC0: https://polyhaven.com/license
- 공개 제공 치수(mm): [270.7081392676629, 271.29998803138733]
- 실사 diffuse: `art/source/car-v01/photos/rough_linen_diff_1k.jpg`
- SHA256: `04897d7611d7390ddf5b2c6f7273f448dfc48341fdbd1f4f207fa30b331d7f57`
- 다운로드: https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/rough_linen/rough_linen_diff_1k.jpg

## rubberized_track

- 원천: https://polyhaven.com/a/rubberized_track
- 저자: {"Charlotte Baglioni": "All"}
- CC0: https://polyhaven.com/license
- 공개 제공 치수(mm): [2000, 2000]
- 실사 diffuse: `art/source/car-v01/photos/rubberized_track_diff_1k.jpg`
- SHA256: `63bbde84facf0ec99d6d4c7a72bcbd2a8fe2dcb910bd65572b600e53e4ac13ea`
- 다운로드: https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/rubberized_track/rubberized_track_diff_1k.jpg

## rusty_metal_02

- 원천: https://polyhaven.com/a/rusty_metal_02
- 저자: {"Rob Tuytel": "All"}
- CC0: https://polyhaven.com/license
- 공개 제공 치수(mm): [1000, 1000]
- 실사 diffuse: `art/source/car-v01/photos/rusty_metal_02_diff_1k.jpg`
- SHA256: `adaf094391518b6afe787a123942110aa88ef521c844f970b124420f5ed5d3d9`
- 다운로드: https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/rusty_metal_02/rusty_metal_02_diff_1k.jpg

## 소재의 한계

Rusty Metal 02는 실제 도장 금속 사진이며 자동차 페인트 실측 샘플은 아니다. 명도 분포만 차체에 옮겼고 사진의 전체 녹 분포는 바랜 도장 2색으로 낮췄다. Rubberized Track은 고무 트랙 사진으로 타이어 재질의 저대비 바탕만 사용했다. Rough Linen은 린넨 사진으로 보온가방의 직물 인상을 위한 바탕이며 실제 단열 가방 촬영본은 아니다.

## 글자

- `07`: Galmuri11 실제 글리프, 11px, 이진 마스크로 팔레트 잉크 인쇄.
- 폰트: `art/_review/2026-10-23-d123/fonts/Galmuri11.ttf`
- OFL-1.1: `art/_review/2026-10-23-d123/fonts/LICENSE.txt`
- 원화는 색 팔레트/방향 참조만. 원화 픽셀을 게임 텍스처로 복사하지 않음.
