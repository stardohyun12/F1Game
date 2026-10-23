# 사진 → 공용 팔레트 픽셀 패치

Python + Pillow 필요. 사용 전에 실제 사진의 CC0 출처·다운로드 주소·라이선스를 `SOURCES.md`에 적고 `--source-id`로 연결한다. 스크립트는 라이선스를 자동 인증하지 않는다.

```powershell
python tools/asset-gen/photo_to_pixel.py source.jpg patch.png --palette art/source/palette/warm_street.gpl --width-m 0.40 --height-m 0.30 --density 32 --source-id cardboard-cc0 --source-url https://example.org/source --license-url https://creativecommons.org/publicdomain/zero/1.0/ --dither none
```

- 위 예시는 13×10px 패치이며 실측 밀도는 32.5×33.33px/m이다. 사진 전체를 해당 물리 면에 대응시킨다. 크롭은 `--crop LEFT TOP RIGHT BOTTOM`이며 EXIF 방향 보정 후의 픽셀 좌표다.
- `--resample box`(기본) 또는 `lanczos`로 **먼저 축소**한 뒤 최대 32색 공용 팔레트의 최근접 RGB로 양자화한다. 기본 디더 없음, `--dither floyd-steinberg` 선택 가능. 사진보다 큰 패치를 요구하면 실패한다.
- GPL 또는 JSON `{"colors": [[R,G,B], ...]}` 지원. JSON 색은 `"#RRGGBB"`, `{"rgb": [...]}`도 가능하다.
- `--canvas-size 64`는 패치를 64×64 캔버스 좌상단에 배치하고 나머지를 팔레트 첫 색으로 채운다. 패치를 늘리지 않는다. 6면 아틀라스 배치는 별도 모델 제작 스크립트가 담당한다.
- 출력 옆 `.json`에 원본/팔레트/출력 SHA-256, 크롭, 실제 크기, 요청/실제 밀도, 디더·축소 방식을 기록한다. `--metadata PATH`로 경로 지정 가능하다.
- PNG 자체에는 Point 필터를 강제하는 기능이 없다. Blender `Closest`, Unity `Point`로 별도 설정한다. 실제 ±10% 판정은 아틀라스의 UV와 모델 치수를 함께 검사해야 한다.
- 소스 사진의 명암만 보존하며 임의 노이즈·마모를 추가하지 않는다. 디더는 옵션이며 마모 효과가 아니다.

참고: [Pillow Image 공식 문서](https://pillow.readthedocs.io/en/stable/reference/Image.html)의 `resize`, `Resampling` 동작을 재사용했다. 팔레트 선택은 추가 색이 끼지 않도록 직접 최근접 검색한다. 테스트용 합성 이미지는 `%TEMP%/pitcrew-photo-pipeline-*`에만 만들며 실제 에셋의 사진 출처로 사용하지 않는다.
