using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Windows;
using AppResources = Auto_Recorder.Properties.Resources;
using Folder = System.Environment.SpecialFolder;

namespace Auto_Recorder {
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application {
    public static string DataPath = Environment.GetFolderPath(Folder.ApplicationData) + @"\MFro\AutoRecord\";

    public App() {
      var source = AppResources.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
      var resources = new Dictionary<object, object>();
      foreach (System.Collections.DictionaryEntry item in source) resources.Add(item.Key, item.Value);

      AppDomain.CurrentDomain.AssemblyResolve += (src, arg) => {
        string name = new AssemblyName(arg.Name).Name;
        if (resources.ContainsKey(name) && resources[name].GetType() == typeof(byte[]))
          return Assembly.Load(resources[name] as byte[]);
        return null;
      };

      System.IO.Directory.CreateDirectory(DataPath);
    }
  }
}
