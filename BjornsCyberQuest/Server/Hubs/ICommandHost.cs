using System.Collections.Generic;
using System.Threading.Tasks;

namespace BjornsCyberQuest.Server.Hubs
{
    public interface ICommandHost
    {
        IEnumerable<File> Files { get; }
        Task Send(string s);
    }
}