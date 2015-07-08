using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using MFroehlich.Parsing;
using MFroehlich.RiotAPI;

namespace Auto_Recorder {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {
    private static System.IO.FileInfo save = new System.IO.FileInfo(App.DataPath + "save");

    public BindingList<Summoner> Summoners { get; private set; }

    private NotifyIcon icon;
    private System.Windows.Forms.ContextMenu menu;

    public MainWindow() {
      MFroehlich.RiotAPI.RiotAPI.UrlFormat = "https://main-mfro.rhcloud.com/api/rito{0}";
      Summoners = new BindingList<Summoner>();

      try {
        if (save.Exists) {
          using (var fs = save.OpenRead()) {
            int len = fs.ReadInt();
            string param = "";
            for (int i = 0; i < len; i++) {
              param += "," + fs.ReadLong();
            }
            var summoners = RiotAPI.SummonerAPI.ById(param.Substring(1));
            foreach (var item in summoners.Values) {
              Summoners.Add(new Summoner(item.name, item.id));
            }
          }
        }
      } catch { }
      
      menu = new System.Windows.Forms.ContextMenu();
      menu.MenuItems.Add("Auto Recorder");
      menu.MenuItems.Add("Exit", OnExit);
      icon = new NotifyIcon();
      icon.Text = "Auto Recorder";
      icon.Icon = Auto_Recorder.Properties.Resources.Game_Recorder;
      icon.Click += (src, e) => {
        if ((e as System.Windows.Forms.MouseEventArgs).Button == MouseButtons.Left)
          Show();
      };
      icon.ContextMenu = menu;
      icon.Visible = true;

      InitializeComponent();

      new Thread(Record) { IsBackground = true, Name = "Recorder" }.Start();
    }

    protected override void OnClosing(CancelEventArgs e) {
      if (icon.Visible) {
        e.Cancel = true;
        Hide();
      }
      base.OnClosing(e);
    }

    private void OnExit(object src, EventArgs args) {
      icon.Visible = false;
      Close();
    }

    private void Remove(object sender, RoutedEventArgs e) {
      var summ = (sender as System.Windows.Controls.Button).DataContext as Summoner;
      Summoners.Remove(summ);
      Save();
    }

    private void Add(object sender, RoutedEventArgs e) {
      AddPopup popup = new AddPopup();
      popup.Owner = this;
      popup.ShowDialog();
      if (popup.SummonerName == null) return;
      string summ = System.Text.RegularExpressions.Regex.Replace(popup.SummonerName.ToLower(), @"\s+", "");
      try {
        var summoners = RiotAPI.SummonerAPI.ByName(summ);
        foreach (var item in summoners.Values) {
          Summoners.Add(new Summoner(item.name, item.id));
        }
      } catch {
        System.Windows.MessageBox.Show(this, string.Format("An error occured while finding the summoner {0}.", popup.SummonerName), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      Save();
    }

    private void Record() {
      while (true) {
        foreach (var summ in new List<Summoner>(Summoners)) {
          try {
            RiotAPI.CurrentGameAPI.BySummoner("NA1", summ.Id);
            Process.Start("League Replay.exe", "record " + summ.Id);
            Console.WriteLine("Succeeded " + summ.Name);
          } catch {
            Console.WriteLine("Failed " + summ.Name);
          }
        }
        Thread.Sleep(60000);
      }
    }

    private void Save() {
      using (var fs = new System.IO.FileStream(App.DataPath + "save", System.IO.FileMode.Create)) {
        fs.WriteInt(Summoners.Count);
        foreach (var summ in Summoners) {
          fs.WriteLong(summ.Id);
        }
      }
    }
  }

  public class Summoner {
    public bool Recording { get; set; }
    public string Name { get; set; }
    public long Id { get; set; }

    public Summoner(string name, long id) {
      Recording = false;
      Name = name;
      Id = id;
    }
  }
}
