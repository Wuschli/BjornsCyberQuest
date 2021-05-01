using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BjornsCyberQuest.Server.Hubs;
using Pastel;

namespace BjornsCyberQuest.Server.Commands
{
    public class MailsCommands
    {
        [Command("mails.list")]
        public async Task ListMails(ICommandHost host)
        {
            var id = 0;
            await host.WriteLine($"ID\t\t{"Date".PadRight(15)}\t\t\t{"From".PadRight(50)}\t\tSubject");
            foreach (var mail in host.Mails)
            {
                await host.WriteLine($"[{id++.ToString().Pastel(Color.Coral)}]\t{mail.Timestamp.ToString("yyyy-MMM-dd ddd").Pastel(Color.Azure)}\t\t\t{mail.From.PadRight(50).Pastel(Color.Aqua)}\t\t{mail.Subject}");
                await Task.Delay(200);
            }
        }

        [Command("mails.open")]
        public async Task OpenMail(ICommandHost host, OpenMailParameters? parameters)
        {
            if (parameters?.Id == null)
            {
                await host.WriteLine($"Usage: mails.open {"{ id: 0}".Pastel(Color.Aquamarine)}...");
                return;
            }

            var mails = host.Mails.ToList();
            if (mails.Count <= parameters.Id || parameters.Id < 0)
            {
                await host.WriteLine($"Invalid ID {parameters.Id}".Pastel(Color.Red));
                return;
            }

            var mail = mails[parameters.Id.Value];
            await host.WriteLine($"Date: {mail.Timestamp}".Pastel(Color.Gray));
            await Task.Delay(100);
            await host.WriteLine($"From: {mail.From}".Pastel(Color.Gray));
            await Task.Delay(100);
            await host.WriteLine($"To: {mail.To}".Pastel(Color.Gray));
            await Task.Delay(100);
            await host.WriteLine($"Subject: {mail.Subject}".Pastel(Color.Coral));
            await Task.Delay(100);
            await host.WriteLine();
            await Task.Delay(100);

            var lines = Regex.Split(mail.Text, "\r\n|\r|\n");
            foreach (var line in lines)
            {
                await host.WriteLine(line.Trim());
                await Task.Delay(100);
            }

            await host.WriteLine();
        }

        public class OpenMailParameters
        {
            public int? Id { get; set; }
        }
    }
}