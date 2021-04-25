using System;

namespace BjornsCyberQuest.Server.Data
{
    public class Mail
    {
        public DateTime Timestamp { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        public string Text { get; set; }
    }
}