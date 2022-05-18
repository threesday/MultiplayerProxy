using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerProxy
{
    internal class ConnectionPair
    {
        public IPEndPoint HostEndPoint { get; set; }
        public IPEndPoint ClientEndPoint { get; set; }
    }
}
