using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerProxy
{
    internal class ProxyServer : IHostedService
    {
        protected readonly int TcpHostServerPort = 4000;
        protected readonly int TcpClientServerPort = 4001;
        protected readonly int UdpServerPort = 5000;
        protected GameRoomCollection GameRoomCollection = new GameRoomCollection();
        protected bool Running = false;
        protected async void RunUdpServer(CancellationToken cancellationToken)
        {
            Log(nameof(RunUdpServer), "Server started.");
            try
            {
                var udpServer = new UdpClient();
                udpServer.ExclusiveAddressUse = false;
                udpServer.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpServer.Client.Bind(new IPEndPoint(IPAddress.Any, 5000));
                Log(nameof(RunUdpServer), "UDP Server started. Awaiting packets.");
                while (Running && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        UdpReceiveResult? receiveResult = null;
                        try
                        {
                            receiveResult = await udpServer.ReceiveAsync(cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Log(nameof(RunUdpServer), $"UDP Server Receive Exception Ocurred: {ex.Message}");
                        }
                        Log(nameof(RunUdpServer), $"Received packet from {receiveResult.Value.RemoteEndPoint.ToString()}. Trying to forward.");
                        var pairedClientEndPoint = GameRoomCollection.GetPairedClient(receiveResult.Value.RemoteEndPoint);
                        if (pairedClientEndPoint != null)
                        {
                            Log(nameof(RunUdpServer), $"Paired client found. Sending data to {pairedClientEndPoint.ToString()}.");
                            try
                            {
                                udpServer.Send(receiveResult.Value.Buffer, receiveResult.Value.Buffer.Length, pairedClientEndPoint);
                            }
                            catch (Exception ex)
                            {
                                Log(nameof(RunUdpServer), $"UDP Server Exception: {ex.ToString()}");
                            }
                        }
                        else
                        {
                            Log(nameof(RunUdpServer), "Paired client could not be found. Ignoring data.");
                        }
                    }
                    catch(Exception ex)
                    {
                        Log(nameof(RunUdpServer), ex.Message);
                    }
                }

            }
            catch(Exception ex)
            {
                Log(nameof(RunUdpServer), ex.ToString());
            }
        }
        protected async void RunTcpHostServer(CancellationToken cancellationToken)
        {
            try
            {
                var tcpServer = new TcpListener(IPAddress.Any, TcpHostServerPort);
                tcpServer.Start();
                while (Running && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var tcpClient = await tcpServer.AcceptTcpClientAsync(cancellationToken);
                        Log(nameof(RunTcpHostServer), $"Client connected: {tcpClient.Client.RemoteEndPoint.ToString()}");
                        var hostTcpClient = new HostTcpClient(tcpClient, GameRoomCollection, cancellationToken);
                        hostTcpClient.Run();

                    }
                    catch (Exception ex)
                    {
                        Log(nameof(RunTcpHostServer), ex.Message);
                    }
                }
            }
            catch(Exception ex)
            {
                Log(nameof(RunTcpHostServer), ex.ToString());
            }

        }
        protected async void RunTcpClientServer(CancellationToken cancellationToken)
        {
            try
            {
                var tcpServer = new TcpListener(IPAddress.Any, TcpClientServerPort);
                tcpServer.Start();
                while (Running && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var tcpClient = await tcpServer.AcceptTcpClientAsync(cancellationToken);
                        Log(nameof(RunTcpClientServer), $"Client connected: {tcpClient.Client.RemoteEndPoint.ToString()}");
                        var clientTcpClient = new ClientTcpClient(tcpClient, GameRoomCollection, cancellationToken);
                        clientTcpClient.Run();

                    }
                    catch (Exception ex)
                    {
                        Log(nameof(RunTcpClientServer), ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log(nameof(RunTcpClientServer), ex.Message);
            }

        }


        protected void Log(string requestor, string message)
        {
            Console.WriteLine($"{requestor}: {message}");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Running = true;
            RunTcpHostServer(cancellationToken);
            RunTcpClientServer(cancellationToken);
            RunUdpServer(cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Running = false;
            return Task.CompletedTask;
        }
    }
}
