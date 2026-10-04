using LP.Core.Models.LightningProtectors.SingleRod;

namespace LP.Core.Models.LightningProtectors.DoubleRod
{
    public class DoubleRodProtectionArea
    {
        public SingleRodProtectionArea Rod1Area { get; }
        public SingleRodProtectionArea Rod2Area { get; }
        public double H0 { get; }
        public double R0 { get; }
        public double L { get; }
        public double Lmax { get; }
        public double Lc { get; }
        public double Hc { get; }
        public bool IsCombined { get; }
        public double Rx { get; }
        public double Lx { get; }
        public double Rcx { get; }
        public double Lx1 { get; }
        public double Lx2 { get; }

        public DoubleRodProtectionArea(
            SingleRodProtectionArea rod1Area,
            SingleRodProtectionArea rod2Area,
            double h0,
            double r0,
            double l,
            double lmax,
            double lc,
            double hc,
            bool isCombined,
            double rx,
            double lx,
            double rcx,
            double lx1 = 0,
            double lx2 = 0)
        {
            Rod1Area = rod1Area;
            Rod2Area = rod2Area;
            H0 = h0;
            R0 = r0;
            L = l;
            Lmax = lmax;
            Lc = lc;
            Hc = hc;
            IsCombined = isCombined;
            Rx = rx;
            Lx = lx;
            Rcx = rcx;
            Lx1 = lx1;
            Lx2 = lx2;
        }
    }
}