// Fixed exec body, maintained by PitCrew. No user text is substituted.
// Uses Unity's internal console API; unsupported versions fail explicitly.
try
{
var asm = typeof(UnityEditor.EditorApplication).Assembly;
var logs = asm.GetType("UnityEditor.LogEntries", true);
var rowType = asm.GetType("UnityEditor.LogEntry", true);
var statics = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var instances = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var start = logs.GetMethod("StartGettingEntries", statics);
var end = logs.GetMethod("EndGettingEntries", statics);
var count = logs.GetMethod("GetCount", statics);
var get = logs.GetMethod("GetEntryInternal", statics);
var flags = logs.GetProperty("consoleFlags", statics);
var getFilter = logs.GetMethod("GetFilteringText", statics);
var setFilter = logs.GetMethod("SetFilteringText", statics);
var message = rowType.GetField("message", instances);
var mode = rowType.GetField("mode", instances);
if (start == null || end == null || count == null || get == null || flags == null ||
    !flags.CanRead || !flags.CanWrite || getFilter == null || setFilter == null || message == null || mode == null)
    throw new System.NotSupportedException("This Unity version does not expose the required console read/restore API.");
int savedFlags = System.Convert.ToInt32(flags.GetValue(null, null));
string savedFilter = (string)getFilter.Invoke(null, null);
bool started = false;
var entries = new System.Collections.Generic.List<string>();
bool truncated = false;
int scanned = 0, total = 0, index = -1;
System.Exception readFailure = null;
System.Func<System.Exception, string> describe = error => {
    var cause = error.GetBaseException();
    string detail = cause.GetType().Name + ": " + cause.Message;
    if (detail.Length > 240) {
        int length = 240;
        if (System.Char.IsHighSurrogate(detail[length - 1]) && System.Char.IsLowSurrogate(detail[length])) length--;
        detail = detail.Substring(0, length) + " [truncated]";
    }
    return detail;
};
try
{
    // Error/warning visible, ordinary logs hidden, Collapse off; preserve other options.
    int readFlags = (savedFlags | (1 << 8) | (1 << 9)) & ~((1 << 0) | (1 << 7));
    flags.SetValue(null, readFlags, null);
    setFilter.Invoke(null, new object[] { "" });
    if (System.Convert.ToInt32(flags.GetValue(null, null)) != readFlags ||
        !string.IsNullOrEmpty((string)getFilter.Invoke(null, null)))
        throw new System.InvalidOperationException("Console display filters could not be prepared for a complete read.");
    start.Invoke(null, null);
    started = true;
    total = System.Convert.ToInt32(count.Invoke(null, null));
    if (total < 0) throw new System.InvalidOperationException("Invalid console row count.");
    var row = System.Activator.CreateInstance(rowType);
    var watch = System.Diagnostics.Stopwatch.StartNew();
    for (index = total - 1; index >= 0 && entries.Count < 50 && scanned < 5000 && watch.ElapsedMilliseconds < 100; index--)
    {
        scanned++;
        if (!(bool)get.Invoke(null, new object[] { index, row }))
            throw new System.InvalidOperationException("Console changed during observation; retry.");
        int bits = System.Convert.ToInt32(mode.GetValue(row));
        int errorBits = (1 << 0) | (1 << 1) | (1 << 4) | (1 << 6) | (1 << 8) | (1 << 11) |
                        (1 << 17) | (1 << 21);
        int warningBits = (1 << 7) | (1 << 9) | (1 << 12);
        bool error = (bits & errorBits) != 0;
        if (!error && (bits & warningBits) == 0) continue;
        string text = (string)message.GetValue(row);
        if (string.IsNullOrEmpty(text)) continue;
        string prefix = error ? "[Error] " : "[Warning] ";
        int room = 4000 - prefix.Length;
        if (text.Length > room) {
            if (System.Char.IsHighSurrogate(text[room - 1]) && System.Char.IsLowSurrogate(text[room])) room--;
            text = text.Substring(0, room); truncated = true;
        }
        entries.Add(prefix + text);
    }
    return new System.Collections.Generic.Dictionary<string, object> {
        { "dataPath", UnityEngine.Application.dataPath }, { "entries", entries },
        { "order", "newest-first" }, { "totalRows", total }, { "scannedRows", scanned },
        { "hasEarlier", index >= 0 }, { "scanLimited", index >= 0 && entries.Count < 50 },
        { "textTruncated", truncated }
    };
}
catch (System.Exception error)
{
    readFailure = error;
    throw;
}
finally
{
    // Attempt every cleanup and preserve both observation and cleanup failures.
    var failures = new System.Collections.Generic.List<string>();
    bool restoreFailed = false;
    try { if (started) end.Invoke(null, null); }
    catch (System.Exception error) { failures.Add("End: " + describe(error)); }
    try { flags.SetValue(null, savedFlags, null); }
    catch (System.Exception error) { restoreFailed = true; failures.Add("Flags: " + describe(error)); }
    try { setFilter.Invoke(null, new object[] { savedFilter }); }
    catch (System.Exception error) { restoreFailed = true; failures.Add("Search: " + describe(error)); }
    try {
        if (System.Convert.ToInt32(flags.GetValue(null, null)) != savedFlags ||
            (string)getFilter.Invoke(null, null) != savedFilter)
            throw new System.InvalidOperationException("Console display settings could not be restored.");
    }
    catch (System.Exception error) { restoreFailed = true; failures.Add("Verify: " + describe(error)); }
    if (failures.Count > 0) {
        if (readFailure != null) failures.Add("Read: " + describe(readFailure));
        throw new System.InvalidOperationException(
            (restoreFailed ? "Console settings may not be restored. " : "Console cleanup failed. ") +
            string.Join(" | ", failures.ToArray()), readFailure);
    }
}
}
catch (System.Reflection.TargetInvocationException error)
{
    // The connector unwraps Execute's invocation once. Expose the underlying
    // console failure here, after all restoration attempts have completed.
    var cause = error.GetBaseException();
    throw new System.InvalidOperationException("Console read/restore failed: " +
        cause.GetType().Name + ": " + cause.Message, error);
}
