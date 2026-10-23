# 상자 글꼴

- Galmuri7 v2.40.4, Lee Minseo (quiple).
- 원본: 기존 검증 보관 ZIP `art/_review/2026-10-23-d123/fonts/Galmuri-v2.40.4.zip`에서 `Galmuri7.ttf`, `LICENSE.txt` 그대로 추출. 글꼴 수정 없음.
- 공식 저장소: https://github.com/quiple/galmuri
- 공식 README: https://github.com/quiple/galmuri/blob/main/README.md
- README의 기본 격자: **Galmuri7 (8px, 6pt)**. Pillow에서 8px로 직접 rasterize하고 축소하지 않는다.
- 라이선스: SIL Open Font License 1.1, 동봉 `LICENSE.txt` 참조. CC0 사진과 별개인 OFL 글꼴이다.
- Galmuri7의 한글 지원은 일부 음절에 한정되므로 사용 글리프는 실제 렌더 확인이 필요하다. 이 상자는 송장 숫자 `1`만 사용한다.
- 2026-10-23 실제 Pillow 확인: size=8에서 `1` bbox=(0,1,4,8), advance=4px, 마스크 값={0,255}; 1:1 4×7px이므로 13×10px 앞면 안에 배치 가능.
- TTF SHA256: `c845c6bf6f7236ed370aed4a8f8d9b4f25e6a18f63515777fe4153d287c32917`.
