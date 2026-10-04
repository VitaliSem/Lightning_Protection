using LP.Core.Enums;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.DoubleRod;
using LP.Core.Models.LightningProtectors.DoubleWire;
using LP.Core.Models.LightningProtectors.SingleRod;
using LP.Core.Models.LightningProtectors.SingleWire;

namespace LP.Core.Interfaces
{
    public interface ILightningProtectionService
    {
        OperationResult<ProtectionArea> GetSingleRodProtectionArea(
            SingleRodLightningProtector protector,
            LightningProtectionReliability reliability,
            double hx);

        OperationResult<ProtectionArea> GetSingleWireProtectionArea(
            SingleWireLightningProtector protector,
            LightningProtectionReliability reliability,
            double hx);

        OperationResult<ProtectionArea> GetDoubleRodProtectionArea(
            DoubleRodLightningProtector protector,
            LightningProtectionReliability reliability,
            double hx);

        OperationResult<ProtectionArea> GetDoubleWireProtectionArea(
            DoubleWireLightningProtector protector,
            LightningProtectionReliability reliability,
            double hx);
    }
}
