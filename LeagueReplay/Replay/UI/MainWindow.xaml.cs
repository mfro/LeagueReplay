using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

using MFroehlich.Parsing.JSON;
using System.ComponentModel;

namespace LeagueReplay.Replay.UI {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {
    private List<ReplayItem> replays = new List<ReplayItem>();
    public BindingList<ReplayItem> SortedReplays { get; private set; }

    public MainWindow() {
      MFroehlich.RiotAPI.RiotAPI.UrlFormat = "https://main-mfro.rhcloud.com/api/rito{0}";
      SortedReplays = new BindingList<ReplayItem>();
      InitializeComponent();
      this.ClientArea.Width = 800 + SystemParameters.VerticalScrollBarWidth;
      Show();
      SearchBox.KeyUp += (src, e) => { if (e.Key == Key.Enter) Search(); };
      
      new Thread(LoadMatches) { IsBackground = true }.Start();
    }

    private void Search() {
      var items = from item in replays
                  where item.Matches(SearchBox.Text)
                  orderby item.Timestamp descending
                  select item;
      SortedReplays.Clear();
      foreach (var item in items) SortedReplays.Add(item);
    }

    private void OpenDetails(ReplayItem info) {
      new Details.ReplayDetails(new MFroReplay(info.file), this).Show();
    }

    private void LoadMatches() {
      LeagueData.Init();
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

      JSONObject summary;
      try {
        summary = MFroehlich.Parsing.MFro.MFroFormat.Deserialize(mems.ToArray());
        Logger.WriteLine("Loaded {0} summaries from {1}", summary.Count, summaryFile.FullName);
      } catch (Exception x) {
        summary = new JSONObject();
        Logger.WriteLine(Priority.ERROR, "Error loading summaries {0}, starting new summary list", x.Message);
      }
      var newSummary = new JSONObject();

      List<FileInfo> files = new DirectoryInfo(App.Rootpath).EnumerateFiles("*.lol").ToList();
      files.Sort((a, b) => b.Name.CompareTo(a.Name));

      int summaries = 0;
      var timer = new System.Diagnostics.Stopwatch(); timer.Start();

      for (int i = 0; i < files.Count; i++) {
        string filename = files[i].Name.Substring(0, files[i].Name.Length - 4);

        if (summary.ContainsKeys(filename)){
          int b = i;
          App.Current.Dispatcher.BeginInvoke(new Action(() => {
            var item = new ReplayItem(summary.Get<JSONObject>(filename).Save<SummaryData>(), files[b]);
            SortedReplays.Add(item);
            item.MouseUp += (src, e) => OpenDetails(item);
            replays.Add(item);
          }));
          newSummary.Add(filename, summary[filename]);
        } else {
          SummaryData data = new SummaryData(new MFroReplay(files[i]));
          newSummary.Add(filename, JSONObject.Load(data));
          App.Current.Dispatcher.BeginInvoke(new Action(() => {
            var item = new ReplayItem(data, files[i]);
            SortedReplays.Add(item);
            item.MouseUp += (src, e) => OpenDetails(item);
            replays.Add(item);
          }));
          summaries++;
        }
      }
      Logger.WriteLine("All replays loaded, took {0}ms", timer.ElapsedMilliseconds);

      using (FileStream saveSummary = summaryFile.Open(FileMode.Open)) {
        byte[] summBytes = MFroehlich.Parsing.MFro.MFroFormat.Serialize(newSummary);
        saveSummary.Write(summBytes, 0, summBytes.Length);
        Logger.WriteLine("Saved summaries, {0} total summaries, {1} newly generated", newSummary.Count, summaries);
      }
      App.Current.Dispatcher.BeginInvoke(new Action(() => {
        SearchBox.Text = "";
        SearchBox.IsEnabled = true;
        Search();
      }));
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
      ReplayData data = player.Save<ReplayData>();
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
      if (player.ContainsKeys("statistics", "neutralMinionsKilledYourJungle"))
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
      var combine = replay.Combine;
      var player = combine.Get<JSONObject>("players", replay.SummonerId.ToString());
      var stats = player.Get<JSONObject>("statistics");
      kills = stats.Get<int>("championsKilled");
      deaths = stats.Get<int>("numDeaths");
      assists = stats.Get<int>("assists");
      win = stats.ContainsKeys("win") && stats.Get<int>("win") == 1;
      championId = player.Get<int>("championId");
      spell1Id = player.Get<int>("spell1Id");
      spell2Id = player.Get<int>("spell2Id");
      mapId = combine.Get<int>("mapId");
      queueId = combine.Get<int>("gameQueueConfigId");
      gameTime = combine.Get<long>("gameStartTime");
      gameId = combine.Get<long>("gameId");
      item0 = stats.Get<int>("item0");
      item1 = stats.Get<int>("item1");
      item2 = stats.Get<int>("item2");
      item3 = stats.Get<int>("item3");
      item4 = stats.Get<int>("item4");
      item5 = stats.Get<int>("item5");
      item6 = stats.Get<int>("item6");
    }

    public SummaryData() { }
  }
  [JSONStructure]
  public class ReplayData {
    public int championId, spell1Id, spell2Id, mapId, gameQueueConfigId;

    public long gameStartTime;
    public Statistics statistics;
  }
  [JSONStructure]
  public class Statistics {
    public int item0, item1, item2, item3, item4, item5, item6,
      championsKilled, numDeaths, assists, win, goldEarned,
      minionsKilled, neutralMinionsKilledYourJungle, neutralMinionsKilledEnemyJungle, neutralMinionsKilled;
  }
}

