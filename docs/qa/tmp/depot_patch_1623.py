"""artlive 1623: q3-depot assemble.py 패치 — 아이콘 간판·셔터 올림·사람 문 열림·내부(천장·카운터·벽 선반·손수레·테이프·시계·작업복)·CamIn 빈 객체·렌더 생략."""
from pathlib import Path
p = Path(r'D:/Github/F1Game/art/source/q3-depot/assemble.py'); s = p.read_text(encoding='utf-8')
def rep(a, b):
    global s
    assert s.count(a) == 1, a
    s = s.replace(a, b)
rep("box('front_door_bay',(3.66,.12,1.8),(2.28,.24,3.6),'wall_cream')",
    "box('front_door_bay',(2.92,.12,1.8),(.80,.24,3.6),'wall_cream')  # 20261028-1623: 문칸을 세 조각으로 → 실제 구멍\nbox('front_door_bay',(4.52,.12,1.8),(.56,.24,3.6),'wall_cream')\nbox('front_door_bay',(3.78,.12,2.95),(.92,.24,1.30),'wall_cream')")
rep("box('half_open_shutter',(-2.1,-.04,2.20),(3.80,.06,1.40),'shutter')", "box('half_open_shutter',(-2.1,-.04,2.80),(3.80,.06,.20),'shutter')  # 1623: 거의 다 올라감")
rep("box('shutter_bottom_rail',(-2.1,-.10,1.50),(3.88,.12,.08),'gutter')", "box('shutter_bottom_rail',(-2.1,-.10,2.70),(3.88,.12,.08),'gutter')")
rep("box('person_door',(3.78,-.035,1.18),(.92,.07,2.12),'wood_door')", "DOOR=box('person_door',(3.78,.21,1.18),(.92,.05,2.12),'wood_door')  # 1623: 안쪽으로 열림(아래에서 돌림)")
rep("box('door_handle',(4.07,-.20,1.15),(.07,.06,.15),'gutter')", "DOOR+=box('person_door_handle',(4.10,.16,1.15),(.07,.05,.15),'gutter')")
rep("box('sign_back',(2.35,-.15,3.08),(4.0,.17,.64),'wood_door')", "box('sign_back',(2.35,-.15,3.10),(.96,.12,.92),'wood_door')  # 1623: 글자 대신 택배 아이콘 판")
rep("face('sign_blank',[(.40,-.245,2.82),(4.30,-.245,2.82),(4.30,-.245,3.34),(.40,-.245,3.34)],'sign_cream',[(0,0),(1,0),(1,1),(0,1)])",
    "face('sign_icon',[(1.93,-.215,2.68),(2.77,-.215,2.68),(2.77,-.215,3.52),(1.93,-.215,3.52)],'parcel',[(0,0),(1,0),(1,1),(0,1)])")
rep("lettering('BAY PARCEL',(2.35,-.257,2.91),3.40)", "# 1623: 'BAY PARCEL' 글자 삭제(도현: 이름 써두지 말고 아이콘만)")
rep("table('packing_table',3.75,-1.3,1.8,.85,.88)\nparcel('egg_delivery_box',3.35,-1.3,.93,.55,.42,.24)\nparcel('packing_box',4.10,-1.3,.93,.45,.36,.32)\ntable('inside_workbench',2.10,3.4,2.2,.9,.90)\nparcel('sort_box',1.4,3.4,.95,.62,.50,.40)\nparcel('sort_box_small',2.3,3.4,.95,.42,.35,.28)",
    "table('packing_table',1.62,.78,1.7,.8,.88)  # 1623: 계란 포장대는 안쪽 창가\nparcel('egg_delivery_box',1.25,.78,.93,.55,.42,.24)\nparcel('packing_box',1.98,.78,.93,.45,.36,.32)")
a = s.index("# Shelves located close enough"); b = s.index("table('packing_table'")
s = s[:a] + "# 1623: 가운데 선반 대신 뒷벽 선반 3줄(아래 INTERIOR)\n" + s[b:]
for o, n in [("wheel('handcart_wheel',xx,-1.03,.18)", "wheel('handcart_wheel',xx+3.4,1.27,.18)"),
             ("box('handcart_toe',(-4.28,-.95,.25)", "box('handcart_toe',(-.88,1.35,.25)"),
             ("box('handcart_upright',(xx,-.77,.76)", "box('handcart_upright',(xx+3.4,1.53,.76)"),
             ("box('handcart_handle',(-4.28,-.77,1.34)", "box('handcart_handle',(-.88,1.53,1.34)"),
             ("parcel('handcart_load',-4.28,-1.02,.28,", "parcel('handcart_load',-.88,1.28,.28,")]:
    rep(o, n)
INT = (Path(__file__).with_name('depot_interior_1623.txt')).read_text(encoding='utf-8')
rep("# Correct all flat", INT + "\n# Correct all flat")
rep("for o in asset:o.parent=root", "for o in asset:o.parent=root\nfor nm,loc in [('STUDIO_CamIn',(3.78,2.0,1.5)),('STUDIO_CamInLook',(-1.8,6.0,1.1)),('STUDIO_Lamp',(0,3.3,3.25))]:\n e=bpy.data.objects.new(nm,None);bpy.context.collection.objects.link(e);e.location=loc;e.parent=root")
rep("# Preview only ground and lighting. Not exported.", "import os\nif os.environ.get('NO_RENDER'):print('DEPOT BUILT (no render)',flush=True);bpy.ops.wm.quit_blender()\n# Preview only ground and lighting. Not exported.")
p.write_text(s, encoding='utf-8'); print('ok')
