using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using BjornsCyberQuest.Server.Commands;
using BjornsCyberQuest.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pastel;

namespace BjornsCyberQuest.Server.Hubs
{
    public class TerminalHub : Hub<ITerminalHub>, ITerminal, ICommandHost
    {
        private readonly ILogger<TerminalHub> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, ParsedCommand> _commands = new();

        public TerminalHub(ILogger<TerminalHub> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
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
            catch (Exception e)
            {
                _logger.LogError(e, $"Error while executing command {input}");

                await Clients.Caller.ServerToClient("Error while executing command!\r\n".Pastel(Color.Red));
            }

            await Ready();
        }

        public async Task Send(string s)
        {
            await Clients.Caller.ServerToClient(s);
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
                    if (parameters.Length != 2)
                    {
                        _logger.LogWarning($"Parameters do not match for command {commandName}");
                        continue;
                    }

                    if (parameters[0].ParameterType != typeof(ICommandHost))
                    {
                        _logger.LogWarning($"Parameters do not match for command {commandName}");
                        continue;
                    }

                    var parameterType = parameters[1].ParameterType;

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
}