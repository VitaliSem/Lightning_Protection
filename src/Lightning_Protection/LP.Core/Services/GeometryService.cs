using System;
using LP.Core.Models.Base;

namespace LP.Core.Services
{
    public static class GeometryService
    {
        public static double Get2DDistance(Point3D point1, Point3D point2)
        {
            double dx = point2.X - point1.X;
            double dy = point2.Y - point1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static double GetAngleToHorizontal(Point3D point1, Point3D point2)
        {
            double dx = point2.X - point1.X;
            double dy = point2.Y - point1.Y;
            return Math.Atan2(dy, dx);
        }

        public static double GetTangentDeviationAngle(Point3D center1, double radius1, Point3D center2, double radius2)
        {
            double d = Get2DDistance(center1, center2);

            if (d <= 0d)
                throw new ArgumentException("Tangent deviation angle is undefined when the centers coincide.", nameof(center2));

            double cosAlpha = (radius1 - radius2) / d;
            return Math.Acos(cosAlpha);
        }
    }
}
