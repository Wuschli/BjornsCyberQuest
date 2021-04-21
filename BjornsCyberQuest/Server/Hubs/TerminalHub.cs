using System.Drawing;
using System.Threading.Tasks;
using BjornsCyberQuest.Shared;
using Microsoft.AspNetCore.SignalR;
using Pastel;

namespace BjornsCyberQuest.Server.Hubs
{
    public class TerminalHub : Hub<ITerminalHub>, ITerminal
    {
        public async Task SendInput(string input)
        {
            await Clients.Caller.ReceiveOutput(input.Pastel(Color.Aqua) + "\r\n");
        }
    }
}