using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MFroehlich.Parsing.JSON;

namespace LeagueReplay.Replay.UI.Details {
  /// <summary>
  /// Interaction logic for ReplayDetails.xaml
  /// </summary>
  public partial class ReplayDetails : Window {
    private long gameId;
    private MFroReplay replay;
    private static readonly Brush person = new SolidColorBrush(Color.FromArgb(130, 130, 130, 130));

    public ReplayDetails(MFroReplay replay, Window parent) {
      InitializeComponent();
      Owner = parent;
      WindowStartupLocation = WindowStartupLocation.CenterOwner;
      JSONObject data = replay.Combine;
      this.replay = replay;
      this.gameId = data.Get<long>("gameId");
      int red = 0, blue = 0,
        redGold = 0, blueGold = 0,
        redKill = 0, blueKill = 0,
        redAssist = 0, blueAssist = 0,
        redDeaths = 0, blueDeaths = 0;
      foreach (object value in data.Get<JSONObject>("players").Values) {
        var json = value as JSONObject;
        var info = json.Save<ReplayData>();
        if (info.teamId == 100) {
          if(info.statistics.win > 0){
            BlueOutcome.Content = "Victory";
            RedOutcome.Content = "Defeat";
          }
          BluePlayerDetails player = new BluePlayerDetails() { DataContext = new PlayerInfo(json) };
          if (info.summonerId == replay.SummonerId) player.Background = person;
          this.PlayerGrid.Children.Add(player);
          Grid.SetColumn(player, 0);
          Grid.SetRow(player, blue++);
          blueGold += info.statistics.goldEarned;
          blueKill += info.statistics.championsKilled;
          blueAssist += info.statistics.assists;
          blueDeaths += info.statistics.numDeaths;
        } else {
          if (info.statistics.win > 0) {
            BlueOutcome.Content = "Defeat";
            RedOutcome.Content = "Victory";
          }
          RedPlayerDetails player = new RedPlayerDetails() { DataContext = new PlayerInfo(json) };
          if (info.summonerId == replay.SummonerId) player.Background = person;
          this.PlayerGrid.Children.Add(player);
          Grid.SetColumn(player, 1);
          Grid.SetRow(player, red++);
          redGold += info.statistics.goldEarned;
          redKill += info.statistics.championsKilled;
          redAssist += info.statistics.assists;
          redDeaths += info.statistics.numDeaths;
        }
      }
      BlueGold.Content = (blueGold * .001).ToString("F1") + "k";
      RedGold.Content = (redGold * .001).ToString("F1") + "k";
      BlueKDA.Content = blueKill + " / " + blueDeaths + " / " + blueAssist;
      RedKDA.Content = redKill + " / " + redDeaths + " / " + redAssist;
      PlayerGrid.Height = (red > blue) ? red * 72 : blue * 72;
    }

    private void OpenDetails(object sender, EventArgs e) {
      System.Diagnostics.Process.Start("http://matchhistory.na.leagueoflegends.com/en/#match-details/NA1/" + gameId);
    }

    private void OpenReplay(object sender, EventArgs e) {
      new ReplayServer(replay);
    }
  }
}
