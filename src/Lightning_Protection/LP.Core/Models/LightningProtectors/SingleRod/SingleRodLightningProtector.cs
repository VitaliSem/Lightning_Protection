using LP.Core.Models.Base;

namespace LP.Core.Models.LightningProtectors.SingleRod
{
    public class SingleRodLightningProtector
    {
        public Point3D Position { get; set; }
        public double Height { get; set; }

        public SingleRodLightningProtector(Point3D position, double height)
        {
            Position = position;
            Height = height;
        }
    }
}
