using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BjornsCyberQuest.Server.Commands;
using BjornsCyberQuest.Server.Data;
using BjornsCyberQuest.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pastel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using File = BjornsCyberQuest.Server.Data.File;

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
        private Config? _config;

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
        public string? HelpText => _config?.HelpText;

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
            LoadConfig();
            CollectCommands();
        }

        public override async Task OnConnectedAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentHost))
                CurrentHost = !string.IsNullOrWhiteSpace(_config?.DefaultHost) ? _config.DefaultHost : "localhost";
            if (string.IsNullOrWhiteSpace(CurrentUser))
                CurrentUser = !string.IsNullOrWhiteSpace(_config?.DefaultUser) ? _config.DefaultUser : string.Empty;

            await WriteLine();
            await Task.Delay(100);
            await WriteLine("connected...");
            await Task.Delay(100);
            await WriteLine();
            await Task.Delay(100);

            if (!string.IsNullOrWhiteSpace(_config?.StartupText))
            {
                var lines = Regex.Split(_config.StartupText, "\r\n|\r|\n");
                foreach (var line in lines)
                {
                    await WriteLine(line.Trim());
                    await Task.Delay(100);
                }
            }

            await Ready();
        }

        public async Task ClientToServer(string input)
        {
            var tokens = input.Trim().Split(' ', 2, StringSplitOptions.TrimEntries);
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
                    if (await CheckJson(tokens[0], json))
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

        private async Task<bool> CheckJson(string command, string json)
        {
            json = json.Trim();
            if (!json.StartsWith('{'))
            {
                var index = command.Length + 1;
                await PrintErrorAtPosition($"{command} {json}", index, "Expected opening {");
                return false;
            }

            if (!json.EndsWith('}'))
            {
                var index = command.Length + json.Length + 1;
                await PrintErrorAtPosition($"{command} {json}", index, "Expected matching }");
                return false;
            }

            try
            {
                JToken.Parse(json);
            }
            catch (JsonReaderException e)
            {
                var index = command.Length + e.LinePosition + 1;
                var message = e.Message;
                if (message.StartsWith("Invalid JavaScript property identifier character"))
                    message = $"Invalid character {message[50]}";
                else if (message.StartsWith("Unterminated string. Expected delimiter: "))
                    message = $"Expected matching {message[41]}";
                else if (message.StartsWith("Invalid property identifier character: "))
                    message = $"Invalid character {message[39]}";
                else if (message.StartsWith("Invalid character after parsing property name. Expected '"))
                    message = $"Expected {message[57]} after Key";
                else if (message.StartsWith("Unexpected character encountered while parsing value: "))
                    message = $"Unexpected character in value {message[54]}";
                await PrintErrorAtPosition($"{command} {json}", index, message);
                return false;
            }

            return true;
        }

        private async Task PrintErrorAtPosition(string input, int index, string error)
        {
            await WriteLine(error.Pastel(Color.Red));
            await WriteLine(input.Pastel(Color.Red));
            await WriteLine(new string(' ', index) + "^".Pastel(Color.Red));
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

            prompt = prompt.Pastel(Color.Aqua) + "> ";
            await Clients.Caller.Ready(prompt);
        }

        private void LoadHosts()
        {
            var ymlFiles = Directory.GetFiles("./config/hosts", "*.yml", SearchOption.AllDirectories);
            var yamlFiles = Directory.GetFiles("./config/hosts", "*.yaml", SearchOption.AllDirectories);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            foreach (var hostFile in yamlFiles.Concat(ymlFiles))
            {
                var yaml = System.IO.File.ReadAllText(hostFile);
                var hostname = Path.GetFileNameWithoutExtension(hostFile).ToLower();
                var host = deserializer.Deserialize<Host>(yaml);
                _hosts[hostname] = host;
                _logger.LogInformation($"Registered host {hostname}");
            }
        }

        private void LoadConfig()
        {
            var configFile = "./config/config.yml";
            if (!System.IO.File.Exists(configFile))
                configFile = "./config/config.yaml";
            if (!System.IO.File.Exists(configFile))
                return;
            var yaml = System.IO.File.ReadAllText(configFile);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            _config = deserializer.Deserialize<Config>(yaml);
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