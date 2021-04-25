using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BjornsCyberQuest.Server.Hubs
{
    public class ParsedCommand
    {
        private readonly JsonSerializerSettings _settings;
        public MethodInfo Method { get; init; }
        public object Instance { get; init; }
        public Type? ParameterType { get; init; }

        public ParsedCommand()
        {
            _settings = new JsonSerializerSettings
            {
            };
        }

        public async Task Execute(ICommandHost host, string json)
        {
            object? parameter;

            if (string.IsNullOrWhiteSpace(json) || ParameterType == null)
                parameter = null;
            else
                parameter = JsonConvert.DeserializeObject(json, ParameterType /*, _settings*/);

            var parameters = new List<object?> {host};
            if (parameter != null)
                parameters.Add(parameter);
            if (Method.Invoke(Instance, parameters.ToArray()) is Task task)
                await task;
        }
    }
}