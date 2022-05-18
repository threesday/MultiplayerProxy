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
    internal class GameRoom
    {
        protected bool Running { get; set; }
        protected CancellationToken CancellationToken { get; set; }
        protected CancellationTokenRegistration CancellationTokenRegistration { get; set; }
        protected CancellationToken RoomCancellationToken { get; set; }
        protected CancellationTokenSource RoomCancellationTokenSource { get; set; }
        public GameRoom(string gameName, HostTcpClient hostTcpClient, CancellationToken cancellationToken)
        {
            GameName = gameName;
            ConnectionPairs = new ConcurrentBag<ConnectionPair>();
            HostTcpClient = hostTcpClient;
            CancellationToken = cancellationToken;
            RoomCancellationTokenSource = new CancellationTokenSource();
            RoomCancellationToken = RoomCancellationTokenSource.Token;
            CancellationTokenRegistration = CancellationToken.Register(() => RoomCancellationTokenSource.Cancel());
        }
        public HostTcpClient HostTcpClient { get; set; }
        public string GameName { get; set; }
        public ConcurrentBag<ConnectionPair> ConnectionPairs { get; set; }
        public void Start()
        {
            Running = true;
        }
        public void Stop()
        {
            Running = false;
            CancellationTokenRegistration.Unregister();
            RoomCancellationTokenSource.Cancel();
        }
        public void AddConnectionPair(ConnectionPair connectionPair)
        {
            Log("Adding connection pair.");
            ConnectionPairs.Add(connectionPair);
        }
        protected void Log(string message)
        {
            Console.WriteLine($"{nameof(GameRoom)}: {message}");
        }
    }
}
