using System.Collections.Generic;

namespace LP.Core.Models.Base
{
    public class ProtectionArea
    {
        public List<ProtectionPoint> Points { get; set; }

        public ProtectionArea()
        {
            Points = new List<ProtectionPoint>();
        }

        public ProtectionArea(List<ProtectionPoint> points)
        {
            Points = points;
        }
    }
}
