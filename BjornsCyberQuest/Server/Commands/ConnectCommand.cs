using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using BjornsCyberQuest.Server.Hubs;
using Pastel;

namespace BjornsCyberQuest.Server.Commands
{
    public class ConnectCommand
    {
        [Command("connect")]
        public async Task Connect(ICommandHost host, ConnectCommandParameters? parameters)
        {
            if (parameters?.Host == null)
            {
                await host.WriteLine($"Usage: connect {"{ host: \"hostname\"}".Pastel(Color.Aquamarine)}...");
                return;
            }

            var h = host.GetHost(parameters.Host);
            if (h == null)
            {
                await host.WriteLine($"Unknown host {parameters.Host}".Pastel(Color.Red));
                return;
            }

            await host.Write($"Establishing connection to {parameters.Host.Pastel(Color.Aquamarine)}...");
            host.CurrentHost = parameters.Host;
            await Task.Delay(500);
            await host.WriteLine(" connected!");
        }
    }

    public class ConnectCommandParameters
    {
        public string? Host { get; set; }
    }
}