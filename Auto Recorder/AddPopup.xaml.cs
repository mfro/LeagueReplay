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
