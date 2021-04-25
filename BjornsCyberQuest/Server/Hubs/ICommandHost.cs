using System.Collections.Generic;
using System.Threading.Tasks;
using BjornsCyberQuest.Server.Data;

namespace BjornsCyberQuest.Server.Hubs
{
    public interface ICommandHost
    {
        IEnumerable<File> Files { get; }
        IEnumerable<Mail> Mails { get; }
        Task Write(string s);
        Task WriteLine(string? s = null);
        Task OpenYouTube(string youTubeLink);
    }
}