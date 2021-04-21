using System.Drawing;
using System.Threading.Tasks;
using BjornsCyberQuest.Shared;
using Microsoft.AspNetCore.SignalR;
using Pastel;

namespace BjornsCyberQuest.Server.Hubs
{
    public class TerminalHub : Hub<ITerminalHub>, ITerminal
    {
        public override async Task OnConnectedAsync()
        {
            if (!Context.Items.TryGetValue("host", out var host))
                Context.Items["host"] = "localhost";
            if (!Context.Items.TryGetValue("directory", out var directory))
                Context.Items["directory"] = "~";
            await Ready();
        }

        public async Task SendInput(string input)
        {
            await Clients.Caller.ReceiveOutput(input.Pastel(Color.Aqua) + "\r\n");
            await Ready();
        }

        private async Task Ready()
        {
            var prompt = "";
            if (Context.Items.TryGetValue("user", out var user))
                prompt += $"{user}@";

            if (Context.Items.TryGetValue("host", out var host))
                prompt += $"{host}";

            if (Context.Items.TryGetValue("directory", out var directory))
                prompt += $":{directory}";

            prompt += "> ";
            await Clients.Caller.Ready(prompt);
        }
    }
}