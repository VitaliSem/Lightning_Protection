using LP.Core.Models.Base;

namespace LP.Core.Models.LightningProtectors.Block
{
    public class LightningProtectorInsertDto
    {
        public Point3D Position { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public double HeightM { get; set; }
        public double? BaseHeightM { get; set; }
        public string Description { get; set; }
    }
}
