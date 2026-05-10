namespace LP.Core.Models.Base
{
    public class ProtectionPoint
    {
        public Point3D Point { get; set; }
        public double? Radius { get; set; }
        public bool IsLastPoint { get; set; }

        public ProtectionPoint(Point3D point, double? radius = null, bool isLastPoint = false)
        {
            Point = point;
            Radius = radius;
            IsLastPoint = isLastPoint;
        }
    }
}
