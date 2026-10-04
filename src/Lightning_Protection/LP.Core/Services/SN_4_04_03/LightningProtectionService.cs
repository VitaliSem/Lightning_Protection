using System;
using System.Collections.Generic;
using LP.Core.Enums;
using LP.Core.Interfaces;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.DoubleRod;
using LP.Core.Models.LightningProtectors.DoubleWire;
using LP.Core.Models.LightningProtectors.SingleRod;
using LP.Core.Models.LightningProtectors.SingleWire;

namespace LP.Core.Services.SN_4_04_03
{
    public class LightningProtectionService : ILightningProtectionService
    {
        public OperationResult<ProtectionArea> GetSingleRodProtectionArea(
            SingleRodLightningProtector protector,
            LightningProtectionReliability reliability,
            double hx)
        {
            var calculationResult = SingleRodProtectionService.Calculate(protector, reliability, hx);

            if (!calculationResult.IsSuccess)
                return OperationResult<ProtectionArea>.Fail(calculationResult.Message, calculationResult.Exception);

            return OperationResult<ProtectionArea>.Ok(new ProtectionArea
            {
                Points = new List<ProtectionPoint>
                { 
                    new ProtectionPoint(calculationResult.Result.Center, calculationResult.Result.Radius, isLastPoint: true)
                }
            });
        }

        public OperationResult<ProtectionArea> GetSingleWireProtectionArea(
            SingleWireLightningProtector protector,
            LightningProtectionReliability reliability,
            double hx)
        {
            var calculationResult = SingleWireProtectionService.Calculate(protector, reliability, hx);

            if (!calculationResult.IsSuccess)
                return OperationResult<ProtectionArea>.Fail(calculationResult.Message, calculationResult.Exception);

            return SingleWireProtectionService.GetProtectionArea(calculationResult.Result);
        }

        public OperationResult<ProtectionArea> GetDoubleRodProtectionArea(
            DoubleRodLightningProtector protector,
            LightningProtectionReliability reliability,
            double hx)
        {
            var calcResult = DoubleRodProtectionService.Calculate(protector, reliability, hx);

            if (!calcResult.IsSuccess)
                return OperationResult<ProtectionArea>.Fail(calcResult.Message, calcResult.Exception);

            return DoubleRodProtectionService.GetProtectionArea(calcResult.Result);
        }

        public OperationResult<ProtectionArea> GetDoubleWireProtectionArea(
            DoubleWireLightningProtector protector,
            LightningProtectionReliability reliability,
            double hx)
        {
            throw new NotImplementedException("Double wire protection area calculation is not implemented yet.");
        }
    }
}
