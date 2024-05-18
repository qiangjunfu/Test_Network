using System.Collections;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class TCPClient : MonoBehaviour
{
    private TcpClient _client;
    private NetworkStream _stream;
    private bool _isConnected = false;

    public string serverIp = "127.0.0.1";
    public int serverPort = 12345;
    public string messageToSend = "Hello from Unity Client!";

    async void Start()
    {
        await ConnectToServerAsync();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SendMessageToServer(messageToSend);
        }
    }

    private async Task ConnectToServerAsync()
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(serverIp, serverPort);
            _stream = _client.GetStream();
            _isConnected = true;
            Debug.Log("Connected to server");

            _ = Task.Run(async () => await ReceiveMessagesAsync());
        }
        catch (SocketException ex)
        {
            Debug.LogError("Socket exception: " + ex.Message);
        }
    }

    private async Task ReceiveMessagesAsync()
    {
        byte[] buffer = new byte[4096];

        while (_isConnected)
        {
            if (_stream.DataAvailable)
            {
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Debug.Log("Received from server: " + message);
                }
            }
            await Task.Delay(100); // Reduce CPU usage
        }
    }

    private async void SendMessageToServer(string message)
    {
        if (!_isConnected) return;

        try
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            await _stream.WriteAsync(buffer, 0, buffer.Length);
            await _stream.FlushAsync();
            Debug.Log("Sent to server: " + message);
        }
        catch (SocketException ex)
        {
            Debug.LogError("Error sending message: " + ex.Message);
        }
    }

    private void Disconnect()
    {
        _isConnected = false;
        if (_stream != null)
        {
            _stream.Close();
            _stream = null;
        }

        if (_client != null)
        {
            _client.Close();
            _client = null;
        }

        Debug.Log("Disconnected from server");
    }

    void OnApplicationQuit()
    {
        Disconnect();
    }
}
