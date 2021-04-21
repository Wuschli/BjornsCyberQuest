using System.Threading.Tasks;

namespace BjornsCyberQuest.Shared
{
    public interface ITerminal
    {
        Task SendInput(string input);
    }
}