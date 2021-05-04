using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
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
            if (!string.IsNullOrWhiteSpace(host.HelpText))
            {
                var lines = Regex.Split(host.HelpText, "\r\n|\r|\n");
                foreach (var line in lines)
                {
                    await host.WriteLine(line.Trim());
                    await Task.Delay(100);
                }
            }
            else
            {
                var knownCommands = new List<string> {"help", "files.list", "files.open", "mails.list", "mails.open", "connect"};

                await host.WriteLine("known commands:".Pastel(Color.Gray));
                foreach (var command in knownCommands)
                {
                    await host.WriteLine(command);
                }
            }
        }
    }
}