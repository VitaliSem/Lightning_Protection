namespace LP.Core.Models.LightningProtectors.Block
{
    public class LightningProtectorBlockData
    {
        public int Number { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public double HeightM { get; set; }
        public double? BaseHeightM { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }

        public LightningProtectorBlockData(int number, string name, string type, double heightM, bool isActive,
            double? baseHeightM = null, string description = null, string title = null)
        {
            Number = number;
            Name = name;
            Type = type;
            HeightM = heightM;
            IsActive = isActive;
            BaseHeightM = baseHeightM;
            Description = description;
            Title = title;
        }
    }
}
