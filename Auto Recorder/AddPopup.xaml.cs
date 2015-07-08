using System;
using System.Windows;

namespace Auto_Recorder {
  /// <summary>
  /// Interaction logic for AddPopup.xaml
  /// </summary>
  public partial class AddPopup : Window {
    public string SummonerName { get; private set; }

    public AddPopup() {
      InitializeComponent();
      SummonerBox.Focus();
    }

    public void Submit(object src, EventArgs arg) {
      SummonerName = SummonerBox.Text;
      Close();
    }

    public void Cancel(object src, EventArgs arg) {
      SummonerName = null;
      Close();
    }
  }
}
