using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MFroehlich.Parsing.DynamicJSON;
using MFroehlich.League.RiotAPI;
using MFroehlich.League.Assets;

namespace LeagueReplay.Replay.UI {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {
    private List<ReplayItem> replays = new List<ReplayItem>();
    public BindingList<ReplayItem> SortedReplays { get; private set; }

    public MainWindow() {
      RiotAPI.UrlFormat = "https://main-mfro.rhcloud.com/api/rito{0}";
      SortedReplays = new BindingList<ReplayItem>();
      InitializeComponent();
      this.ClientArea.Width = 800 + SystemParameters.VerticalScrollBarWidth;
      SearchBox.KeyUp += (src, e) => { Search(); };

      new Thread(LongInit).Start();
    }

    private void Search() {
      var items = from item in replays
                  where item.Matches(SearchBox.Text)
                  orderby item.Timestamp descending
                  select item;
      SortedReplays.Clear();
      foreach (var item in items) SortedReplays.Add(item);
    }

    private void OpenDetails(object info, MouseEventArgs args) {
      new Details.ReplayDetails(new MFroReplay(((ReplayItem) info).file), this).Show();
    }

    private void LongInit() {
      LeagueData.InitalizeProgressed += (status, detail, progess) => {
        Dispatcher.BeginInvoke(new Action(() => {
          LoadingStatus.Text = string.Format("{0} {1}...", status, detail);
          if (progess < 0) LoadingBar.IsIndeterminate = true;
          else {
            LoadingBar.Value = progess * 100;
            LoadingBar.IsIndeterminate = false;
          }
        }));
      };
      LeagueData.Initialize();
      Dispatcher.Invoke(() => {
        LoadingStatus.Text = "Loading replays...";
        LoadingDetail.Text = "";
        LoadingBar.Width = LoadArea.ActualWidth;
      });
      Dispatcher.Invoke(LoadMatches);
    }

    private void LoadMatches() {
      FileInfo summaryFile = new FileInfo(App.SummaryPath);
      var dir = new DirectoryInfo(App.Rootpath);
      if (!dir.Exists) dir.Create();

      Logger.WriteLine("Loading replays from {0}", App.Rootpath);

      FileStream loadSummary;
      if (!summaryFile.Exists) loadSummary = summaryFile.Create();
      else loadSummary = summaryFile.Open(FileMode.Open);
      var mems = new MemoryStream();
      loadSummary.CopyTo(mems);
      loadSummary.Close();

      dynamic summary;
      try {
        summary = MFroehlich.Parsing.MFro.MFroFormat.Deserialize(mems.ToArray());
        Logger.WriteLine("Loaded {0} summaries from {1}", summary.Count, summaryFile.FullName);
      } catch (Exception x) {
        summary = new JSONObject();
        Logger.WriteLine(Priority.Error, "Error loading summaries {0}, starting new summary list", x.Message);
      }
      dynamic newSummary = new JSONObject();

      List<FileInfo> files = new DirectoryInfo(App.Rootpath).EnumerateFiles("*.lol").ToList();
      files.Sort((a, b) => b.Name.CompareTo(a.Name));

      int summaries = 0;
      var timer = new System.Diagnostics.Stopwatch(); timer.Start();

      for (int i = 0; i < files.Count; i++) {
        string filename = files[i].Name.Substring(0, files[i].Name.Length - 4);

        ReplayItem item;
        if (summary.ContainsKey(filename)) {
          item = new ReplayItem((SummaryData) summary[filename], files[i]);
          newSummary.Add(filename, summary[filename]);
        } else {
          SummaryData data = new SummaryData(new MFroReplay(files[i]));
          newSummary.Add(filename, JSONObject.From(data));
          item = new ReplayItem(data, files[i]);
          summaries++;
        }
        item.MouseUp += OpenDetails;
        replays.Add(item);
      }

      Logger.WriteLine("All replays loaded, took {0}ms", timer.ElapsedMilliseconds);

      using (FileStream saveSummary = summaryFile.Open(FileMode.Open)) {
        byte[] summBytes = MFroehlich.Parsing.MFro.MFroFormat.Serialize(newSummary);
        saveSummary.Write(summBytes, 0, summBytes.Length);
        Logger.WriteLine("Saved summaries, {0} total summaries, {1} newly generated", newSummary.Count, summaries);
      }
      Search();
      ReplayArea.Visibility = System.Windows.Visibility.Visible;
      LoadArea.Visibility = System.Windows.Visibility.Hidden;
      Console.WriteLine("DONE");
    }
  }

