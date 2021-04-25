using System.Threading.Tasks;
using BjornsCyberQuest.Server.Hubs;

namespace BjornsCyberQuest.Server.Commands
{
    public class ConnectCommand
    {
        [Command("connect")]
        public async Task Connect(ICommandHost host, ConnectCommandParameters? parameters)
        {
        }
    }

    public class ConnectCommandParameters
    {
        public string? Host { get; set; }
    }
}