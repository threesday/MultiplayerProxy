// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiplayerProxy;

var host = Host
            .CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddHostedService<ProxyServer>();
            })
            .Build();
host.Run();