# OSM 마을 만들기 — 더 나은 방법 비교 (리서치 2, 2026-10-20)

조사만 했다. 다운로드·설치·코드 없음. 웹 검색 결과와 우리 문서(STRUCTURE-12.md)만 근거. "비용"은 전부 **가설**.

## 지금 방식의 뿌리 문제
길 가운데 선 + 폭 가설로 띠를 만들고, 건물은 따로 압출해서 **서로 모른 채** 앉힌 뒤 겹치면 하나를 빼거나 줄인다. 길과 건물이 서로를 모르니 (a) 겹침 사후 처리 (b) 교차로는 띠끼리 겹쳐서 지저분. 아래 방법은 전부 "서로를 아는 하나의 평면 도형"으로 푸는 쪽이다.

## ① 길 면 = 건물 사이 빈 공간 (여집합)
- **검색에서 확인된 사례: 없음.** "건물 윤곽의 여집합으로 길 면" 을 그대로 쓴 공개 사례는 못 찾았다. 근접한 것만:
  - CARLA Digital Twin 은 반대 방향(길 먼저, 길 사이 빈 땅에 건물이 모양을 맞춤).
  - ArcGIS 문서: 중심선을 대략 폭으로 buffer → 겹친 이웃 도형을 clip.
- 일반 기하 연산으로는 단순하다(Shapely 류 `union` / `difference`): `길면 = (중심선 buffer 합집합 − 건물 합집합)`. 순수 여집합(= 블록 밖 전부)은 필지 뒷마당·바다까지 포함돼 **그대로는 못 쓴다** — 반드시 "중심선 buffer 를 상한 마스크로" 같이 써야 함.
- 효과: 길 위 건물 문제가 구조적으로 사라진다(건물이 길을 파고드는 만큼 길이 깎임). 교차로는 buffer 합집합이 자동으로 하나의 면이 되어 겹침·들쭉날쭉 폭이 줄어든다.
- 위험: ① 건물이 길 가운데까지 나와 있으면 길이 끊김(OSM 건물 오차) → 최소 폭 보장 후 건물을 밀어야 함. ② 폭이 건물에 맞춰 들쭉날쭉(질문의 "폭 들쭉날쭉" 은 오히려 심해질 수 있음) → buffer 폭으로 캡하고 부드럽게(모폴로지 open/close). ③ 결과 폴리곤을 Unity 메시로 삼각분할 + 지형에 맞춰야 함.
- 넣는 비용 가설: 파이썬 전처리(`tools/town-gen/`)에 폴리곤 라이브러리 1개 + 2D 단계 추가 → 1~2일. 현재 파이썬 환경에 shapely **없음**(확인함) → 설치 필요(도현 영역 아님, 일반 pip). Unity 쪽은 면 메시 삼각분할만 필요.

## ② Blender blosm(Blender-OSM) 은 어떻게 맞추나
- 길: **곡선 + 프로파일 객체로 폭을 줌**, 지형이 있으면 길을 지형에 **투영**(projected). 길 면과 건물 사이를 맞추는 로직은 확인 못 함 — 길과 건물은 따로 올리고 지형에 얹는 방식으로 보인다(추정).
- 건물: 지붕 형태(평·맞배·모임 등) 풍부. 프리미엄판은 텍스처·UV·나무 등.
- **라이선스: 애드온 소스 GPL.** GPL 코드를 프로젝트에 쓰면 프로젝트도 GPL 배포해야 한다는 개발자 설명 → **코드 이식은 불가에 가깝다.** 아이디어(지붕 종류 등)만 참고.
- 결론: 우리 문제(길·건물 맞춤)에 대한 해답이 아님. 지붕 다양성 참고 정도.

## ③ 교차로 면 생성 (게임·도구)
- **CityEngine**: 교차로 형태를 **그래프 노드마다** 계산(crossing / junction / roundabout / freeway / smart 자동). 아크 반지름이 노드별 옵션. 노드 둘이 너무 가까우면 한 노드가 이웃 노드 도형 안에 들어가 **충돌**(빨간 점선) — 즉 짧은 구간 문제는 그쪽도 똑같이 겪는다. 학술 비교(arXiv 2103.08275)는 CityEngine 교차로를 "사각형 + 좁은 삼각형 연결" 로 묘사.
- OSM 커뮤니티 토론: ① 길은 그대로 두고 **교차로 위에 아스팔트 폴리곤을 덧씌움**(간단, 길 개수 무관) ② 조합 수가 폭증(중간/끝점 연결, 5갈래 이상) ③ 길을 지형 메시에 **접어 넣는** 방식은 성능·버그로 갈아탄 사례.
- Cities: Skylines / Houdini 내부 알고리즘은 이번 검색에서 **확인 못 함.**
- 우리에게 쓸 만한 핵심 아이디어: "노드마다 교차로 도형 하나를 만들고, 그 도형이 연결된 길 띠의 끝을 잘라 준다" + 코너 둥글리기(아크 반지름). 이건 알고리즘이라 라이선스 없이 직접 구현 가능. 비용 가설 2~3일. 위험: 노드 간격이 가까운 골목 격자에서 충돌(위 CityEngine 사례) → 근접 노드 병합 필요.
- 더 싼 대안: ①의 buffer 합집합이 교차로를 자동으로 면 하나로 만들어 주므로, 별도 교차로 알고리즘 없이 시작 가능(코너가 뾰족한 건 나중에 buffer 의 round join 으로 완화).

