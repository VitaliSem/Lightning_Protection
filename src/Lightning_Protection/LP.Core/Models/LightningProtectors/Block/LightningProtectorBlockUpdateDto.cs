namespace LP.Core.Models.LightningProtectors.Block
{
    public class LightningProtectorBlockUpdateDto
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public double HeightM { get; set; }
        public double? BaseHeightM { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
}
