using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using BjornsCyberQuest.Server.Hubs;
using Pastel;

namespace BjornsCyberQuest.Server.Commands
{
    public class FilesCommands
    {
        [Command("files.list")]
        public async Task List(ICommandHost host)
        {
            foreach (var file in host.Files)
            {
                if (file.Password == null)
                    await host.WriteLine($"  {file.Name}");
                else
                    await host.WriteLine($"{"E".Pastel(Color.Coral)} {file.Name}");
                await Task.Delay(200);
            }
        }

        [Command("files.open")]
        public async Task Open(ICommandHost host, FilesOpenParameters? parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters?.File))
            {
                await host.WriteLine($"Usage: files.open {"{ file: \"fileName\"}".Pastel(Color.Aquamarine)}...");
                return;
            }

            var file = host.Files.FirstOrDefault(f => f.Name == parameters.File);
            if (file == null)
            {
                await host.WriteLine($"File {parameters.File} not found!".Pastel(Color.Red));
                return;
            }

            if (file.Password != null && parameters.Password == null)
            {
                await host.WriteLine("Missing parameter \"password\"".Pastel(Color.Red));
                await host.WriteLine($"Usage: files.open {"{ file: \"fileName\", password:\"password\"}".Pastel(Color.Aquamarine)}...");
                return;
            }

            if (file.Password != null && file.Password != parameters.Password)
            {
                await host.WriteLine("Wrong password!".Pastel(Color.Red));
                return;
            }

            if (!string.IsNullOrWhiteSpace(file.Text))
            {
                await host.WriteLine(file.Text);
                return;
            }

            if (!string.IsNullOrWhiteSpace(file.YouTube))
            {
                await host.OpenYouTube(file.YouTube);
                return;
            }

            await host.WriteLine($"File {parameters.File} is empty.".Pastel(Color.Yellow));
        }
    }

    public class FilesOpenParameters
    {
        public string? File { get; set; }
        public string? Password { get; set; }
    }
}