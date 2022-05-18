using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerProxy
{
    internal class HostTcpClient
    {
        protected TcpClient TcpClient { get; set; }
        protected CancellationToken CancellationToken { get; set; }
        protected CancellationTokenSource ClientCancellationTokenSource { get; set; }
        protected CancellationToken ClientCancellationToken { get; set; }
        protected CancellationTokenRegistration CancellationTokenRegistration { get; set; }
        protected GameRoomCollection GameRoomCollection { get; set; }
        protected ConcurrentQueue<string> MessageQueue { get; set; }
        protected ConcurrentQueue<ClientTcpClient> ClientConnectionQueue { get; set; }
        protected GameRoom GameRoom { get; set; }
        public HostTcpClient(TcpClient tcpClient, GameRoomCollection gameRoomCollection, CancellationToken cancellationToken)
        {
            TcpClient = tcpClient;
            CancellationToken = cancellationToken;
            GameRoomCollection = gameRoomCollection;
            ClientConnectionQueue = new ConcurrentQueue<ClientTcpClient>();
            ClientCancellationTokenSource = new CancellationTokenSource();
            ClientCancellationToken = ClientCancellationTokenSource.Token;
            CancellationTokenRegistration = cancellationToken.Register(() => ClientCancellationTokenSource.Cancel());
            MessageQueue = new ConcurrentQueue<string>();
        }
        protected GameRoom? NegotiateGameRoomName()
        {
            try
            {
                // Do the initial handshake
                // Get the room name
                var buffer = new byte[1024];
                var memoryBuffer = new Memory<byte>(buffer);
                TcpClient.Client.Receive(buffer);
                var gameName = Encoding.ASCII.GetString(memoryBuffer.ToArray());
                var gameRoom = new GameRoom(gameName, this, CancellationToken);
                Log($"Game name requested: {gameName}");
                var gameNameAdded = GameRoomCollection.AddRoom(gameRoom);
                if (!gameNameAdded)
                {
                    Log($"Game name is taken.");
                    TcpClient.Client.Send(Encoding.ASCII.GetBytes("taken"));
                    TcpClient.Close();
                    return null;
                }
                else
                {
                    TcpClient.Client.Send(Encoding.ASCII.GetBytes("ok"));
                }
                return gameRoom;
            }
            catch (Exception ex)
            {
                Log($"Exception ocurred in NegotiateGameRoom: {ex.Message}");
            }
            return null;
        }
        public async void Run()
        {
            Log("Host TCP Client Connected.");
            var gameRoom = NegotiateGameRoomName();
            if (gameRoom == null)
            {
                Log("Game room could not be negotiated.");
                return;
            }
            GameRoom = gameRoom;
            GameRoom.Start();
            Log("Game room negotiated.");
            HandleIncomingMessages();
            await HandleConnectedClients();
        }
        protected void CleanupGameRoom()
        {
            try
            {
                // Clean up game room
                GameRoomCollection.RemoveRoom(GameRoom.GameName);
                GameRoom.Stop();
                Log($"Game room {GameRoom.GameName} was removed.");
                ClientCancellationTokenSource.Cancel();
            }
            catch(Exception ex)
            {
                Log($"Exception ocurred in CleanupGameRoom: {ex.Message}");
            }
        }
        protected async Task HandleIncomingMessages()
        {
            try
            {
                var buffer = new byte[1024];
                var memoryBuffer = new Memory<byte>(buffer);
                while (!ClientCancellationToken.IsCancellationRequested)
                {
                    int bytesReceived = await TcpClient.Client.ReceiveAsync(memoryBuffer, SocketFlags.None, CancellationToken);
                    if (bytesReceived <= 0)
                    {
                        Log($"Client {TcpClient.Client.RemoteEndPoint.ToString()} is no longer connected.");
                        CleanupGameRoom();
                    }
                    var message = Encoding.ASCII.GetString(memoryBuffer.ToArray());
                    MessageQueue.Enqueue(message);
                }
            }
            catch(Exception ex)
            {
                Log($"Exception ocurred in HandleIncomingMessages: {ex.Message}");
                CleanupGameRoom();
            }
            return;
        }
        protected async Task HandleConnectedClients()
        {
            try
            {
                while (!ClientCancellationToken.IsCancellationRequested)
                {
                    if (ClientConnectionQueue.Any())
                    {
                        ClientConnectionQueue.TryDequeue(out var client);
                        var ipEndpoint = client.TcpClient.Client.RemoteEndPoint as IPEndPoint;
                        var clientUdpEndpoint = new IPEndPoint(ipEndpoint.Address.Address, client.ClientUdpPort);
                        await TcpClient.Client.SendAsync(new ReadOnlyMemory<byte>(Encoding.ASCII.GetBytes("client connected")), SocketFlags.None, CancellationToken);
                        string portMessage = GetNextMessage();
                        int port = Int32.Parse(portMessage);
                        Console.WriteLine($"Host port received: {port.ToString()}");
                        var hostEndpoint = new IPEndPoint(((IPEndPoint)TcpClient.Client.RemoteEndPoint).Address, port);
                        GameRoom.AddConnectionPair(new ConnectionPair
                        {
                            HostEndPoint = hostEndpoint,
                            ClientEndPoint = clientUdpEndpoint
                        });
                        await TcpClient.Client.SendAsync(new ReadOnlyMemory<byte>(Encoding.ASCII.GetBytes("ok")), SocketFlags.None, CancellationToken);
                        await client.TcpClient.Client.SendAsync(new ReadOnlyMemory<byte>(Encoding.ASCII.GetBytes("ok")), SocketFlags.None, CancellationToken);
                        client.TcpClient.Client.Close();
                    }
                    else
                        Thread.Sleep(500);
                }
            }
            catch (Exception ex)
            {
                Log($"Exception occurred in HandleConnectedClients: {ex.Message}");
                CleanupGameRoom();
            }
            Log("Handle connected clients exiting.");
            CancellationTokenRegistration.Unregister();
        }
        protected string GetNextMessage()
        {
            while(!CancellationToken.IsCancellationRequested && MessageQueue.Count == 0)
            {
                Thread.Sleep(500);
            }
            MessageQueue.TryDequeue(out var result);
            return result;
        }
        public void HandleNewClientConnected(ClientTcpClient client)
        {
            Log($"New client connected: {client.TcpClient.Client.RemoteEndPoint.ToString()}");
            ClientConnectionQueue.Enqueue(client);
        }
        protected void Log(string message)
        {
            Console.WriteLine($"{nameof(HostTcpClient)}: {message}");
        }
    }
}
