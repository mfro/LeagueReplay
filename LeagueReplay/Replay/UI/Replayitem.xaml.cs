using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MFroehlich.League.Assets;

namespace LeagueReplay.Replay.UI {
  public partial class ReplayItem : UserControl {
    public ReplayItem(SummaryData data, FileInfo file) {
      this.file = file;
      InitializeComponent();
      if (data.win) this.OutcomeBox.Background = new SolidColorBrush(Color.FromRgb(110, 213, 110));
      else this.OutcomeBox.Background = new SolidColorBrush(Color.FromRgb(166, 57, 53));

      var champ = LeagueData.GetChampData(data.championId);
      this.Champion = champ["name"];
      this.ChampIcon.Source = LeagueData.GetChamp(champ["id"]);
      this.Spell1Icon.Source = LeagueData.GetSpell(data.spell1Id);
      this.Spell2Icon.Source = LeagueData.GetSpell(data.spell2Id);
      this.KDALabel.Content = data.kills + " / " + data.deaths + " / " + data.assists;
      this.GameLabel.Content = GameType = LeagueData.QueueTypes[data.queueId];
      this.MapLabel.Content = MapName = LeagueData.GameMaps[data.mapId];
      this.Item0Icon.Source = LeagueData.GetItem(data.item0);
      this.Item1Icon.Source = LeagueData.GetItem(data.item1);
      this.Item2Icon.Source = LeagueData.GetItem(data.item2);
      this.Item3Icon.Source = LeagueData.GetItem(data.item3);
      this.Item4Icon.Source = LeagueData.GetItem(data.item4);
      this.Item5Icon.Source = LeagueData.GetItem(data.item5);
      this.Item6Icon.Source = LeagueData.GetItem(data.item6);
      this.Timestamp = data.gameTime;
      DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(Timestamp);
      this.DateLabel.Content = date.ToString("M / d / yyyy");
      this.TimeLabel.Content = date.ToString("h:mm tt");
    }

    public FileInfo file { get; private set; }
    public string Champion { get; private set; }
    public string GameType { get; private set; }
    public string MapName { get; private set; }
    public long Timestamp { get; private set; }

    public bool Matches(string filter) {
      filter = Minimize(filter);
      return Minimize(Champion).Contains(filter) || Minimize(MapName).Contains(filter) ||
        Minimize(GameType).Contains(filter);
    }
    private static string Minimize(string str) {
      return System.Text.RegularExpressions.Regex.Replace(str, @"\s+", "").ToLower();
    }

    private void Grid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
      MainGrid.Background = new SolidColorBrush(Color.FromRgb(203, 232, 246));
    }

    private void Grid_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
      MainGrid.ClearValue(Grid.BackgroundProperty);
    }
  }
}
