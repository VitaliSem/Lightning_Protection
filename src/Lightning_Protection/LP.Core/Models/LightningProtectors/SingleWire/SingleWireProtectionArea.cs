using LP.Core.Models.Base;

namespace LP.Core.Models.LightningProtectors.SingleWire
{
    public class SingleWireProtectionArea
    {
        public Point3D PinPoint1 { get; set; }
        public double Radius1 { get; set; }

        public Point3D PinPoint2 { get; set; }
        public double Radius2 { get; set; }

        public SingleWireProtectionArea(Point3D pinPoint1, double radius1, Point3D pinPoint2, double radius2)
        {
            PinPoint1 = pinPoint1;
            Radius1 = radius1;
            PinPoint2 = pinPoint2;
            Radius2 = radius2;
        }
    }
}
