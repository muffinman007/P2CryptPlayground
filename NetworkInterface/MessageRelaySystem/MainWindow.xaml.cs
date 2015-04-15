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
using System.Windows.Navigation;
using System.Windows.Shapes;

using Network;

namespace MessageRelaySystem {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow:Window {

		#region Fields

		NetworkServer networkServer; 

		#endregion Fields

		public MainWindow() {
			InitializeComponent();
		}

		private void btnStart_Click(object sender, RoutedEventArgs e) {
			networkServer = new NetworkServer();
			networkServer.Start();

			txtStatus.Text = "Server running";
			btnStart.IsEnabled = false;

			btnStop.IsEnabled = true;
			btnRemoteConnect.IsEnabled = true;
		}


	}
}
