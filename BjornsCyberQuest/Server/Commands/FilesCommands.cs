using System.Threading.Tasks;
using BjornsCyberQuest.Server.Hubs;

namespace BjornsCyberQuest.Server.Commands
{
    public class FilesCommands
    {
        [Command("files.list")]
        public async Task List(ICommandHost host)
        {
            foreach (var file in host.Files)
            {
                await host.Send($"{file.Name}\r\n");
                await Task.Delay(200);
            }
        }
    }
}