using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatApi
{
    public sealed class ChatConnection
    {
        private readonly PipeLineSocket _pipeLineSocket;

		private readonly uint _maxMessageSize;

        public ChatConnection(PipeLineSocket pipeLineSocket, uint MaxMessageSize = 65536)
        {
            _pipeLineSocket = pipeLineSocket;
			_maxMessageSize = MaxMessageSize;
			MainTask = Task.WhenAll(pipeLineSocket.MainTask, HandlePipeLineAsync());
        }

        public Socket Socket => _pipeLineSocket.Socket;
		public Task MainTask { get; }

		public async Task SendMessage(IMessage message)
		{		
			//TODO: currently not thread safe
			if (message is ChatMessage chatMessage) {
				var messageBytes = Encoding.UTF8.GetBytes(chatMessage.Text);
				var memory = _pipeLineSocket.OutputPipe.GetMemory(messageBytes.Length + 8);
				BinaryPrimitives.TryWriteUInt32BigEndian(memory.Span, (uint)messageBytes.Length + 4);// message length including type
				BinaryPrimitives.TryWriteUInt32BigEndian(memory.Span.Slice(4), 0); // message type

				messageBytes.CopyTo(memory.Span.Slice(8));//actual message starts after 8 bytes for length and type
				_pipeLineSocket.OutputPipe.Advance(messageBytes.Length + 8);
			}
			else
				throw new InvalidOperationException("unkonwn message type");

			await _pipeLineSocket.OutputPipe.FlushAsync();

		}
		private async Task HandlePipeLineAsync()
		{
			while (true)
			{
				var data = await _pipeLineSocket.InputPipe.ReadAsync();

				foreach (var message in ParseMessages(data.Buffer))
				{
					if (message is ChatMessage chatMessage)
						Console.WriteLine($"Got message from {Socket.RemoteEndPoint} : {chatMessage.Text}");
					else
						Console.WriteLine($"got unknown message from {Socket.RemoteEndPoint}.");
				}

				if (data.IsCompleted)
				{
					break;
				}

			}


		}

		private IReadOnlyList<IMessage> ParseMessages(ReadOnlySequence<byte> buffer)
		{
			var result = new List<IMessage>();
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
				if (lengthPrefix > _maxMessageSize)
				{
					throw new InvalidOperationException($"Message size {lengthPrefix} exceeds maximum of {_maxMessageSize}");
				}
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
						_pipeLineSocket.InputPipe.AdvanceTo(buffer.Start, buffer.End);
						break;
					}
					sequenceReader.Advance(chatMessaageBytes.Length);
					result.Add(new ChatMessage(Encoding.UTF8.GetString(chatMessaageBytes)));


				}
				else
				{
					throw new InvalidOperationException($"Unknown message type {messageType}");

				}
				_pipeLineSocket.InputPipe.AdvanceTo(sequenceReader.Position);


			}
			return result;

		}

	}
}
