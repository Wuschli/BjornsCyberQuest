using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BjornsCyberQuest.Server.Hubs
{
    public class ParsedCommand
    {
        public MethodInfo Method { get; }
        public object Instance { get; }
        public Type? ParameterType { get; }
        public int ParameterCount { get; }

        public ParsedCommand(MethodInfo method, object instance, int parameterCount, Type? parameterType)
        {
            Method = method;
            Instance = instance;
            ParameterCount = parameterCount;
            ParameterType = parameterType;
        }

        public async Task Execute(ICommandHost host, string? json)
        {
            object? parameter;

            if (string.IsNullOrWhiteSpace(json) || ParameterType == null)
                parameter = null;
            else
                parameter = JsonConvert.DeserializeObject(json, ParameterType);

            var parameters = new List<object?> {host};
            if (ParameterCount == 2)
                parameters.Add(parameter);
            if (Method.Invoke(Instance, parameters.ToArray()) is Task task)
                await task;
        }
    }
}