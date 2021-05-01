using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly List<string> _history = new();
        private int _historyIndex;
        private int _cursorPosition;

        private readonly TerminalOptions _options = new()
        {
            CursorBlink = true,
            CursorStyle = CursorStyle.underline,
            RendererType = RendererType.dom,
            FontFamily = "Consolas",
        };

        private HubConnection _hubConnection;
        private string _prompt;
        private string? _youTubeLink;

        private bool IsStartOfLine => _cursorPosition == 0;
        private bool IsEndOfLine => _cursorPosition == _input.Length;

        protected override async Task OnInitializedAsync()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(NavigationManager.ToAbsoluteUri("/terminalhub"))
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<string>(nameof(ITerminalHub.ServerToClient), async output => { await _terminal.Write(output); });
            _hubConnection.On<string>(nameof(ITerminalHub.OpenYouTube), youTubeLink =>
            {
                _youTubeLink = youTubeLink;
                Console.WriteLine(youTubeLink);
                StateHasChanged();
            });
            _hubConnection.On<string>(nameof(ITerminalHub.Ready), async prompt =>
            {
                _ready = true;
                _prompt = prompt;
                await _terminal.Write(prompt);
            });
            _hubConnection.Closed += async e =>
            {
                _ready = false;
                await _terminal.WriteLine("disconnected...");
            };
            _hubConnection.Reconnecting += async e =>
            {
                _ready = false;
                await _terminal.WriteLine("connection lost, reconnecting...");
            };

            await _hubConnection.StartAsync();
        }

        private async Task OnKeyPress(KeyboardEventArgs keyPress)
        {
            if (!_ready)
                return;

            switch (keyPress.Key)
            {
                case "Enter":
                    await Enter();
                    break;

                case "Escape":
                    _youTubeLink = null;
                    StateHasChanged();
                    break;

                case "Backspace":
                    await Backspace();
                    break;

                case "ArrowLeft":
                    await MoveCursorLeft();
                    break;
                case "ArrowRight":
                    await MoveCursorRight();
                    break;
                case "ArrowUp":
                    await HistoryUp();
                    break;
                case "ArrowDown":
                    await HistoryDown();
                    break;
                case "Home":
                    await MoveCursorHome();
                    break;
                case "End":
                    await MoveCursorEnd();
                    break;
                case "PageDown":
                case "PageUp":
                    break;

                case "Insert":
                    break;

                case "Delete":
                    await Delete();
                    break;

                case "F1":
                case "F2":
                case "F3":
                case "F4":
                case "F5":
                case "F6":
                case "F7":
                case "F8":
                case "F9":
                case "F10":
                case "F11":
                case "F12":
                    break;

                default:
                    await Print(keyPress);
                    break;
            }
        }

        private async Task Print(KeyboardEventArgs keyPress)
        {
            var printable = !keyPress.AltKey && !keyPress.CtrlKey && !keyPress.MetaKey;
            if (!printable)
                return;
            _input = _input.Insert(_cursorPosition++, keyPress.Key);
            await _terminal.Write(keyPress.Key);
            var replacement = _input.Substring(_cursorPosition);
            await _terminal.Write($"{replacement} {new string('\b', replacement.Length + 1)}");
        }

        private async Task Enter()
        {
            if (string.IsNullOrWhiteSpace(_input))
                return;
            _ready = false;
            await _hubConnection.SendAsync(nameof(ITerminal.ClientToServer), _input);
            if (_input != _history.LastOrDefault())
                _history.Add(_input);
            _historyIndex = _history.Count;
            _cursorPosition = 0;
            _input = string.Empty;
            await _terminal.WriteLine();
        }

        private async Task Backspace()
        {
            if (IsStartOfLine)
                return;

            _input = _input.Remove(_cursorPosition-- - 1, 1);
            var replacement = _input.Substring(_cursorPosition);
            await _terminal.Write($"\b{replacement} {new string('\b', replacement.Length + 1)}");
        }

        private async Task Delete()
        {
            if (IsEndOfLine)
                return;

            _input = _input.Remove(_cursorPosition, 1);
            var replacement = _input.Substring(_cursorPosition);
            await _terminal.Write($"{replacement} {new string('\b', replacement.Length + 1)}");
        }

        private async Task MoveCursorLeft()
        {
            if (IsStartOfLine)
                return;

            await _terminal.Write("\b");

            _cursorPosition--;
        }

        private async Task MoveCursorRight()
        {
            if (IsEndOfLine)
                return;

            await _terminal.Write("\x1b[C");

            _cursorPosition++;
        }

        private async Task MoveCursorHome()
        {
            while (!IsStartOfLine)
                await MoveCursorLeft();
        }

        private async Task MoveCursorEnd()
        {
            while (!IsEndOfLine)
                await MoveCursorRight();
        }

        private async Task ClearLine()
        {
            await MoveCursorEnd();
            while (!IsStartOfLine)
                await Backspace();
        }

        private async Task WriteNewString(string str)
        {
            await ClearLine();
            await _terminal.Write(str);
            _input = str;
            _cursorPosition = str.Length;
        }

        private async Task HistoryUp()
        {
            if (_historyIndex > 0)
            {
                _historyIndex--;
                await WriteNewString(_history[_historyIndex]);
            }
        }

        private async Task HistoryDown()
        {
            if (_historyIndex < _history.Count)
            {
                _historyIndex++;
                if (_historyIndex == _history.Count)
                    await ClearLine();
                else
                    await WriteNewString(_history[_historyIndex]);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _hubConnection.DisposeAsync();
        }

        private void OnClickCloseYouTube()
        {
            _youTubeLink = null;
            StateHasChanged();
        }
    }
}