## ④ Unity 에서 쓸 수 있는 오픈소스 (검색 범위 내)
- **osm-unity-graph (enisbt)** — MIT. OSM → 2D 노드/길 그래프까지만. 메시·교차로 없음. 우리가 이미 가진 것(OSM 파싱)과 겹침 → 가치 낮음.
- **OSM-2-Unity (raybarrera)** — MIT 라고 적힘, 작자 스스로 "거칠다·오래 갱신 안 됨". 참고용도 약함.
- **Road Architect (MicroGSD)** — MIT, 교차로 처리 좋다는 평, 단 **수동 도로 제작 도구**이고 저장소 보관(archived). OSM 입력 없음. 교차로 메시 생성 코드를 읽어볼 가치는 있음(코드 읽기만, 이식은 도현 승인 후).
- **MarcusElg/roadcreator** — MIT, 수동 도구, archived.
- **OSM2World** — Java 라이브러리/앱, 길(차선·표시)·건물 높이 추정·지형. 라이선스와 교차로 코드는 이번 검색에서 **확인 못 함**(저장소에서 직접 확인 필요; 후속 조사 후보). 언어가 Java 라 Unity 직접 사용 불가, 알고리즘 참고용.
- 결론: **Unity 에서 "OSM → 길·건물 맞춤" 을 이미 해 주는 쓸 만한 오픈소스는 못 찾았다.** 직접 짜는 쪽이 현실적.

## 추천 (3개 이하)

1. **2D 평면 도형으로 통일 (①+③ 합침, 1순위).** 파이썬 쪽에서 중심선 buffer(종류별 폭) 합집합 → 건물 합집합을 뺌 → 최소 폭 점검 → 폴리곤을 Unity 로 내보내 삼각분할 + 지형 투영. 길 위 건물·교차로 겹침을 한 번에. 비용 1~3일 가설. 위험: 건물 오차로 길 끊김, 폭 들쭉날쭉 → 상한 buffer·부드럽게 처리. **먼저 한 구역(중심 400m)만 해서 지금 v12 와 톱다운 나란히 비교**를 권한다.
2. **교차로 노드 도형 + 코너 둥글리기 (③ 아이디어).** 1번의 코너가 마음에 안 들 때만. 근접 노드 병합 선행 필요.
3. **Road Architect 교차로 코드 읽기 (④, 참고만).** MIT 라 읽고 배우는 건 자유, 코드를 프로젝트에 넣는 건 별건(도현 승인).

넣지 말 것: blosm 코드(GPL), osm-unity-graph / OSM-2-Unity(이미 가진 것 또는 너무 거침).

## 확인 못 한 것
- "건물 여집합으로 길 면" 정확 사례, Cities: Skylines/Houdini 내부 알고리즘, OSM2World 라이선스·교차로 코드.

## 출처
- [OSM 도로 절차 생성 (IEEE 2022)](https://ieeexplore.ieee.org/document/9887441)
- [CARLA Digital Twin](https://carla.readthedocs.io/en/0.9.16/adv_digital_twin/)
- [blender-osm (OSM wiki)](https://wiki.openstreetmap.org/wiki/Blender-osm)
- [blender-osm 소스 GitHub 복귀 글](https://www.openstreetmap.org/user/vvoovv/diary/392254)
- [OSM 포럼 — Rendering road intersections](https://community.openstreetmap.org/t/rendering-road-intersections/72309)
- [CityEngine Street and intersection shapes](https://doc.arcgis.com/en/cityengine/latest/help/help-street-and-crossing-shapes.htm)
- [arXiv 2103.08275 대규모 3D 도로망 생성](https://arxiv.org/pdf/2103.08275)
- [osm-unity-graph](https://github.com/enisbt/osm-unity-graph)
- [OSM-2-Unity 포럼](https://discussions.unity.com/t/osm-open-street-map-to-unity-utility/456266)
- [Unity 포럼 Road Creator / Road Architect 논의](https://discussions.unity.com/t/1-11-4-standard-rp-lwrp-free-opensource-road-creator/732368?page=3)
- [Create polygon around roads (buffer+union)](https://discuss.python.org/t/create-polygon-around-roads/24907)
- [ArcGIS 중심선→도로 폴리곤](https://support.esri.com/en-us/knowledge-base/how-to-create-road-polygons-from-centerlines-in-arcgis--000036180)
- [OSM2World FOSDEM 2026](https://fosdem.org/2026/schedule/event/osm2world_3d_rendering_openstreetmap_data)
