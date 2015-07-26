using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using AppResources = LeagueReplay.Properties.Resources;
using Folder = System.Environment.SpecialFolder;

namespace LeagueReplay {
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application {

    public static readonly string
      DataPath = Environment.GetFolderPath(Folder.ApplicationData) + @"\MFro\LeagueReplay\",
      Rootpath = Environment.GetFolderPath(Folder.MyDocuments) + @"\League Games\",
      SummaryPath = DataPath + "summary",
      LogPath = DataPath + "log.txt";

    public App() {
      new DirectoryInfo(DataPath).Create();
      Logger.WriteLine(" - Launch [" + DateTime.Now.ToString("MM/dd/yy H:mm:ss:FFF") + "] - ");

      var source = AppResources.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
      var resources = new Dictionary<object, object>();
      foreach (System.Collections.DictionaryEntry item in source) resources.Add(item.Key, item.Value);

      AppDomain.CurrentDomain.AssemblyResolve += (src, arg) => {
        string name = new AssemblyName(arg.Name).Name;
        if (resources.ContainsKey(name) && resources[name].GetType() == typeof(byte[]))
          return Assembly.Load(resources[name] as byte[]);
        return null;
      };

      string[] args = Environment.GetCommandLineArgs();

      if (args.Length == 1) {
        new LeagueReplay.Replay.UI.MainWindow().Show();
      } else if (args[1].Equals("record") && args.Length > 2) {
        string summonerId = args[2];
        new Record.Recorder(summonerId);
        Environment.Exit(0);
      } else {
        var file = new FileInfo(args[1]);
        if (file.Exists) new Replay.UI.Details.ReplayDetails(new MFroReplay(file), null).Show();
        else Environment.Exit(0);
      }
    }
  }

  public static class Extensions {
    public static string Format(this string str, params object[] args) {
      return String.Format(str, args);
    }

    public static byte[] GetBytes(this string str) {
      return System.Text.Encoding.UTF8.GetBytes(str);
    }

    public static string GetString(this byte[] bytes) {
      return System.Text.Encoding.UTF8.GetString(bytes);
    }

    public static int ReadFully(this System.IO.Stream stream, byte[] array, int off, int len) {
      int read = 0;
      int count;
      while ((count = stream.Read(array, read + off, len - read)) > 0) read += count;
      return read;
    }

    public static void WriteInt(this System.IO.Stream stream, int value) {
      byte[] data = BitConverter.GetBytes(value);
      if (BitConverter.IsLittleEndian) data = data.Flip();
      stream.Write(data, 0, 4);
    }

    public static void WriteLong(this System.IO.Stream stream, long value) {
      byte[] data = BitConverter.GetBytes(value);
      if (BitConverter.IsLittleEndian) data = data.Flip();
      stream.Write(data, 0, 8);
    }

    public static void Write(this System.IO.Stream stream, string value) {
      byte[] data = System.Text.Encoding.UTF8.GetBytes(value);
      stream.Write(data, 0, data.Length);
    }

    public static byte[] Flip(this byte[] bytes) {
      byte[] ret = new byte[bytes.Length];
      for (int i = 0; i < bytes.Length; i++) ret[bytes.Length - 1 - i] = bytes[i];
      return ret;
    }
  }
}