  public class PlayerInfo {
    public BitmapImage ChampURI { get; private set; }
    public BitmapImage Spell1URI { get; private set; }
    public BitmapImage Spell2URI { get; private set; }

    public string KDA { get; private set; }

    public BitmapImage Item0URI { get; private set; }
    public BitmapImage Item1URI { get; private set; }
    public BitmapImage Item2URI { get; private set; }
    public BitmapImage Item3URI { get; private set; }
    public BitmapImage Item4URI { get; private set; }
    public BitmapImage Item5URI { get; private set; }
    public BitmapImage Item6URI { get; private set; }

    public string Minions { get; private set; }
    public string Gold { get; private set; }

    public PlayerInfo(JSONObject player) {
      ReplayData data = (dynamic) player;
      var stats = data.statistics;
      this.ChampURI = LeagueData.GetChamp(data.championId);
      this.Spell1URI = LeagueData.GetSpell(data.spell1Id);
      this.Spell2URI = LeagueData.GetSpell(data.spell2Id);
      this.KDA = stats.championsKilled + " / " + stats.numDeaths + " / " + stats.assists;
      this.Item0URI = LeagueData.GetItem(stats.item0);
      this.Item1URI = LeagueData.GetItem(stats.item1);
      this.Item2URI = LeagueData.GetItem(stats.item2);
      this.Item3URI = LeagueData.GetItem(stats.item3);
      this.Item4URI = LeagueData.GetItem(stats.item4);
      this.Item5URI = LeagueData.GetItem(stats.item5);
      this.Item6URI = LeagueData.GetItem(stats.item6);
      if (player["statistics"].ContainsKey("neutralMinionsKilledYourJungle"))
        this.Minions = (stats.minionsKilled + stats.neutralMinionsKilledEnemyJungle
          + stats.neutralMinionsKilledYourJungle).ToString();
      else this.Minions = (stats.minionsKilled + stats.neutralMinionsKilled).ToString();
      this.Gold = (stats.goldEarned*.001).ToString("F1") + "k";
    }
  }
  [JSONStructure]
  public class SummaryData {
    public int kills;
    public int deaths;
    public int assists;
    public bool win;
    public int championId;
    public int spell1Id;
    public int spell2Id;
    public int mapId;
    public int queueId;
    public long gameTime;
    public long gameId;

    public int item0;
    public int item1;
    public int item2;
    public int item3;
    public int item4;
    public int item5;
    public int item6;

    public SummaryData(MFroReplay replay) {
      dynamic combine = replay.Combine;
      var player = combine.players[replay.SummonerId.ToString()];
      var stats = player.statistics;
      kills = stats.championsKilled;
      deaths = stats.numDeaths;
      assists = stats.assists;
      win = stats.ContainsKey("win") && stats.win == 1;
      championId = player.championId;
      spell1Id = player.spell1Id;
      spell2Id = player.spell2Id;
      mapId = combine.mapId;
      queueId = combine.gameQueueConfigId;
      gameTime = combine.gameStartTime;
      gameId = combine.gameId;
      item0 = stats.item0;
      item1 = stats.item1;
      item2 = stats.item2;
      item3 = stats.item3;
      item4 = stats.item4;
      item5 = stats.item5;
      item6 = stats.item6;
    }

    public SummaryData() { }
  }
  [JSONStructure]
  public class ReplayData {
    public int championId, spell1Id, spell2Id, mapId, gameQueueConfigId, teamId;

    public long gameStartTime, summonerId;
    public Statistics statistics;
  }
  [JSONStructure]
  public class Statistics {
    public int item0, item1, item2, item3, item4, item5, item6,
      championsKilled, numDeaths, assists, win, goldEarned,
      minionsKilled, neutralMinionsKilledYourJungle, neutralMinionsKilledEnemyJungle, neutralMinionsKilled;
  }
}

