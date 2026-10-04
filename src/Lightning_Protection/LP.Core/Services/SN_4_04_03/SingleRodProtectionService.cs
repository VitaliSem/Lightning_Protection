using System;
using LP.Core.Enums;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.SingleRod;

namespace LP.Core.Services.SN_4_04_03
{
    public static class SingleRodProtectionService
    {
        /// <summary>
        /// Calculates the protection area (radius at height hx) for a single rod lightning protector
        /// according to Table 10.1 (IEC/Belarussian standard).
        /// </summary>
        /// <param name="protector">The single rod lightning protector.</param>
        /// <param name="reliability">The required lightning protection reliability level.</param>
        /// <param name="hx">The height at which the protection radius is calculated.</param>
        /// <returns>OperationResult containing the SingleRodProtectionArea at height hx.</returns>
        public static OperationResult<SingleRodProtectionArea> Calculate(
            SingleRodLightningProtector protector,
            LightningProtectionReliability reliability,
            double hx)
        {
            try
            {
                if (protector == null)
                    return OperationResult<SingleRodProtectionArea>.Fail("Protector cannot be null.");

                double h = protector.Height;

                if (h <= 0)
                    return OperationResult<SingleRodProtectionArea>.Fail("Protector height must be greater than zero.");

                if (h > 150)
                    return OperationResult<SingleRodProtectionArea>.Fail("Protector height exceeds maximum supported value of 150 m.");

                if (hx < 0)
                    return OperationResult<SingleRodProtectionArea>.Fail("Calculation height hx cannot be negative.");

                if (hx >= h)
                    return OperationResult<SingleRodProtectionArea>.Fail("Calculation height hx must be less than the protector height.");

                double h0 = CalculateConeHeight(h, reliability);

                if (hx >= h0)
                    return OperationResult<SingleRodProtectionArea>.Ok(new SingleRodProtectionArea(protector.Position, 0));

                double r0 = CalculateConeBaseRadius(h, reliability);

                double rx = r0 * (h0 - hx) / h0;

                var area = new SingleRodProtectionArea(protector.Position, rx);

                return OperationResult<SingleRodProtectionArea>.Ok(area);
            }
            catch (Exception ex)
            {
                return OperationResult<SingleRodProtectionArea>.Fail("An unexpected error occurred during calculation.", ex);
            }
        }

        public static double CalculateConeHeight(double h, LightningProtectionReliability reliability)
        {
            switch (reliability)
            {
                case LightningProtectionReliability.P0_900:
                    // h0 = 0.85h for all ranges (0..150]
                    return 0.85 * h;

                case LightningProtectionReliability.P0_990:
                    if (h <= 100)
                        return 0.8 * h;
                    // 100 < h <= 150
                    return (0.8 - 1e-3 * (h - 100)) * h;

                case LightningProtectionReliability.P0_999:
                    if (h <= 30)
                        return 0.7 * h;
                    if (h <= 100)
                        return (0.7 - 7.14e-4 * (h - 30)) * h;
                    // 100 < h <= 150
                    return (0.65 - 1e-3 * (h - 100)) * h;

                default:
                    throw new ArgumentOutOfRangeException(nameof(reliability), "Unknown reliability level.");
            }
        }

        public static double CalculateConeBaseRadius(double h, LightningProtectionReliability reliability)
        {
            switch (reliability)
            {
                case LightningProtectionReliability.P0_900:
                    if (h <= 100)
                        return 1.2 * h;
                    // 100 < h <= 150
                    return (1.2 - 1e-3 * (h - 100)) * h;

                case LightningProtectionReliability.P0_990:
                    if (h <= 30)
                        return 0.8 * h;
                    if (h <= 100)
                        return (0.8 - 1.43e-3 * (h - 30)) * h;
                    // 100 < h <= 150
                    return 0.7 * h;

                case LightningProtectionReliability.P0_999:
                    if (h <= 30)
                        return 0.6 * h;
                    if (h <= 100)
                        return (0.6 - 1.43e-3 * (h - 30)) * h;
                    // 100 < h <= 150
                    return (0.5 - 2e-3 * (h - 100)) * h;

                default:
                    throw new ArgumentOutOfRangeException(nameof(reliability), "Unknown reliability level.");
            }
        }
    }
}
