using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BjornsCyberQuest.Server.Data;
using BjornsCyberQuest.Server.Hubs;
using Pastel;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using File = System.IO.File;

namespace BjornsCyberQuest.Server.Commands
{
    public class FilesCommands
    {
        [Command("files.list")]
        public async Task List(ICommandHost host)
        {
            foreach (var file in host.Files)
            {
                if (file.Passwords == null || !file.Passwords.Any())
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

            if (file.Passwords != null && file.Passwords.Any())
            {
                if (string.IsNullOrWhiteSpace(parameters.Password))
                {
                    await host.WriteLine("Missing parameter \"password\"".Pastel(Color.Red));
                    await host.WriteLine($"Usage: files.open {"{ file: \"fileName\", password:\"password\"}".Pastel(Color.Aquamarine)}...");
                    return;
                }

                if (!file.Passwords.Contains(parameters.Password))
                {
                    await host.WriteLine("Invalid password!".Pastel(Color.Red));
                    return;
                }
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

            if (!string.IsNullOrWhiteSpace(file.Sequence) && File.Exists($"./config/{file.Sequence}"))
            {
                await ParseSequence(host, file);
                return;
            }

            await host.WriteLine($"File {parameters.File} is empty.".Pastel(Color.Yellow));
        }

        private async Task ParseSequence(ICommandHost host, Data.File file)
        {
            var reader = new StreamReader($"./config/{file.Sequence}");
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var parser = new Parser(reader);
            parser.Consume<StreamStart>();

            string? speaker = null;
            string speakerColor = "68C355";
            bool isNewLine = true;

            while (parser.Accept<DocumentStart>(out _))
            {
                var step = deserializer.Deserialize<SequenceStep>(parser);

                if (step.Speaker != null)
                    speaker = step.Speaker;
                if (step.SpeakerColor != null)
                    speakerColor = step.SpeakerColor;

                if (!string.IsNullOrWhiteSpace(speaker) && isNewLine)
                    await host.Write($"{speaker}: ".Pastel(speakerColor));

                foreach (var c in step.Text)
                {
                    await host.Write(c.ToString().Pastel(step.Color));
                    await Task.Delay(1000 / step.Speed);
                }

                //await host.Write(step.Text.Pastel(step.Color));
                if (step.LineBreak)
                {
                    isNewLine = true;
                    await host.WriteLine();
                }
                else
                {
                    isNewLine = false;
                }

                await Task.Delay(step.Delay);
            }
        }
    }

    public class FilesOpenParameters
    {
        public string? File { get; set; }
        public string? Password { get; set; }
    }
}