using LP.Core.Models.Base;

namespace LP.Core.Models.LightningProtectors.SingleWire
{
    public class SingleWireLightningProtector
    {
        public Point3D PinPoint1 { get; set; }
        public double Height1 { get; set; }

        public Point3D PinPoint2 { get; set; }
        public double Height2 { get; set; }

        public SingleWireLightningProtector(Point3D pinPoint1, double height1, Point3D pinPoint2, double height2)
        {
            PinPoint1 = pinPoint1;
            Height1 = height1;
            PinPoint2 = pinPoint2;
            Height2 = height2;
        }
    }
}
