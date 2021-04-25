using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BjornsCyberQuest.Server.Commands;
using BjornsCyberQuest.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pastel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BjornsCyberQuest.Server.Hubs
{
    public class TerminalHub : Hub<ITerminalHub>, ITerminal, ICommandHost
    {
        private readonly ILogger<TerminalHub> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, ParsedCommand> _commands = new();
        private readonly Dictionary<string, Host> _hosts = new();

        public IEnumerable<File> Files
        {
            get
            {
                if (_hosts.TryGetValue(CurrentHost, out var host))
                    return host.Files;
                return Enumerable.Empty<File>();
            }
        }

        private string CurrentHost
        {
            get
            {
                if (Context.Items.TryGetValue("host", out var host))
                    return host?.ToString() ?? string.Empty;
                return string.Empty;
            }
        }

        public TerminalHub(ILogger<TerminalHub> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            LoadHosts();
            CollectCommands();
        }

        public override async Task OnConnectedAsync()
        {
            if (!Context.Items.TryGetValue("host", out var host))
                Context.Items["host"] = "localhost";
            if (!Context.Items.TryGetValue("directory", out var directory))
                Context.Items["directory"] = "~";
            await Ready();
        }

        public async Task ClientToServer(string input)
        {
            //await Clients.Caller.ServerToClient(input.Pastel(Color.Aqua) + "\r\n");
            var tokens = input.Split(' ', 2, StringSplitOptions.TrimEntries);
            if (tokens.Length == 0)
            {
                await Ready();
                return;
            }

            if (!_commands.TryGetValue(tokens[0], out var command))
            {
                await Clients.Caller.ServerToClient($"Command {tokens[0].Pastel(Color.Aqua)} not found!\r\n");
                await Ready();
                return;
            }

            try
            {
                if (tokens.Length >= 2)
                {
                    var json = tokens[1];
                    await command.Execute(this, json);
                }
                else
                {
                    await command.Execute(this, null);
                }
            }
            catch (JsonSerializationException e)
            {
                _logger.LogError(e, $"Error while executing command {input}");

                await Clients.Caller.ServerToClient($"{e.Message}\r\n".Pastel(Color.Red));
            }
            catch (JsonReaderException e)
            {
                _logger.LogError(e, $"Error while executing command {input}");

                await Clients.Caller.ServerToClient($"{e.Message}\r\n".Pastel(Color.Red));
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error while executing command {input}");

                await Clients.Caller.ServerToClient("Error while executing command!\r\n".Pastel(Color.Red));
            }

            await Ready();
        }


        public async Task Write(string s)
        {
            await Clients.Caller.ServerToClient(s);
        }

        public async Task WriteLine(string s)
        {
            await Clients.Caller.ServerToClient(s + "\r\n");
        }

        public async Task OpenYouTube(string youTubeLink)
        {
            await Clients.Caller.OpenYouTube(youTubeLink);
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

        private void LoadHosts()
        {
            var ymlFiles = Directory.GetFiles("./hosts", "*.yml", SearchOption.AllDirectories);
            var yamlFiles = Directory.GetFiles("./hosts", "*.yaml", SearchOption.AllDirectories);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            foreach (var hostFile in yamlFiles.Concat(ymlFiles))
            {
                var yaml = System.IO.File.ReadAllText(hostFile);
                var hostname = Path.GetFileNameWithoutExtension(hostFile);
                var host = deserializer.Deserialize<Host>(yaml);
                _hosts[hostname] = host;
            }
        }

        private void CollectCommands()
        {
            var commandAttributeType = typeof(CommandAttribute);
            var commandMethods = commandAttributeType.Assembly
                .GetTypes()
                .SelectMany(t => t.GetMethods())
                .Where(method => method.GetCustomAttributes(commandAttributeType, false).Any())
                .ToList();

            foreach (var commandMethod in commandMethods)
            {
                foreach (var commandAttribute in commandMethod.GetCustomAttributes(commandAttributeType, false).OfType<CommandAttribute>())
                {
                    var commandName = commandAttribute.Name;

                    if (commandMethod.ReturnType != typeof(Task))
                    {
                        _logger.LogWarning($"Command {commandName} does not return Task");
                        continue;
                    }

                    var parameters = commandMethod.GetParameters();
                    if (parameters.Length != 1 && parameters.Length != 2)
                    {
                        _logger.LogWarning($"Parameters do not match for command {commandName}");
                        continue;
                    }

                    if (parameters[0].ParameterType != typeof(ICommandHost))
                    {
                        _logger.LogWarning($"Parameters do not match for command {commandName}");
                        continue;
                    }

                    Type? parameterType = null;
                    if (parameters.Length == 2)
                        parameterType = parameters[1].ParameterType;

                    var instanceType = commandMethod.DeclaringType;
                    if (instanceType == null)
                    {
                        _logger.LogWarning($"Declaring type not found for {commandName}");
                        continue;
                    }

                    var instance = ActivatorUtilities.CreateInstance(_serviceProvider, instanceType);
                    var parsedCommand = new ParsedCommand
                    {
                        Method = commandMethod,
                        Instance = instance,
                        ParameterType = parameterType
                    };
                    if (_commands.ContainsKey(commandName))
                    {
                        _logger.LogWarning($"Command {commandName} cannot be registered twice");
                        continue;
                    }

                    _commands.Add(commandName, parsedCommand);
                    _logger.LogInformation($"Register Command {commandName}");
                }
            }
        }
    }

    public class Host
    {
        public List<User> Users { get; set; }
        public List<File> Files { get; set; }
        public List<Email> Mails { get; set; }
    }

    public class User
    {
        public string UserName { get; set; }
        public string? Password { get; set; }
    }

    public class Email
    {
    }

    public class File
    {
        public string Name { get; set; }
        public string? Text { get; set; }
        public string? YouTube { get; set; }
    }
}