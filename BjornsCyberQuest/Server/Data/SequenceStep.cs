namespace BjornsCyberQuest.Server.Data
{
    public class SequenceStep
    {
        public string? Speaker { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Color { get; set; } = "FFFFFF";
        public int Delay { get; set; } = 1000;
        public bool LineBreak { get; set; } = true;
        public string? SpeakerColor { get; set; }
        public int Speed { get; set; } = 20;
    }
}