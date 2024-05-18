using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class UDPServer
{
    private UdpClient _udpClient;
    private IPEndPoint _localEndPoint;
    private bool _isRunning = false;
    private CancellationTokenSource _cancellationTokenSource;
    private HashSet<IPEndPoint> _clients;

    public async Task StartServerAsync(int port)
    {
        _udpClient = new UdpClient(port);
        _localEndPoint = new IPEndPoint(IPAddress.Any, port);
        _isRunning = true;
        _cancellationTokenSource = new CancellationTokenSource();
        _clients = new HashSet<IPEndPoint>();

        Console.WriteLine("UDP Server started on port " + port);

        try
        {
            while (_isRunning)
            {
                var receivedResult = await _udpClient.ReceiveAsync();
                string clientMessage = Encoding.ASCII.GetString(receivedResult.Buffer);
                Console.WriteLine($"Received from {receivedResult.RemoteEndPoint}: {clientMessage}");

                lock (_clients)
                {
                    if (_clients.Add(receivedResult.RemoteEndPoint))
                    {
                        Console.WriteLine($"Added client: {receivedResult.RemoteEndPoint}");
                    }
                }

                BroadcastMessage(clientMessage);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception in StartServerAsync: " + ex.Message);
        }
    }

    public void BroadcastMessage(string message)
    {
        byte[] buffer = Encoding.ASCII.GetBytes(message);

        Console.WriteLine($"Broadcasting message: {message}");
        lock (_clients)
        {
            foreach (var client in _clients)
            {
                Console.WriteLine($"服务器发送: {message} 到 {client}");
                _udpClient.Send(buffer, buffer.Length, client);
            }
        }
    }

    public void StopServer()
    {
        _isRunning = false;
        _cancellationTokenSource.Cancel();
        _udpClient.Close();
        Console.WriteLine("UDP Server stopped.");
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        UDPServer server = new UDPServer();
        await server.StartServerAsync(12345);

        Console.WriteLine("Press any key to stop the server...");
        Console.ReadKey();

        server.StopServer();
    }
}
