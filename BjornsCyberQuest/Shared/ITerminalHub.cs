using System.Threading.Tasks;

namespace BjornsCyberQuest.Shared
{
    public interface ITerminalHub
    {
        Task ReceiveOutput(string output);
    }
}