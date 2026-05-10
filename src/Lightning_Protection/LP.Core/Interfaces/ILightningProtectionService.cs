using LP.Core.Enums;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors;

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
