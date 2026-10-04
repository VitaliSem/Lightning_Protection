using LP.Core.Models.LightningProtectors.SingleWire;

namespace LP.Core.Models.LightningProtectors.DoubleWire
{
    public class DoubleWireLightningProtector
    {
        public SingleWireLightningProtector Wire1 { get; set; }
        public SingleWireLightningProtector Wire2 { get; set; }

        public DoubleWireLightningProtector(SingleWireLightningProtector wire1, SingleWireLightningProtector wire2)
        {
            Wire1 = wire1;
            Wire2 = wire2;
        }
    }
}
