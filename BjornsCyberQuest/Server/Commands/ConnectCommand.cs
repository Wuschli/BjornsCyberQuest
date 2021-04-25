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
            await Task.Delay(500);

            if (h.Users != null && h.Users.Any())
            {
                if (string.IsNullOrWhiteSpace(parameters.User))
                {
                    await host.WriteLine();
                    await host.WriteLine("Missing parameter \"user\"".Pastel(Color.Red));
                    await host.WriteLine($"Usage: connect {"{ host: \"hostname\", user: \"username\"}".Pastel(Color.Aquamarine)}...");
                    return;
                }

                var user = h.Users.FirstOrDefault(u => u.UserName == parameters.User);
                if (user == null)
                {
                    await host.WriteLine();
                    await host.WriteLine($"Unknown user {parameters.User}".Pastel(Color.Red));
                    return;
                }

                if (!string.IsNullOrWhiteSpace(user.Password))
                {
                    if (string.IsNullOrWhiteSpace(parameters.Password))
                    {
                        await host.WriteLine();
                        await host.WriteLine("Missing parameter \"password\"".Pastel(Color.Red));
                        await host.WriteLine($"Usage: connect {"{ host: \"hostname\", user: \"username\", password: \"password\"}".Pastel(Color.Aquamarine)}...");
                        return;
                    }

                    if (user.Password != parameters.Password)
                    {
                        await host.WriteLine();
                        await host.WriteLine("Invalid password".Pastel(Color.Red));
                        return;
                    }
                }

                host.CurrentUser = user.UserName;
            }
            else
            {
                host.CurrentUser = null;
            }

            host.CurrentHost = parameters.Host;
            await host.WriteLine(" connected!");
        }
    }

    public class ConnectCommandParameters
    {
        public string? Host { get; set; }
        public string? User { get; set; }
        public string? Password { get; set; }
    }
}