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

		var pipe = new Pipe();
		_ = PipelineToSocketAsync(pipe.Reader, _clientSocket);

		var message = "Hello server!";
        var messageBytes = Encoding.UTF8.GetBytes(message);
		var memory = pipe.Writer.GetMemory(messageBytes.Length + 8);
		BinaryPrimitives.TryWriteUInt32BigEndian(memory.Span, (uint) messageBytes.Length + 4);// message length including type
		BinaryPrimitives.TryWriteUInt32BigEndian(memory.Span.Slice(4), 0); // message type

		messageBytes.CopyTo(memory.Span.Slice(8));//actual message starts after 8 bytes for length and type
		pipe.Writer.Advance(messageBytes.Length + 8);
		await pipe.Writer.FlushAsync();

	}

    private async Task PipelineToSocketAsync(PipeReader pipeReader, Socket socket)
    {
        while (true)
        {
            var result = await pipeReader.ReadAsync();
			var buffer = result.Buffer;

            while(true)
			{
				var memory = buffer.First;
                if(memory.IsEmpty)
				{
					break;
				}
				var bytesSent = await socket.SendAsync(memory, SocketFlags.None);
				buffer = buffer.Slice(bytesSent);
				if (bytesSent != memory.Length)
                {
                    break;
                }
			}
			pipeReader.AdvanceTo(buffer.Start);
			if (result.IsCompleted)
			{
				break;
			}

		}
	}
	private Socket _clientSocket;
}