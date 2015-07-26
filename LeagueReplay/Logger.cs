using System;
using System.IO;

namespace LeagueReplay {
  public static class Logger {
    private static bool newLine = true;
    private static long start = GetTimestamp();

    public static long GetTimestamp() {
      return Convert.ToInt64(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds);
    }

    public static void Write(object obj) {
      Write(Priority.Normal, obj);
    }

    public static void Write(Priority priority, object obj) {
      if (newLine) {
        newLine = false;
        Write(priority, "[" + (GetTimestamp() - start).ToString("D8") + "] ");
      }
      using (var log = new FileStream(App.LogPath, FileMode.Append))
        log.Write(obj.ToString());
      if (priority != Priority.Low)
        Console.Write(obj.ToString());
    }

    public static void WriteLine(object obj) {
      WriteLine(Priority.Normal, obj);
    }

    public static void WriteLine(Priority priority, object obj) {
      Write(priority, obj.ToString() + '\n');
      newLine = true;
    }

    public static void WriteLine(string format, params object[] obj) {
      WriteLine(Priority.Normal, format.Format(obj) as object);
    }

    public static void WriteLine(Priority priority, string format, object obj) {
      WriteLine(priority, format.Format(obj) as object);
    }

    public static void Log(this System.Diagnostics.StackTrace trace) {
      foreach (var item in trace.GetFrames()) {
        WriteLine(item);
      }
    }
  }

  public enum Priority { Low, Normal, High, Error }
}
