using System.Threading.Tasks;

namespace BjornsCyberQuest.Server.Hubs
{
    public interface ICommandHost
    {
        Task Send(string s);
    }
}