using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueReplay {
  public static class Logger {
    private static bool newLine = true;
    private static long start = GetTimestamp();

    public static long GetTimestamp() {
      return Convert.ToInt64(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds);
    }

    public static void Write(object obj) {
      using(var log = new FileStream(App.LogPath, FileMode.Append))
        log.Write(obj.ToString());
      Console.Write(obj.ToString());
    }

    public static void WriteLine(object obj) {
      if (newLine) Write("[" + (GetTimestamp() - start).ToString("D8") + "] ");
      Write(obj.ToString() + '\n');
      newLine = true;
    }

    public static void WriteLine(Priority priority, object obj) {
      WriteLine(obj);
    }

    public static void WriteLine(string format, params object[] obj) {
      WriteLine(format.Format(obj) as object);
    }

    public static void WriteLine(Priority priority, string format, object obj) {
      WriteLine(format, obj);
    }

    public static void Log(this System.Diagnostics.StackTrace trace) {
      foreach (var item in trace.GetFrames()) {
        WriteLine(item);
      }
    }
  }

  public enum Priority { LOW, NORMAL, HIGH, ERROR }
}
