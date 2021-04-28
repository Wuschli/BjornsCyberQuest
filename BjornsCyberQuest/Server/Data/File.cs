using System.Collections.Generic;

namespace BjornsCyberQuest.Server.Data
{
    public class File
    {
        public string Name { get; set; }
        public string? Text { get; set; }
        public string? YouTube { get; set; }
        public List<string>? Passwords { get; set; }
    }
}