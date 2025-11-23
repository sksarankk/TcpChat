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
		_clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await _clientSocket.ConnectAsync("localhost", 5000);	
        Log.Information("Connected to server\n");
        StatusTextBox.Text += $"Connected {_clientSocket.RemoteEndPoint}\n";

		var pipelineSocket = new PipeLineSocket(_clientSocket);

		var message = "Hello server!";
        var messageBytes = Encoding.UTF8.GetBytes(message);
		var memory = pipelineSocket.OutputPipe.GetMemory(messageBytes.Length + 8);
		BinaryPrimitives.TryWriteUInt32BigEndian(memory.Span, (uint) messageBytes.Length + 4);// message length including type
		BinaryPrimitives.TryWriteUInt32BigEndian(memory.Span.Slice(4), 0); // message type

		messageBytes.CopyTo(memory.Span.Slice(8));//actual message starts after 8 bytes for length and type
		pipelineSocket.OutputPipe.Advance(messageBytes.Length + 8);
		await pipelineSocket.OutputPipe.FlushAsync();

	}

    
	private Socket _clientSocket;
}