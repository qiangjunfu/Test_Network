using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class UDPClient : MonoBehaviour
{
    private UdpClient _udpClient;
    private IPEndPoint _remoteEndPoint;
    private IPEndPoint _localEndPoint;

    public string serverIp = "127.0.0.1";
    public int serverPort = 12345;
    public int localPort;  // 客户端本地端口
    public string messageToSend = "Hello from Unity UDP Client!";

    async void Start()
    {
        localPort = Random.Range(50000, 60000);  // 生成随机端口
        _udpClient = new UdpClient(localPort);  // 使用指定的本地端口
        _remoteEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
        _localEndPoint = new IPEndPoint(IPAddress.Any, localPort);

        Debug.Log("Client started on port: " + localPort);

        await SendMessageToServer(messageToSend);
        _ = Task.Run(async () => await ReceiveMessagesAsync());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _ = SendMessageToServer(messageToSend);
        }
    }

    private async Task SendMessageToServer(string message)
    {
        try
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            await _udpClient.SendAsync(buffer, buffer.Length, _remoteEndPoint);
            Debug.Log("Sent to server: " + message);
        }
        catch (SocketException ex)
        {
            Debug.LogError("Error sending message: " + ex.Message);
        }
    }

    private async Task ReceiveMessagesAsync()
    {
        while (true)
        {
            try
            {
                UdpReceiveResult receivedResult = await _udpClient.ReceiveAsync();
                string message = Encoding.ASCII.GetString(receivedResult.Buffer);
                Debug.Log("Received from server: " + message);
            }
            catch (SocketException ex)
            {
                Debug.LogError("Error receiving message: " + ex.Message);
            }
        }
    }

    private void OnApplicationQuit()
    {
        _udpClient.Close();
    }
}
