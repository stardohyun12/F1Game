# PitCrew 리서치 — 사람 손으로 읽히는 아트
2026-10-23 의뢰 / 10-24 확인 · [상세·출처·검증 절차](../../research/2026-10-23-non-ai-art-codex.md)

**핵심 발견 5개**

1. **[확인] 시각 단서의 정량 연구를 찾았다.** 사진 판별은 61.34%·약 62%; 우리 게임의 판별률은 아니다. 빛·배경·매끈함도 오판에 쓰인다. [연구1][R1] [연구2][R2] [단서][R3]
2. **[확인] 미감과 출처 반감은 별개다.** 같은 AI 그림도 인간 라벨에서 가치 평가가 높았다(d=.61); 출처 비공개 때 AI 선호가 나온 연구도 있다. [라벨][R5] [반대 결과][R6]
3. **[추정] ‘사람 손’은 낮은 해상도보다 구체적 선택과 일관성이다.** Sable의 손그림+절차 지형, DUSK의 사진+손질이 근거다. [Sable][G2] [DUSK][G6]
4. **[확인] 두 캡처에는 바다·항구 마을이 보이지 않는다.** [추정] 생활 기척도 약하다. [확인] 감사상 Meshy 형태 4개 외 AI 엔진음 1개도 있고, 화물 대체품은 준비돼 있다. [캡처][L4] [감사][L3]
5. **[확인] Valve 공개 문서는 출시·소비 콘텐츠 중심이다.** [추정] 내부 참고 후 폐기는 제외 해석이 타당하지만, 내부 원화·AI 시각 자산용 코드의 일괄 면제 문구는 못 찾았다. [Valve][V1]

**실행 순서 — 모두 [추정], 상세 보고서 §9와 같은 10개**

| 우선 | 실행 | 근거 |
|---|---|---|
| P0 | ① 실제 게임 기준 2장 ② 준비된 화물 교체·엔진음/의존성 감사 ③ 도착 지점의 바다·아래 마을 시야 | [감사][L3] [전망 설계][G1] |
| P1 | ④ 서늘한 공기+작은 따뜻함 ⑤ 집 앞 생활 장면 ⑥ 들판 군락/빈 공간 ⑦ 화물 구조·글자 ⑧ 벽 하나 사진→보정 시험 | [색][G2] [장소][G5] [단서][R3] [보정][G6] |
| P2 | ⑨ 바람·생활음 ⑩ 출처 비공개 비교→사실인 제작 과정 공개 | [목표][L2] [라벨][R5] |

**[추정] 규칙 수정:** 여름/AI 기준 원화 문구 정리, 32색 검사는 원본 albedo와 최종 화면 구별, ambient 한 색 강제 재검토, 감사는 메시·소리까지. [브리프][L2] [감사][L3]

**[미확인]** PitCrew 플레이어 반응, 7개 게임의 미공개 텍스처/광원 수치, Valve 경계 사례, 교체 후 출시 의존성. [상세 §12](../../research/2026-10-23-non-ai-art-codex.md#12-남은-모르는-것과-조사-한계)  
**[확인] 산출물·검증:** 상세에 연구 원문 8개+초록 1개·게임 7개·논란 5개·캡처 2장 대조와 URL 수록; 문헌/수치/이미지 확인, Unity·플레이 실험·코드/에셋/씬 수정·커밋은 안 함. [상세](../../research/2026-10-23-non-ai-art-codex.md)

[R1]: https://arxiv.org/html/2304.13023v3
[R2]: https://arxiv.org/html/2507.18640v1
[R3]: https://journals.sagepub.com/doi/10.1177/10711813251363209
[R5]: https://link.springer.com/article/10.1186/s41235-023-00499-6
[R6]: https://www.frontiersin.org/journals/psychology/articles/10.3389/fpsyg.2024.1497469/full
[G1]: https://blog.playstation.com/2021/08/05/crafting-a-tiny-open-world-a-look-behind-the-scenes-at-the-creation-of-a-short-hike/
[G2]: https://unity.com/resources/shedworks-sable-modular-design-approach
[G5]: https://www.gamedeveloper.com/design/using-photography-to-document-the-end-of-the-world-in-i-umurangi-generation-i-
[G6]: https://x.com/DUSKdev/status/1204560125236781056
[V1]: https://partner.steamgames.com/doc/gettingstarted/contentsurvey?l=english
[L2]: ../../../art/_briefs/2026-10-23-art-direction-v2.md
[L3]: ../../../art/_review/2026-10-23-ai-audit/AUDIT.md
[L4]: ../../qa/evidence/proto-course-lonehouse.png
