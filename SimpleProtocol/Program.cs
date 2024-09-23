using System.IO.Pipelines;
using System.Text.Json;

var clientSenderReciever = new SenderReciever<ClientMessage, ServerMessage>();
var serverSenderReciever = new SenderReciever<ServerMessage, ClientMessage>();

var client = new Client();
var server = new Server();

var clientTask = clientSenderReciever.Start(client.SendMessage);
var serverTask = serverSenderReciever.Start(server.SendMessage);

var connection = clientSenderReciever.ConnectAsync(serverSenderReciever);

Task.WaitAll(clientTask, serverTask, connection);

static class Settings
{
    public const int throttle = 0;
    public const int messageCount = 1000;
}

public class Client
{
    public async IAsyncEnumerable<ClientMessage?> SendMessage(IAsyncEnumerable<ServerMessage?> messages)
    {
        int messageId = 0;

        yield return new ClientMessage
        {
            Id = messageId++,
            Message = "Client is ready"
        };

        await foreach (var message in messages)
        {
            if (Settings.throttle > 0)
            {
                await Task.Delay(Settings.throttle);
                Console.WriteLine($"Client received: {message.Id}:{message.Message}");
            }
            else
            {
                if (messageId % Settings.messageCount == 0)
                {
                    Console.WriteLine($"Client received: {message.Id}:{message.Message}");
                }
            }


            yield return new ClientMessage
            {
                Id = messageId++,
                Message = $"Client has processed {message.Id} from server"
            };
        }
    }
}

public class Server
{
    public async IAsyncEnumerable<ServerMessage?> SendMessage(IAsyncEnumerable<ClientMessage?> messages)
    {
        int messageId = 0;

        yield return new ServerMessage
        {
            Id = messageId++,
            Message = "Server is ready"
        };

        await foreach (var message in messages)
        {
            if (Settings.throttle > 0)
            {
                await Task.Delay(Settings.throttle);
                Console.WriteLine($"Server received: {message.Id}:{message.Message}");
            }
            else
            {
                if (messageId % Settings.messageCount == 0)
                {
                    Console.WriteLine($"Server received: {message.Id}:{message.Message}");
                }
            }

            yield return new ServerMessage
            {
                Id = messageId++,
                Message = $"Server has processed {message.Id} from client"
            };
        }
    }
}


public class ClientMessage
{
    public int Id { get; set; }
    public string Message { get; set; }
}

public class ServerMessage
{
    public int Id { get; set; }
    public string Message { get; set; }
}

public class SenderReciever<TSend, TRecieve>
    where TSend : class
    where TRecieve : class
{
    private readonly Pipe _inputPipe = new Pipe();
    private readonly Pipe _outputPipe = new Pipe();

    public Stream OutputStream => _outputPipe.Reader.AsStream();

    public Stream InputStream => _inputPipe.Writer.AsStream();

    public Task Start(Func<IAsyncEnumerable<TRecieve?>, IAsyncEnumerable<TSend?>> func)
    {
        var inputEnumeration = JsonSerializer.DeserializeAsyncEnumerable<TRecieve>(_inputPipe.Reader.AsStream());

        if (inputEnumeration == null)
        {
            throw new InvalidOperationException("Failed to deserialize input enumeration");
        }

        return JsonSerializer.SerializeAsync(_outputPipe.Writer.AsStream(true), func(inputEnumeration));
    }

    public async Task ConnectAsync(SenderReciever<TRecieve, TSend> that)
    {
        // Connect the output of this sender to the input of the other sender
        var thisWriterToThatReader = this.OutputStream.CopyToAsync(that.InputStream);

        // Connect the output of the other sender to the input of this sender
        var thatWriterToThisReader = that.OutputStream.CopyToAsync(this.InputStream);

        await Task.WhenAll(thisWriterToThatReader, thatWriterToThisReader);
    }
}