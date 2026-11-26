using LP.Core.Models.Base;

namespace LP.Core.Models.LightningProtectors.Block
{
    public class LightningProtectorListEntry
    {
        public LightningProtectorBlockData Data { get; set; }
        public Point3D Position { get; set; }
        public string LayoutName { get; set; }

        public LightningProtectorListEntry(LightningProtectorBlockData data, Point3D position, string layoutName = null)
        {
            Data = data;
            Position = position;
            LayoutName = layoutName;
        }
    }
}
