"""Compile the exact shipped C# body against a fake Unity console, without opening Unity."""
from pathlib import Path
import subprocess
import tempfile
import unittest
import unity_console as C

FAKE = r'''
using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine { public static class Application { public static string dataPath="X:/Game/Assets"; } }
namespace UnityEditor {
 public class EditorApplication { }
 public class LogEntry { public int mode; public string message; }
 public static class LogEntries {
  public static int consoleFlags { get; set; }
  public static string filter="";
  public static List<LogEntry> all=new List<LogEntry>(), visible;
  public static bool readFail, endFail, ignoreFilters, restoreFail;
  public static int starts, ends;
  public static string GetFilteringText() { return filter; }
  public static void SetFilteringText(string value) { if (restoreFail && value=="old-filter") throw new Exception("restore failed"); filter=value; }
  public static int StartGettingEntries() {
   starts++; visible=all.Where(r=>ignoreFilters ||
    ((((r.mode & 1)!=0 && (consoleFlags & 512)!=0) || ((r.mode & 512)!=0 && (consoleFlags & 256)!=0)) &&
    (filter=="" || r.message.Contains(filter)))).ToList();
   if ((consoleFlags & 1)!=0) visible=visible.GroupBy(r=>r.message).Select(g=>g.First()).ToList();
   return visible.Count;
  }
  public static void EndGettingEntries() { ends++; if(endFail)throw new Exception("end failed"); }
  public static int GetCount() { return visible.Count; }
  public static bool GetEntryInternal(int index, LogEntry row) {
   if(readFail)throw new System.Reflection.TargetInvocationException(new InvalidOperationException("read failed")); var source=visible[index];row.mode=source.mode;row.message=source.message;return true;
  }
 }
}
public static class Probe { public static object Execute() { BODY } }
public static class Program {
 static void Check(bool value,string name) { if(!value)throw new Exception(name); }
 static void Reset() {
  UnityEditor.LogEntries.all.Clear();UnityEditor.LogEntries.consoleFlags=131;UnityEditor.LogEntries.filter="old-filter";
  UnityEditor.LogEntries.readFail=false;UnityEditor.LogEntries.endFail=false;UnityEditor.LogEntries.restoreFail=false;
  UnityEditor.LogEntries.ignoreFilters=false;UnityEditor.LogEntries.starts=0;UnityEditor.LogEntries.ends=0;
 }
 static Dictionary<string,object> Read() { return (Dictionary<string,object>)Probe.Execute(); }
 static string ConnectorFailure() {
  // Match the installed connector: unwrap the Execute invocation only once.
  try { typeof(Probe).GetMethod("Execute").Invoke(null,null); }
  catch(System.Reflection.TargetInvocationException e) { return (e.InnerException ?? e).Message; }
  throw new Exception("failure unexpectedly succeeded");
 }
 static void Restored() {
  Check(UnityEditor.LogEntries.consoleFlags==131 && UnityEditor.LogEntries.filter=="old-filter","settings restored");
  Check(UnityEditor.LogEntries.starts==1 && UnityEditor.LogEntries.ends==1,"paired start/end");
 }
 public static int Main() {
  try {
   Reset(); for(int i=0;i<80;i++)UnityEditor.LogEntries.all.Add(new UnityEditor.LogEntry{mode=512,message="warning-"+i});
   UnityEditor.LogEntries.all.Add(new UnityEditor.LogEntry{mode=1,message="newest-error"});
   var data=Read();var rows=(List<string>)data["entries"];
   Check(rows.Count==50 && rows[0]=="[Error] newest-error" && rows[49]=="[Warning] warning-31","newest 50");
   Check((bool)data["hasEarlier"] && !(bool)data["scanLimited"],"older rows disclosed");Restored();
   Reset();for(int i=0;i<50;i++)UnityEditor.LogEntries.all.Add(new UnityEditor.LogEntry{mode=1,message="same"});
   data=Read();Check(((List<string>)data["entries"]).Count==50 && !(bool)data["hasEarlier"],"collapse disabled, exact 50");Restored();
   Reset(); data=Read();Check(((List<string>)data["entries"]).Count==0 && !(bool)data["hasEarlier"],"empty");Restored();
   Reset();UnityEditor.LogEntries.all.Add(new UnityEditor.LogEntry{mode=1,message=new string('x',9000)});
   data=Read();Check(((List<string>)data["entries"])[0].Length==4000 && (bool)data["textTruncated"],"bounded row");Restored();
   Reset();UnityEditor.LogEntries.ignoreFilters=true;
   for(int i=0;i<6000;i++)UnityEditor.LogEntries.all.Add(new UnityEditor.LogEntry{mode=4,message="info"});
   data=Read();Check((int)data["scannedRows"]<=5000 && (bool)data["scanLimited"],"bounded scan");Restored();
   Reset();UnityEditor.LogEntries.all.Add(new UnityEditor.LogEntry{mode=1,message="error"});UnityEditor.LogEntries.readFail=true;
   Check(ConnectorFailure().Contains("read failed"),"nested read cause visible through connector");Restored();
   Reset();UnityEditor.LogEntries.endFail=true;string failure=ConnectorFailure();
   Check(failure.Contains("end failed") && !failure.Contains("may not be restored"),"end failure with restored settings");Restored();
   Reset();UnityEditor.LogEntries.restoreFail=true;failure=ConnectorFailure();
   Check(failure.Contains("restore failed") && failure.Contains("may not be restored"),"restore warning visible through connector");
   Check(UnityEditor.LogEntries.consoleFlags==131 && UnityEditor.LogEntries.ends==1,"other restoration still attempted");
   Reset();UnityEditor.LogEntries.all.Add(new UnityEditor.LogEntry{mode=512|(1<<22)|(1<<13),message="warning with metadata"});
   data=Read();Check(((List<string>)data["entries"])[0]=="[Warning] warning with metadata","metadata bits are not severity");Restored();
   Reset();UnityEditor.LogEntries.all.Add(new UnityEditor.LogEntry{mode=1,message=new string('x',3991)+"\uD83D\uDE00tail"});
   data=Read();string unicode=((List<string>)data["entries"])[0];
   Check(!Char.IsHighSurrogate(unicode[unicode.Length-1]) && unicode.Length<=4000,"unicode boundary");Restored();
   Reset();UnityEditor.LogEntries.all.Add(new UnityEditor.LogEntry{mode=1,message="error"});
   UnityEditor.LogEntries.readFail=true;UnityEditor.LogEntries.restoreFail=true;failure=ConnectorFailure();
   Check(failure.Contains("read failed") && failure.Contains("restore failed") && failure.Contains("may not be restored"),"both failure causes preserved");
   Check(UnityEditor.LogEntries.consoleFlags==131 && UnityEditor.LogEntries.ends==1,"other cleanup after dual failure");
   Console.WriteLine("PASS: 11 C# probe scenarios");return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}
 }
}
'''


class ConsoleProbeTests(unittest.TestCase):
    def test_exact_probe_latest_limits_and_restoration(self):
        compiler=Path('C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe')
        if not compiler.exists(): self.skipTest('Windows C# compiler unavailable')
        with tempfile.TemporaryDirectory() as folder:
            source=Path(folder)/'probe.cs'; binary=Path(folder)/'probe.exe'
            source.write_text(FAKE.replace('BODY',C.CONSOLE_CODE),encoding='utf-8')
            built=subprocess.run([str(compiler),'/nologo','/out:'+str(binary),str(source)],capture_output=True,text=True,timeout=20)
            self.assertEqual(built.returncode,0,built.stdout+built.stderr)
            tested=subprocess.run([str(binary)],capture_output=True,text=True,timeout=5)
            self.assertEqual(tested.returncode,0,tested.stdout+tested.stderr)
            self.assertIn('11 C# probe scenarios',tested.stdout)


if __name__=='__main__':unittest.main()
