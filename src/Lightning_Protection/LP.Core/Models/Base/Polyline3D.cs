using System.Collections.Generic;

namespace LP.Core.Models.Base
{
    public class Polyline3D
    {
        public List<Point3D> Points { get; set; } = new List<Point3D>();
        public bool IsClosed { get; set; }

        public Polyline3D()
        {
            IsClosed = false;
        }

        public Polyline3D(List<Point3D> points, bool isClosed = false)
        {
            Points = points;
            IsClosed = isClosed;
        }
    }
}
