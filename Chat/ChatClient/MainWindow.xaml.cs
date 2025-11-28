using ChatApi;
using Serilog;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChatClient;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

	private async void Button_Click(object sender, RoutedEventArgs e)
	{
		var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await clientSocket.ConnectAsync("localhost", 5000);	
        Log.Information("Connected to server\n");
        Status.Text += $"Connected {clientSocket.RemoteEndPoint}\n";

		_chatConnection = new ChatConnection(new PipeLineSocket(clientSocket));

	}

    
	private ChatConnection _chatConnection;

	private async void Button_Click1(object sender, RoutedEventArgs e)
	{
		if (_chatConnection == null)
		{
			Log.Information("No Connection\n");

		}

		await _chatConnection.SendMessage(new ChatMessage(ChatMessageTextBox.Text));
	}
}