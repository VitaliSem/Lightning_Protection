using LP.Core.Models.LightningProtectors.SingleRod;

namespace LP.Core.Models.LightningProtectors.DoubleRod
{
    public class DoubleRodLightningProtector
    {
        public SingleRodLightningProtector Rod1 { get; set; }
        public SingleRodLightningProtector Rod2 { get; set; }

        public DoubleRodLightningProtector(SingleRodLightningProtector rod1, SingleRodLightningProtector rod2)
        {
            Rod1 = rod1;
            Rod2 = rod2;
        }
    }
}
