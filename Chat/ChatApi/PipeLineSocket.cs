using System.IO.Pipelines;
using System.Net.Sockets;
using System.Xml.Linq;

namespace ChatApi
{
	public sealed class PipeLineSocket
	{
		private readonly Pipe _outpuPipe;
		private readonly Pipe _inputPipe;
		public Socket Socket { get; }

		public PipeWriter OutputPipe => _outpuPipe.Writer;

		public PipeReader InputPipe => _inputPipe.Reader;

		public Task MainTask { get;}
		public PipeLineSocket(Socket connectedSocket)
		{
			Socket = connectedSocket;
			_outpuPipe = new Pipe();
			_inputPipe = new Pipe();
			MainTask = Task.WhenAll(
				PipelineToSocketAsync(_outpuPipe.Reader, Socket),
				SocketToPipelineAsync(Socket, _inputPipe.Writer)
			);

		}

		private async Task PipelineToSocketAsync(PipeReader pipeReader, Socket socket)
		{
			while (true)
			{
				var result = await pipeReader.ReadAsync();
				var buffer = result.Buffer;

				while (true)
				{
					var memory = buffer.First;
					if (memory.IsEmpty)
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
					socket.Close();
					break;
				}

			}
		}

		private async Task SocketToPipelineAsync(Socket socket, PipeWriter pipeWriter)
		{
			while (true)
			{
				var buffer = pipeWriter.GetMemory();
				var bytesRead = await socket.ReceiveAsync(buffer, SocketFlags.None);
				if (bytesRead == 0)
				{
					pipeWriter.Complete();
					break;
				}
				pipeWriter.Advance(bytesRead);
				await pipeWriter.FlushAsync();

			}
		}



	}
}
