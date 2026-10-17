"""Real isolated runtime HTTP and SQLite with an independent fake editor; no AI."""
import json
import time
import unittest
from urllib.parse import quote
from unittest.mock import patch
import test_goal_start_http as H
from test_unity_console import Editor
from test_unity_status import make_project


class ConsoleHttpTests(unittest.TestCase):
    cleanup = H.GoalStartHttpTests.cleanup
    wait_for = H.GoalStartHttpTests.wait_for
    request = H.GoalStartHttpTests.request

    def setUp(self):
        child = H.CHILD.replace('runtime.serve()', "import unity_status\nunity_status.INSTANCES=folder/'instances'\nruntime.serve()")
        with patch.object(H, 'CHILD', child): H.GoalStartHttpTests.setUp(self)

    def test_console_route_capability_and_cross_project_boundary(self):
        self.assertTrue(self.bootstrap['capabilities']['unityConsole'])
        project = make_project(self.root / 'project' / 'Game')
        editor = Editor(project); self.addCleanup(editor.close)
        instances = self.root / 'instances'; instances.mkdir()
        (instances / '123456789012.json').write_text(json.dumps(dict(state='ready', projectPath=str(project),
            port=editor.port, timestamp=int(time.time()*1000), pid=1234)), encoding='utf-8')
        path = '/api/hub/unity-console?path=' + quote(str(project))
        code, result = self.request(path)
        self.assertEqual(code, 200, result); self.assertEqual(result['entries'], editor.entries)
        self.assertEqual(len(editor.hits), 2)
        self.assertEqual(self.request('/api/hub/unity-console')[0], 400)
        other = self.root / 'other'; other.mkdir()
        code, p = self.request('/api/hub/projects', dict(path=str(other)))
        self.assertEqual(code, 200, p)
        self.assertEqual(self.request(path+'&project='+p['id'])[0], 400)
        self.assertEqual(len(editor.hits), 2)
        self.assertFalse((self.root/'dispatch.jsonl').exists())


if __name__ == '__main__': unittest.main()
