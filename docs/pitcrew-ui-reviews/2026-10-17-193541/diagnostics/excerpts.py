from pathlib import Path
import sqlite3,json,datetime,re
OUT=Path(__file__).resolve().parents[1];ROOT=OUT.parents[2]
db=ROOT/'tools/studio-dashboard/.runtime/hub.sqlite3'
c=sqlite3.connect(db.as_uri()+'?mode=ro',uri=True)
rows=c.execute("SELECT seq,id,agent,body FROM messages WHERE body LIKE '%WORKFLOW_HANDOFF_BUNDLE.zip%' OR body LIKE '%인계 ZIP%' OR body LIKE '%링크 주삼%' OR body LIKE '%걍 파일 경로%' ORDER BY seq DESC LIMIT 100").fetchall()
out=[]
for seq,mid,aid,body in reversed(rows):
    m=json.loads(body)
    if m.get('role') not in ('user','assistant'):continue
    text=m.get('text','')
    # The user-context prefix repeats old chats: never export those embedded snapshots.
    if '[사용자 메시지]' in text:text=text.rsplit('[사용자 메시지]',1)[-1].strip()
    if len(text)>2500:continue
    if not any(v in text for v in ['WORKFLOW_HANDOFF_BUNDLE.zip','인계 ZIP','링크 주삼','걍 파일 경로','PC 내부 경로','F1Game_WORKFLOW_HANDOFF']):continue
    text=re.sub(r'(?i)[A-Z]:[\\/]Users[\\/][^\\/\s]+','<USER_HOME>',text)
    out.append(dict(source='tools/studio-dashboard/.runtime/hub.sqlite3 (read-only; DB not copied)',message_seq=seq,message_id=mid,role=m['role'],ts=m.get('ts'),text=text))
    # Agent ID used only for selecting this conversation in the local diagnostic browser, not exported.
    (OUT/'diagnostics/target-agent.local').write_text(aid,encoding='utf-8')
c.close()
(OUT/'evidence/download-conversation-excerpts.json').write_text(json.dumps(out[-14:],ensure_ascii=False,indent=2),encoding='utf-8')
print('Selected narrowly relevant messages:',len(out[-14:]))
