using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LeagueReplay.Replay.UI {
  public partial class ReplayItem : UserControl {
    public ReplayItem(SummaryData data, FileInfo file) {
      this.file = file;
      if (data.win) Color = "#6ED56E"; else Color = "#A63935";

      this.Champion = LeagueData.ChampData.data[data.championId + ""].name;
      this.ChampURI = LeagueData.GetChamp(data.championId);
      this.Spell1URI = LeagueData.GetSpell(data.spell1Id);
      this.Spell2URI = LeagueData.GetSpell(data.spell2Id);
      this.KDA = data.kills + " / " + data.deaths + " / " + data.assists;
      this.MapName = LeagueData.GameMaps[data.mapId];
      this.GameType = LeagueData.QueueTypes[data.queueId];
      this.Item0URI = LeagueData.GetItem(data.item0);
      this.Item1URI = LeagueData.GetItem(data.item1);
      this.Item2URI = LeagueData.GetItem(data.item2);
      this.Item3URI = LeagueData.GetItem(data.item3);
      this.Item4URI = LeagueData.GetItem(data.item4);
      this.Item5URI = LeagueData.GetItem(data.item5);
      this.Item6URI = LeagueData.GetItem(data.item6);
      this.Timestamp = data.gameTime;
      DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(Timestamp);
      this.Date = date.ToString("M / d / yyyy");
      this.Time = date.ToString("h:mm tt");

      InitializeComponent();
    }

    public FileInfo file { get; private set; }
    public string Color { get; private set; }
    public string Champion { get; private set; }
    public BitmapImage ChampURI { get; private set; }

    public BitmapImage Spell1URI { get; private set; }
    public BitmapImage Spell2URI { get; private set; }

    public string KDA { get; private set; }
    public string MapName { get; private set; }
    public string GameType { get; private set; }

    public BitmapImage Item0URI { get; private set; }
    public BitmapImage Item1URI { get; private set; }
    public BitmapImage Item2URI { get; private set; }
    public BitmapImage Item3URI { get; private set; }
    public BitmapImage Item4URI { get; private set; }
    public BitmapImage Item5URI { get; private set; }
    public BitmapImage Item6URI { get; private set; }

    public string Date { get; private set; }
    public string Time { get; private set; }
    public long Timestamp { get; private set; }


    public bool Matches(string filter) {
      filter = Minimize(filter);
      return Minimize(Champion).Contains(filter) || Minimize(MapName).Contains(filter) ||
        Minimize(GameType).Contains(filter);
    }
    private static string Minimize(string str) {
      return System.Text.RegularExpressions.Regex.Replace(str, @"\s+", "").ToLower();
    }
  }
}
