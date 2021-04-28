using System.Collections.Generic;

namespace BjornsCyberQuest.Server.Data
{
    public class User
    {
        public string UserName { get; set; }
        public List<string>? Passwords { get; set; }
    }
}