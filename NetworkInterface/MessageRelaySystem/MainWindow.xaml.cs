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
using P2CCore;

namespace MessageRelaySystem {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow:Window {

		#region Fields

		NetworkServer networkServer; 
		UserAccount userAccount;

		#endregion Fields

		public MainWindow() {
			InitializeComponent();
		}

		private void btnStart_Click(object sender, RoutedEventArgs e) {
			networkServer = new NetworkServer();
			networkServer.Start();
			
			userAccount = new UserAccount(){UserNick = txtNick.Text};

			btnStart.IsEnabled = false;

			btnStop.IsEnabled = true;
			btnRemoteConnect.IsEnabled = true;

			txtStatus.Text = "Server running";
		}

		private void btnStop_Click(object sender, RoutedEventArgs e) {
			networkServer.Disconnect();

			btnStart.IsEnabled = true;

			btnStop.IsEnabled = false;
			btnRemoteConnect.IsEnabled = false;

			txtStatus.Text = "Server stopped";
		}

		private void txtNick_TextChanged(object sender, TextChangedEventArgs e) {
			if(String.IsNullOrEmpty(txtNick.Text) || String.IsNullOrWhiteSpace(txtNick.Text)){
				btnRemoteConnect.IsEnabled = false;
				btnSend.IsEnabled = false;

				if(userAccount == null)
					btnStart.IsEnabled = false;
			}
			else{
				btnRemoteConnect.IsEnabled = true;
				btnSend.IsEnabled = true;
				btnStart.IsEnabled = true;
			}
		}


	}
}
