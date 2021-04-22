using System.Threading.Tasks;

namespace BjornsCyberQuest.Shared
{
    public interface ITerminalHub
    {
        Task ServerToClient(string output);
        Task Ready(string prompt);
    }
}