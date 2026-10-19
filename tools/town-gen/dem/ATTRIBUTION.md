# Copernicus DEM GLO-30 — 출처·라이선스 (원문 확인 2026-10-19)

- 데이터: Copernicus DEM GLO-30 Public, 타일 `Copernicus_DSM_COG_10_N40_00_E014_00_DEM.tif` (N40–41, E14–15; 아말피·포지타노 두 상자 모두 포함)
- 받은 곳: AWS Open Data `https://copernicus-dem-30m.s3.amazonaws.com/` (`fetch_sources.py`, 받은 시각은 `.meta.json`)
- 라이선스 원본: `License-COPDEM-30.pdf` (이 폴더, Copernicus Data Space Ecosystem 배포본) — "Licence for the use of the Copernicus WorldDEM-30", 무료·전 세계·기한 없음, 복제·배포·공중 전달·**가공·결합 허용**(Art. 4), 상업 사용 제한 조항 없음.

## 게임에 넣을 표기 (Art. 6)

원본 데이터를 그대로 공개·배포할 때는 6(a) 문구 (원문 그대로):

> © DLR e.V. 2010-2014 and © Airbus Defence and Space GmbH 2014-2018 provided under COPERNICUS by the European Union and ESA; all rights reserved.

우리는 DEM 을 잘라 3단 양자화해 heightMask 로 쓰므로 **가공본 = 6(b)** 문구를 쓴다 (원문 그대로):

> produced using Copernicus WorldDEM-30 © DLR e.V. 2010-2014 and © Airbus Defence and Space GmbH 2014-2018 provided under COPERNICUS by the European Union and ESA; all rights reserved

배포(빌드·공개 리포)하면 6(c) 면책 문장도 라이선스/법적 고지에 함께 넣는다 (원문 그대로, 번역 허용):

> The organisations in charge of the Copernicus programme by law or by delegation do not incur any liability for any use of the Copernicus WorldDEM-30

- 6(d): 공식 후원·보증처럼 보이게 하지 말 것. Copernicus·WorldDEM 로고는 별도 허락 없이 쓰지 않는다(Art. 9).
- 6(e): 우리 가공본을 남이 재배포하게 허락하면 위 의무가 그들에게도 따라가게 한다.
