using MFroehlich.Parsing.JSON;
using System;
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
using System.Windows.Shapes;

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
      int red = 0, blue = 0;
      foreach (object value in data.Get<JSONObject>("players").Values) {
        if ((value as JSONObject).Get<long>("teamId") == 100) {
          BluePlayerDetails player = new BluePlayerDetails() { DataContext = new PlayerInfo(value as JSONObject) };
          if ((value as JSONObject).Get<long>("summonerId") == replay.SummonerId) player.Background = person;
          this.PlayerGrid.Children.Add(player);
          Grid.SetColumn(player, 0);
          Grid.SetRow(player, blue++);
        } else {
          RedPlayerDetails player = new RedPlayerDetails() { DataContext = new PlayerInfo(value as JSONObject) };
          if ((value as JSONObject).Get<long>("summonerId") == replay.SummonerId) player.Background = person;
          this.PlayerGrid.Children.Add(player);
          Grid.SetColumn(player, 1);
          Grid.SetRow(player, red++);
        }
      }
    }

    private void OpenDetails(object sender, EventArgs e) {
      System.Diagnostics.Process.Start("http://matchhistory.na.leagueoflegends.com/en/#match-details/NA1/" + gameId);
    }

    private void OpenReplay(object sender, EventArgs e) {
      new ReplayServer(replay);
    }
  }
}
