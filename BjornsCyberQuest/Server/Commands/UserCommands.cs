using System.Drawing;
using System.Threading.Tasks;
using BjornsCyberQuest.Server.Hubs;
using Pastel;

namespace BjornsCyberQuest.Server.Commands
{
    public class UserCommands
    {
        [Command("user.create")]
        public async Task CreateUser(ICommandHost host, CreateUserParameters? parameters)
        {
            if (parameters?.Name == null)
            {
                await host.WriteLine($"Usage: user.create {"{ name: \"userName\"}".Pastel(Color.Aquamarine)}...");
            }
            else
            {
                await host.WriteLine($"Creating User {parameters.Name?.Pastel(Color.Aquamarine)}...");
            }
        }
    }

    public class CreateUserParameters
    {
        public string Name { get; set; }
    }
}