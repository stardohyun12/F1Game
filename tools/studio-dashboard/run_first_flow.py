"""Bounded real image-input and direct-team trial in its own persistent workspace."""
import base64,json,time
from pathlib import Path
import hub,studio,studio_assets,deliverables

REPO=Path(__file__).resolve().parents[2]
ROOT=REPO/'art'/'pitcrew-first-flow'
DATA=Path(__file__).parent/'.runtime'/'first-flow-data'

def wait(h):
    deadline=time.monotonic()+180
    while h.runs or getattr(h,'post_runs',0):
        if time.monotonic()>deadline:
            for r in list(h.runs.values()):r.stop()
            raise TimeoutError('Trial stopped at 180 seconds')
        time.sleep(.2)

if __name__=='__main__':
    ROOT.mkdir(parents=True,exist_ok=True)
    h=hub.Hub(DATA,ROOT);h.after_run=lambda *args:None
    try:
        studio.setup(h)
        for r in studio.state(h)['rooms'].values():h.store.update(r['agent'],model='gpt-6-astra',effort='low',mode='full')
        studio.action(h,dict(action='goal',text='기존 F1Game 해안 마을·배달 차량을 기준으로 작은 제작 시험. 따뜻한 해안 분위기와 읽기 쉬운 차량, 조작 반응 개선을 비교한다. 시안은 사용자 채택 전까지 실험이다. 기존 게임 파일은 수정하지 않는다.'))
        source=REPO/'art/source/coastal-formula/01_front.png'
        a=studio_assets.upload(h,dict(name=source.name,type='image/png',data=base64.b64encode(source.read_bytes()).decode(),room='art',purpose='기존 차량 형태와 색감 기준'))
        start=time.monotonic()
        studio.submit(h,dict(action='send',room='art',images=[dict(id=a['id'],purpose='이미지에 보이는 차량·배달 상자를 관찰하고 색감 2개를 비교 제안',region=dict(x=.15,y=.2,w=.7,h=.65))],text='첨부된 실제 이미지를 읽으세요. 무엇이 보이는지 구체적으로 설명하고 기존 차량 형태를 유지하는 색감 시안 A/B를 제안하세요. 도구로 raster 이미지를 생성했다고 주장하지 마세요. 두 시안은 Unity에서 실제 소재 복제본으로 렌더링할 예정입니다. 이 폴더에 art-options.json 파일 하나만 작성하세요. 형식 {"observed":"한국어 실제 관찰", "options":[{"id":"A","name":"...","bodyTint":"#RRGGBB","groundTint":"#RRGGBB","reason":"..."},{"id":"B",...}]}. bodyTint는 원본 텍스처에 곱할 tint이므로 너무 어두운 값은 피하세요. JSON을 register_artifact로 등록하세요. 사용자 채택은 아직 없습니다. 추가 에이전트 실행 금지. 다른 파일 수정 금지.'))
        wait(h);print('ART',studio.state(h)['jobs'][-1]['status'],flush=True)
        controller=(REPO/'F1/Assets/Scripts/ArcadeCarController.cs').read_text(encoding='utf-8')
        (ROOT/'ArcadeCarController.reference.txt').write_text(controller,encoding='utf-8')
        studio.submit(h,dict(action='send',room='feel',text='조작감 팀에 직접 요청합니다. ArcadeCarController.reference.txt는 현재 코드의 읽기 전용 복사본입니다. 조향 반응을 조금 더 빠르게 시험할 최소 변경 1개를 제안하세요. 값은 기존 steerLag 0.025초에서 0.015초로 비교하는 가설을 검토하세요. 코드 전체의 입력과 물리 적용을 읽고 기대 효과·부작용·동일 조건 검증·카메라/애니메이션 영향·원복 조건을 feel-experiment.json에 작성하세요. 기존 기준과 제안을 구분하며 사용자 플레이 검증을 했다고 쓰지 마세요. 파일을 register_artifact로 등록하세요. 다른 파일 수정이나 에이전트 실행 금지.'))
        wait(h)
        report=dict(seconds=round(time.monotonic()-start,2),jobs=studio.state(h)['jobs'],artifacts=deliverables.listing(h),models=[h.store.agent(r['agent']).get('actualModel') for r in studio.state(h)['rooms'].values()],userAdoption=False,userPlayFeedback=False)
        (ROOT/'real-model-evidence.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
        print(json.dumps(report,ensure_ascii=False),flush=True)
    finally:h.close();h.store.db.close()
