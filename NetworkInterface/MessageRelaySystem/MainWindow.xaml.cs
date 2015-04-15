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
using P2CCommon;

namespace MessageRelaySystem {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow:Window {

		#region Fields

		NetworkServer networkServer; 
		UserAccount userAccount;

		int messageSentCounter = 0;
		#endregion Fields

		public MainWindow() {
			InitializeComponent();

			for(int i = 0; i < 256; ++i){
				cbFirstIP.Items.Add(i);
				cbSecondIP.Items.Add(i);
				cbThirdIP.Items.Add(i);
				cbFourthIP.Items.Add(i);
			}

			cbFirstIP.SelectedIndex		= 0;
			cbSecondIP.SelectedIndex	= 0;
			cbThirdIP.SelectedIndex		= 0;
			cbFourthIP.SelectedIndex	= 0;
		}

		private void btnStart_Click(object sender, RoutedEventArgs e) {
			userAccount = new UserAccount(){UserNick = txtNick.Text};
			
			networkServer = new NetworkServer(userAccount.PublicProfile);
			networkServer.Start();
			
			networkServer.P2CDS += new NetworkServer.P2CDeliveryService(PackageHandler);

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
			btnSend.IsEnabled = false;

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

		private async void btnSend_Click(object sender, RoutedEventArgs e) {
			if(String.IsNullOrWhiteSpace(txtMessage.Text) || String.IsNullOrEmpty(txtMessage.Text))
				return;

			String message = txtMessage.Text;

			await Task.Factory.StartNew(()=>{ 
				networkServer.Send(PackageStatus.Message, message); 
			}).ConfigureAwait(false);

			txtChatWindow.InvokeIfRequired(()=>{
				txtChatWindow.AppendText(userAccount.UserNick + ":" + Environment.NewLine + txtMessage.Text);
				txtMessage.Clear();
				txtMessage.Focus();
				txtStatus.Text = ++messageSentCounter + " Message(s) sent";
			});					
		}


		async void PackageHandler(Package package, EventArgs e){
			Task.Factory.StartNew(()=>{


			});
		}


	}


	// allow updating in UI Thread from different thread
	static class ControlExtension{
		public static void InvokeIfRequired(this Control control, Action action){
			if(control.Dispatcher.CheckAccess()){
				action();
			}
			else{
				control.Dispatcher.Invoke(action);
			}
		}
	}


}
