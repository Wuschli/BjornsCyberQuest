using System;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BjornsCyberQuest.Server.Hubs
{
    public class ParsedCommand
    {
        private readonly JsonSerializerSettings _settings;
        public MethodInfo Method { get; init; }
        public object Instance { get; init; }
        public Type ParameterType { get; init; }

        public ParsedCommand()
        {
            _settings = new JsonSerializerSettings
            {
            };
        }

        public async Task Execute(ICommandHost host, string json)
        {
            object? parameter;

            if (string.IsNullOrWhiteSpace(json))
                parameter = null;
            else
                parameter = JsonConvert.DeserializeObject(json, ParameterType/*, _settings*/);

            var parameters = new[] {host, parameter};
            if (Method.Invoke(Instance, parameters) is Task task)
                await task;
        }
    }
}