using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BjornsCyberQuest.Server.Commands;
using BjornsCyberQuest.Server.Data;
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
        private const string HostKey = "host";
        private const string UserKey = "user";
        private readonly ILogger<TerminalHub> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, ParsedCommand> _commands = new();
        private readonly Dictionary<string, Host> _hosts = new();

        public IEnumerable<Data.File> Files
        {
            get
            {
                if (_hosts.TryGetValue(CurrentHost, out var host) && host.Files != null)
                    return host.Files;
                return Enumerable.Empty<Data.File>();
            }
        }

        public IEnumerable<Mail> Mails
        {
            get
            {
                if (_hosts.TryGetValue(CurrentHost, out var host) && host.Mails != null)
                    return host.Mails;
                return Enumerable.Empty<Mail>();
            }
        }

        public IEnumerable<string> KnownHosts => _hosts.Keys;

        public string CurrentHost
        {
            get
            {
                if (Context.Items.TryGetValue(HostKey, out var host))
                    return host?.ToString() ?? string.Empty;
                return string.Empty;
            }
            set => Context.Items[HostKey] = value;
        }

        public string? CurrentUser
        {
            get
            {
                if (Context.Items.TryGetValue(UserKey, out var user))
                    return user?.ToString() ?? string.Empty;
                return string.Empty;
            }
            set => Context.Items[UserKey] = value;
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
            if (string.IsNullOrWhiteSpace(CurrentHost))
                CurrentHost = "localhost";
            await Ready();
        }

        public async Task ClientToServer(string input)
        {
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

        public async Task WriteLine(string? s = null)
        {
            if (s != null)
                await Clients.Caller.ServerToClient(s + "\r\n");
            else
                await Clients.Caller.ServerToClient("\r\n");
        }

        public async Task OpenYouTube(string youTubeLink)
        {
            await Clients.Caller.OpenYouTube(youTubeLink);
        }

        public Host? GetHost(string hostname)
        {
            if (!_hosts.TryGetValue(hostname, out var host))
                return null;
            return host;
        }

        private async Task Ready()
        {
            var prompt = "";
            if (!string.IsNullOrWhiteSpace(CurrentUser))
                prompt += $"{CurrentUser}@";

            if (Context.Items.TryGetValue(HostKey, out var host))
                prompt += $"{host}";

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
                    var parsedCommand = new ParsedCommand(commandMethod, instance, parameters.Length, parameterType);

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
}