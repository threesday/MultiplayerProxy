using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerProxy
{
    internal class ClientTcpClient
    {
        public TcpClient TcpClient { get; set; }
        protected CancellationToken CancellationToken { get; set; }
        protected GameRoomCollection GameRoomCollection { get; set; }
        public int ClientUdpPort { get; set; }
        public ClientTcpClient(TcpClient tcpClient, GameRoomCollection gameRoomCollection, CancellationToken cancellationToken)
        {
            TcpClient = tcpClient;
            GameRoomCollection = gameRoomCollection;
            CancellationToken = cancellationToken;
        }
        protected GameRoom? NegotiateGameRoom()
        {
            // Do the initial handshake
            // Get the room name
            var buffer = new byte[1024];
            var memoryBuffer = new Memory<byte>(buffer);
            TcpClient.Client.Receive(buffer);
            var gameName = Encoding.ASCII.GetString(memoryBuffer.ToArray());
            Log($"Game name requested: {gameName}");
            var gameRoom = GameRoomCollection.GetRoom(gameName);
            if (gameRoom == null)
            {
                Log($"{gameName} game room does not exist.");
                TcpClient.Client.Send(Encoding.ASCII.GetBytes("no room"));
                TcpClient.Close();
            }
            else
            {
                TcpClient.Client.Send(Encoding.ASCII.GetBytes("ok"));
            }
            return gameRoom;
        }
        protected int? GetClientPort()
        {
            var buffer = new byte[1024];
            var memoryBuffer = new Memory<byte>(buffer);
            TcpClient.Client.Receive(buffer);
            var portString = Encoding.ASCII.GetString(memoryBuffer.ToArray());
            Log($"Port String Received: {portString}");
            Int32.TryParse(portString, out var port);
            return port;
        }
        public async Task Run()
        {
            var gameRoom = NegotiateGameRoom();
            
            if(gameRoom != null)
            {
                var port = GetClientPort();
                if (port != null)
                {
                    ClientUdpPort = port.Value;
                    gameRoom.HostTcpClient.HandleNewClientConnected(this);
                    Log($"Handed client off to HostTcpClient: {TcpClient.Client.RemoteEndPoint.ToString()}");
                }
            }
        }
        protected void Log(string message)
        {
            Console.WriteLine($"{nameof(ClientTcpClient)}: {message}");
        }
    }
}
