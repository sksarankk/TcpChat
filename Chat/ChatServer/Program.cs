// See https://aka.ms/new-console-template for more information
using ChatApi;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;

Console.WriteLine("starting server...");
const uint MaxMessageSize = 65536;
var listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
listeningSocket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, 5000));

listeningSocket.Listen();
Console.WriteLine("Listening...");

while(true)
{
	var connectedSocket1 = await listeningSocket.AcceptAsync();

	Console.WriteLine($"Got a connection from {connectedSocket1.RemoteEndPoint} to {connectedSocket1.LocalEndPoint}.\n");

	_ = ProcessSocket(connectedSocket1);

}

async Task ProcessSocket(Socket socket)
{
	var pipelineSocket = new ChatConnection(new PipeLineSocket(socket));
	await pipelineSocket.MainTask;
}