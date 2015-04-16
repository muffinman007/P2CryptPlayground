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
using System.IO;

namespace MessageRelaySystem {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow:Window {

		#region Fields

		bool hasServerStarted;

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

			hasServerStarted = false;
		}

		private void btnStart_Click(object sender, RoutedEventArgs e) {
			if(userAccount == null)
				userAccount = new UserAccount(){UserNick = txtNick.Text};
			
			if(networkServer == null){
				networkServer = new NetworkServer(userAccount.PublicProfile);
				networkServer.P2CDS += new NetworkServer.P2CDeliveryService(PackageHandler);
			}

			// we need to await this but most also give user a progress remote or a busy signal letting them know the network is starting up
			networkServer.Start();			

			btnStart.IsEnabled = false;
			hasServerStarted = true;

			btnStop.IsEnabled = true;
			btnRemoteConnect.IsEnabled = true;
			btnSend.IsEnabled = true;
			btnChangeNick.IsEnabled = true;

			txtStatus.Text = "Server running";
		}

		private void btnStop_Click(object sender, RoutedEventArgs e) {
			networkServer.Disconnect();
			hasServerStarted = false;

			// notified everyone user logoff
			Task.Factory.StartNew(()=>{ networkServer.Send(PackageStatus.LogOff, string.Empty); });

			btnStart.IsEnabled = true;

			btnStop.IsEnabled = false;
			btnRemoteConnect.IsEnabled = false;
			btnSend.IsEnabled = false;
			btnChangeNick.IsEnabled = false;

			txtStatus.Text = "Server stopped";
		}


		private void txtNick_TextChanged(object sender, TextChangedEventArgs e) {
			// Require a nick before server can be started for the first time
			if(String.IsNullOrEmpty(txtNick.Text) || String.IsNullOrWhiteSpace(txtNick.Text)){
				if(!hasServerStarted)
					btnSend.IsEnabled = false;
				else
					btnChangeNick.IsEnabled = false;
			}
			else{
				if(!hasServerStarted)
					btnStart.IsEnabled = true;
				else
					btnChangeNick.IsEnabled = true;
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
				txtChatWindow.AppendText(userAccount.UserNick + ":  " + Environment.NewLine + txtMessage.Text + Environment.NewLine);
				txtMessage.Clear();
				txtMessage.Focus();
				txtStatus.Text = ++messageSentCounter + " Message(s) sent";
			});					
		}


		async void PackageHandler(Package package, EventArgs e){
			string str = string.Empty;
			if(package.PackageStatus == PackageStatus.LogOff || package.PackageStatus == PackageStatus.NickUpdate)
				str = txtFriendsList.Text;

			// some user may have thousands of nick on their list so it may be better to do the split on a different thread
			Task.Factory.StartNew(()=>{
				string[] nickArray = null;
				if(!string.IsNullOrEmpty(str))
					nickArray = str.Split(new string[]{Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
				
				switch(package.PackageStatus){
					case PackageStatus.Connect:
						txtChatWindow.InvokeIfRequired(()=>{
							txtChatWindow.AppendText(Environment.NewLine + package.PublicProfile.UserNick + " joined." + Environment.NewLine);
							txtFriendsList.AppendText(package.PublicProfile.UserNick + Environment.NewLine);
						});
						break;

					case PackageStatus.LogOff:
						IEnumerable<string> listOfNicks = from nick in nickArray
														  where !String.Equals(nick, package.Information.Item2)
														  select nick;
						txtChatWindow.InvokeIfRequired(()=>{
							txtChatWindow.AppendText(Environment.NewLine + package.Information.Item2 + " logged out." + Environment.NewLine);
							foreach(var nick in listOfNicks)
								txtFriendsList.AppendText(nick + Environment.NewLine);
						});
						break;

					case PackageStatus.NickUpdate:
						for(int i = 0; i < nickArray.Length; ++i){
							if(string.Equals(nickArray[i], package.Information.Item2)){
								nickArray[i] = package.Information.Item3;
								break;
							}
						}
						txtChatWindow.InvokeIfRequired(()=>{
							txtChatWindow.AppendText(Environment.NewLine + package.Information.Item2 + " changed to " + package.Information.Item3 + Environment.NewLine);
							foreach(var nick in nickArray)
								txtFriendsList.AppendText(nick + Environment.NewLine);
						});
						break;

					case PackageStatus.Message:
						string message = Encoding.UTF8.GetString(userAccount.Decrypt(package.Data));
						txtChatWindow.InvokeIfRequired(()=>{
							txtChatWindow.AppendText(Environment.NewLine + package.Information.Item2 + ":  " + message);
						});
						break;
				}				
			});
		}

		private void btnRemoteConnect_Click(object sender, RoutedEventArgs e) {
			string ip = cbFirstIP.Text + "." + cbSecondIP.Text + "." + cbThirdIP.Text + "." + cbFourthIP.Text + ":" + txtPort.Text;
			
			Task.Factory.StartNew(()=>{
				networkServer.ConnectToRemote(ip);
			});
		}

		private void btnChangeNick_Click(object sender, RoutedEventArgs e) {
			string oldNick = userAccount.UserNick;
			userAccount.UserNick = txtNick.Text;

			Task.Factory.StartNew(()=>{ networkServer.Send(PackageStatus.NickUpdate, oldNick); });
			txtStatus.Text = "Nick change: " + oldNick + " to " + txtNick.Text;
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
