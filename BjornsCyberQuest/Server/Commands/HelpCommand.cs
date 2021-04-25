using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using BjornsCyberQuest.Server.Hubs;
using Pastel;

namespace BjornsCyberQuest.Server.Commands
{
    public class HelpCommand
    {
        [Command("help")]
        public async Task PrintHelp(ICommandHost host)
        {
            var knownCommands = new List<string> {"files.list", "files.open", "mails.list", "mails.open", "connect"};

            await host.WriteLine("known commands:".Pastel(Color.Gray));
            foreach (var command in knownCommands)
            {
                await host.WriteLine(command);
            }
        }
    }
}