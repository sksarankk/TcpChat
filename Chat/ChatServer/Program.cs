// See https://aka.ms/new-console-template for more information
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
	var pipe = new Pipe();
	var socketToPipelineTask = SocketToPipelineAsync(socket, pipe.Writer);
	var pipelineToSocketTask = HandlePipeLineAsync(pipe.Reader);
	await Task.WhenAll(socketToPipelineTask, pipelineToSocketTask);

	async Task HandlePipeLineAsync(PipeReader pipeReader)
	{
		while (true)
		{
			var data = await pipeReader.ReadAsync();

			foreach (var message in ParseMessages(data.Buffer, pipeReader))
			{
				Console.WriteLine($"Got message from {socket.RemoteEndPoint} : {message}");
			}

			if (data.IsCompleted)
			{
				break;
			}

		}
		

	}
}
IReadOnlyList<String> ParseMessages(ReadOnlySequence<byte> buffer, PipeReader pipeReader)
{
	var result = new List<String>();
	var sequenceReader = new SequenceReader<byte>(buffer);

	while (sequenceReader.Remaining != 0)
	{
		var beginOfMessagePosition = sequenceReader.Position;
		if (!sequenceReader.TryReadBigEndian(out int signedLengthPrefix))
		{
			//pipeReader.AdvanceTo(beginOfMessagePosition, buffer.End);
			break;
		}
		var lengthPrefix = (uint)signedLengthPrefix;
		if (lengthPrefix == 0) break;
		//if (lengthPrefix > MaxMessageSize)
		//{
		//	throw new InvalidOperationException($"Message size {lengthPrefix} exceeds maximum of {MaxMessageSize}");
		//}
		if (!sequenceReader.TryReadBigEndian(out int messageType))
		{
			//pipeReader.AdvanceTo(beginOfMessagePosition, buffer.End);
			break;
		}

		if (messageType == 0)
		{
			var chatMessaageBytes = new byte[lengthPrefix - 4];
			if (!sequenceReader.TryCopyTo(chatMessaageBytes))
			{
				//Ensure the pipeline has maximum buffer size;
				pipeReader.AdvanceTo(buffer.Start, buffer.End);
				break;
			}
			sequenceReader.Advance(chatMessaageBytes.Length);
			result.Add(Encoding.UTF8.GetString(chatMessaageBytes));


		}
		else
		{
			throw new InvalidOperationException($"Unknown message type {messageType}");

		}
		pipeReader.AdvanceTo(sequenceReader.Position);


	}
	return result;
	
}


async Task SocketToPipelineAsync(Socket socket, PipeWriter pipeWriter)
{
	while (true)
	{
		var buffer = pipeWriter.GetMemory();
		var bytesRead = await socket.ReceiveAsync(buffer, SocketFlags.None);
		if(bytesRead == 0)
		{	
			pipeWriter.Complete();
			break;	
		}
		pipeWriter.Advance(bytesRead);
		await pipeWriter.FlushAsync();

	}
}
