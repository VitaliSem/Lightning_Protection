using LP.Core.Models.Base;

namespace LP.Core.Models.LightningProtectors.SingleRod
{
    public class SingleRodProtectionArea
    {
        public Point3D Center { get; set; }
        public double Radius { get; set; }

        public SingleRodProtectionArea(Point3D center, double radius)
        {
            Center = center;
            Radius = radius;
        }
    }
}
