using System.Collections.Generic;

namespace BjornsCyberQuest.Server.Data
{
    public class Host
    {
        public List<User>? Users { get; set; }
        public List<File>? Files { get; set; }
        public List<Mail>? Mails { get; set; }
    }
}