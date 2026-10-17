"""Opt-in cancellation test against an actual provider; denied scratch writes stay absent."""
import sys, tempfile, time, uuid
from hub import Hub, HERE

with tempfile.TemporaryDirectory(prefix='pitcrew-cancel-') as directory:
    h=Hub(directory);provider=sys.argv[1]
    try:
        a=h.save_agent({'name':'Cancel check','provider':provider,'model':'sonnet' if provider=='claude' else '', 'effort':'low','mode':'work'})
        target=HERE/'.runtime'/('cancel-'+uuid.uuid4().hex+'.txt')
        h.send(a['id'],{'text':f'Permission UI test. Use a file editing tool to create exactly this new file with text TEST: {target}. Do not touch other files.'})
        deadline=time.time()+90
        while not h.approvals and a['id'] in h.runs and time.time()<deadline:time.sleep(.1)
        assert h.approvals,'Expected actual permission request'
        print('Actual approval pending; stopping.',flush=True)
        h.stop(a['id'])
        deadline=time.time()+20
        while a['id'] in h.runs and time.time()<deadline:time.sleep(.1)
        assert a['id'] not in h.runs,'Cancellation did not release run'
        assert h.store.agent(a['id'])['status']=='interrupted'
        assert not target.exists(),'Cancelled write ran unexpectedly'
        assert not h.approvals
        print('PASS: cancel pending approval, process finishes, no file written',flush=True)
    finally:
        h.close()
        if not h.runs:h.store.db.close()
