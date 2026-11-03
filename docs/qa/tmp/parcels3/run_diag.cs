// 0141: 3판 한 번 돌리고 + 3연속·구덩이 자리
try { var r = ParcelBuild.Run(); return r + "\n---\n" + ParcelBuild.Diag(); } catch (System.Exception e) { return e.ToString(); }
