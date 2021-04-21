using System;
using System.Threading.Tasks;
using BjornsCyberQuest.Shared;
using Blazor.Extensions.XTerm;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;

namespace BjornsCyberQuest.Client.Pages
{
    public partial class Terminal
    {
        private XTerm _terminal;
        private string _input = string.Empty;
        private bool _ready;

        private readonly TerminalOptions _options = new()
        {
            CursorBlink = true,
            CursorStyle = CursorStyle.underline,
            RendererType = RendererType.dom,
            FontFamily = "Consolas",
        };

        private HubConnection _hubConnection;

        protected override async Task OnInitializedAsync()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(NavigationManager.ToAbsoluteUri("/terminalhub"))
                .Build();

            _hubConnection.On<string>(nameof(ITerminalHub.ReceiveOutput), async output => { await _terminal.Write(output); });
            _hubConnection.On<string>(nameof(ITerminalHub.Ready), async prompt =>
            {
                _ready = true;
                await _terminal.Write(prompt);
            });

            await _hubConnection.StartAsync();
        }

        private async Task OnKeyPress(KeyboardEventArgs keyPress)
        {
            if (!_ready)
                return;

            Console.WriteLine(keyPress.Key);
            var printable = !keyPress.AltKey && !keyPress.CtrlKey && !keyPress.MetaKey;

            if (keyPress.Key == "Enter")
            {
                _ready = false;
                await _hubConnection.SendAsync(nameof(ITerminal.SendInput), _input);
                _input = string.Empty;
                await _terminal.WriteLine();
                return;
            }

            if (keyPress.Key == "Backspace")
            {
                if (_input.Length > 0)
                {
                    _input = _input.Remove(_input.Length - 1);
                    await _terminal.Write("\b \b");
                }

                return;
            }

            if (printable)
            {
                _input += keyPress.Key;
                await _terminal.Write(keyPress.Key);
                return;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _hubConnection.DisposeAsync();
        }
    }
}