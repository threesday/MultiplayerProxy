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
    internal class GameRoomCollection
    {
        protected ConcurrentDictionary<string, GameRoom> _gameRooms = new ConcurrentDictionary<string, GameRoom>();
        public bool AddRoom(GameRoom gameRoom)
        {
            return _gameRooms.TryAdd(gameRoom.GameName, gameRoom);
        }
        public GameRoom? GetRoom(string gameName)
        {
            _gameRooms.TryGetValue(gameName, out GameRoom? room);
            return room;
        }
        public void RemoveRoom(string gameName)
        {
            _gameRooms.TryRemove(gameName, out GameRoom? room);
        }
        public IPEndPoint? GetPairedClient(IPEndPoint packetEndpoint)
        {
            IPEndPoint? clientEndPoint = null;
            var connectionPair = 
                _gameRooms
                .SelectMany(e => e.Value.ConnectionPairs)
                .Where(c => 
                        (c.ClientEndPoint.Address.Address == packetEndpoint.Address.Address && c.ClientEndPoint.Port == packetEndpoint.Port) 
                        || 
                        (c.HostEndPoint.Address.Address == packetEndpoint.Address.Address && c.HostEndPoint.Port == packetEndpoint.Port)
                )
                .FirstOrDefault();
            if (connectionPair != null)
            {
                clientEndPoint =
                    (connectionPair.ClientEndPoint.Address.Address == packetEndpoint.Address.Address && connectionPair.ClientEndPoint.Port == packetEndpoint.Port)
                        ? connectionPair.HostEndPoint : connectionPair.ClientEndPoint;

            }
            return clientEndPoint;
        }
    }
}
