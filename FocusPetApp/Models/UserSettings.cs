namespace FocusPetApp.Models
{
    public class UserSettings
    {
        public int WorkMinutes { get; set; } = 25;
        public int BreakCount { get; set; } = 2;
        public string PetColor { get; set; } = "Mint";
        public bool HatOn { get; set; } = false;
    }
}

