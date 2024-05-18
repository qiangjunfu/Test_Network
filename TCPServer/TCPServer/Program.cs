using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

class Program
{
    static async Task Main(string[] args)
    {
        TCPServer server = new TCPServer();
        await server.StartServerAsync(12345);

        Console.WriteLine("Press any key to stop the server...");
        Console.ReadKey();

        await server.StopServerAsync();
    }
}

public class TCPServer
{
    private TcpListener _listener;
    private ConcurrentBag<ClientHandler> _clients = new ConcurrentBag<ClientHandler>();
    private bool _isRunning = false;
    private CancellationTokenSource _cancellationTokenSource;

    public async Task StartServerAsync(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        _isRunning = true;
        Console.WriteLine("Server started on port " + port);

        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            while (_isRunning)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    if (client != null)
                    {
                        string clientEndPoint = client.Client.RemoteEndPoint.ToString();
                        Console.WriteLine($"Client connected from {clientEndPoint}");

                        var clientHandler = new ClientHandler(client, this);
                        _clients.Add(clientHandler);

                        var clientTask = Task.Run(() => clientHandler.HandleClientCommAsync(_cancellationTokenSource.Token));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception accepting client: " + ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception in StartServerAsync: " + ex.Message);
        }
    }

    public void RemoveClient(ClientHandler clientHandler)
    {
        _clients = new ConcurrentBag<ClientHandler>(_clients.Except(new[] { clientHandler }));
    }

    public void BroadcastMessage(string message, ClientHandler sender)
    {
        byte[] buffer = Encoding.ASCII.GetBytes(message);

        foreach (var client in _clients)
        {
            client.SendMessage(buffer);
        }
    }

    public async Task StopServerAsync()
    {
        _isRunning = false;
        _cancellationTokenSource.Cancel();
        _listener.Stop();

        List<Task> disconnectTasks = new List<Task>();
        foreach (var client in _clients)
        {
            disconnectTasks.Add(Task.Run(() => client.Disconnect()));
        }

        await Task.WhenAll(disconnectTasks);

        Console.WriteLine("Server stopped.");
    }
}

public class ClientHandler
{
    private TcpClient _client;
    private NetworkStream _clientStream;
    private TCPServer _server;
    private bool _isConnected;
    private string _clientEndPoint;

    public ClientHandler(TcpClient client, TCPServer server)
    {
        _client = client;
        _clientStream = client.GetStream();
        _server = server;
        _isConnected = true;
        _clientEndPoint = client.Client.RemoteEndPoint.ToString();
    }

    public async Task HandleClientCommAsync(CancellationToken token)
    {
        byte[] message = new byte[4096];

        try
        {
            while (_isConnected && !token.IsCancellationRequested)
            {
                int bytesRead = 0;

                try
                {
                    bytesRead = await _clientStream.ReadAsync(message, 0, message.Length, token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Client {_clientEndPoint} disconnected due to error: {ex.Message}");
                    break;
                }

                if (bytesRead == 0)
                {
                    Console.WriteLine($"Client {_clientEndPoint} disconnected.");
                    break;
                }

                string clientMessage = Encoding.ASCII.GetString(message, 0, bytesRead);
                Console.WriteLine($"Received from {_clientEndPoint}: {clientMessage}");

                string str = _clientEndPoint  + ": " + clientMessage;
                _server.BroadcastMessage(str, this);
            }
        }
        finally
        {
            Disconnect();
        }
    }

    public void SendMessage(byte[] buffer)
    {
        try
        {
            _clientStream.Write(buffer, 0, buffer.Length);
            _clientStream.Flush();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message to {_clientEndPoint}: " + ex.Message);
        }
    }

    public void Disconnect()
    {
        _isConnected = false;
        _clientStream?.Dispose();
        _client?.Close();
        _server.RemoveClient(this);
    }
}
