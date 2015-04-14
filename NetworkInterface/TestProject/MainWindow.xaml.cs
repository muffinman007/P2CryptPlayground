using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

using System.Net.Sockets;
using System.Net;

namespace TestProject {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow:Window {

		CancellationTokenSource tokenSource;
		CancellationToken cancelToken;
		long counter;
		Task t1;

		SocketServer server;

		public MainWindow(){
			InitializeComponent();

			server = new SocketServer();
		}

		private void btnStartTask_Click(object sender, RoutedEventArgs e){
			tokenSource = new CancellationTokenSource();
			cancelToken = tokenSource.Token;
			counter = 0;
			t1 = Task.Factory.StartNew(async ()=>
			{
				while(!cancelToken.IsCancellationRequested){
					++counter;

					
					await server.Run();
				}

				MessageBox.Show("Task is cancelling.");
			});

			btnStartTask.IsEnabled = false;
		}

		private void btnCancelTask_Click(object sender, RoutedEventArgs e) {
			if(t1 == null)
				return;
			
			tokenSource.Cancel();

			MessageBox.Show("Results:" + Environment.NewLine +
							"Task: " + t1.ToString() + Environment.NewLine +
							"Task status:" + t1.Status.ToString() + Environment.NewLine +
							"counter: " + counter.ToString());

			counter = 0;			

			btnStartTask.IsEnabled = true;		
		}

		private void btnShowTaskStatus_Click(object sender, RoutedEventArgs e) {
			if(t1 == null)
				return;

			Task.Factory.StartNew(()=>
			{
				MessageBox.Show("Results:" + Environment.NewLine +
								"Task: " + t1.ToString() + Environment.NewLine +
								"Task status:" + t1.Status.ToString() + Environment.NewLine +
								"counter: " + counter.ToString());
			});

			if(t1.IsCanceled)
				t1 = null;			
		}

	}


	public class SocketServer{
		Socket socket;

		public SocketServer(){
			socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.Bind(new IPEndPoint(IPAddress.Any, 12345));
			socket.Listen(10);
		}

		public async Task Run(){
			Socket client;

			client = socket.Accept();
		}

		public void CleanUp(){
			try{
				socket.Dispose();
			}
			catch(Exception){}
		}

	}
}
