using System;
using System.Collections.Generic;
using LP.Core.Enums;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.SingleWire;

namespace LP.Core.Services.SN_4_04_03
{
    public static class SingleWireProtectionService
    {
        /// <summary>
        /// Calculates the protection area for a single wire lightning protector
        /// at a specified height for both pinning points, according to Table 10.2.
        /// </summary>
        /// <param name="protector">The single wire lightning protector.</param>
        /// <param name="reliability">The required lightning protection reliability level.</param>
        /// <param name="hx">The height at which the protection radii are calculated.</param>
        /// <returns>OperationResult containing the SingleWireProtectionArea.</returns>
        public static OperationResult<SingleWireProtectionArea> Calculate(
            SingleWireLightningProtector protector,
            LightningProtectionReliability reliability,
            double hx)
        {
            try
            {
                if (protector == null)
                    return OperationResult<SingleWireProtectionArea>.Fail("Protector cannot be null.");

                if (protector.Height1 <= 0 || protector.Height2 <= 0)
                    return OperationResult<SingleWireProtectionArea>.Fail("Protector heights must be greater than zero.");

                if (protector.Height1 > 150 || protector.Height2 > 150)
                    return OperationResult<SingleWireProtectionArea>.Fail("Protector heights exceed maximum supported value of 150 m.");

                if (hx < 0)
                    return OperationResult<SingleWireProtectionArea>.Fail("Calculation height hx cannot be negative.");

                if (hx >= protector.Height1 || hx >= protector.Height2)
                    return OperationResult<SingleWireProtectionArea>.Fail("Calculation height hx must be less than both protector heights.");

                double rx1 = CalculateRadius(protector.Height1, reliability, hx);
                double rx2 = CalculateRadius(protector.Height2, reliability, hx);

                if (rx1 == 0 && rx2 == 0)
                    return OperationResult<SingleWireProtectionArea>.Fail("At the specified height hx, both protection radii are zero; no protection area exists.");

                var area = new SingleWireProtectionArea(protector.PinPoint1, rx1, protector.PinPoint2, rx2);

                return OperationResult<SingleWireProtectionArea>.Ok(area);
            }
            catch (Exception ex)
            {
                return OperationResult<SingleWireProtectionArea>.Fail("An unexpected error occurred during calculation.", ex);
            }
        }

        /// <summary>
        /// Builds the ProtectionArea outline for a single wire protector from two arcs and two external tangent lines.
        /// The outline consists of:
        ///   - tangent point on circle 1 (upper tangent)
        ///   - tangent point on circle 2 (upper tangent)
        ///   - tangent point on circle 2 (lower tangent)
        ///   - tangent point on circle 1 (lower tangent)  [IsLastPoint = true to close the shape]
        /// </summary>
        /// <param name="wireArea">The SingleWireProtectionArea containing the pin points and radii.</param>
        /// <returns>OperationResult containing the ProtectionArea.</returns>
        public static OperationResult<ProtectionArea> GetProtectionArea(SingleWireProtectionArea wireArea)
        {
            try
            {
                if (wireArea == null)
                    return OperationResult<ProtectionArea>.Fail("Protection area cannot be null.");

                Point3D c1 = wireArea.PinPoint1;
                Point3D c2 = wireArea.PinPoint2;
                double r1 = wireArea.Radius1;
                double r2 = wireArea.Radius2;

                if (r1 == 0)
                    return OperationResult<ProtectionArea>.Ok(new ProtectionArea
                    {
                        Points = new List<ProtectionPoint>
                        {
                            new ProtectionPoint(c2, radius: r2)
                        }
                    });

                if (r2 == 0)
                    return OperationResult<ProtectionArea>.Ok(new ProtectionArea
                    {
                        Points = new List<ProtectionPoint>
                        {
                            new ProtectionPoint(c1, radius: r1)
                        }
                    });

                double d = GeometryService.Get2DDistance(c1, c2);

                if (d == 0)
                    return OperationResult<ProtectionArea>.Fail("The two pinning points cannot be at the same location.");

                if (d <= Math.Abs(r1 - r2))
                    return OperationResult<ProtectionArea>.Fail("One circle is contained within the other; no external tangent exists.");

                // θ — angle of the line of centres relative to the horizontal X axis
                double theta = GeometryService.GetAngleToHorizontal(c1, c2);

                // α — tangent deviation angle: cos(α) = (R1 - R2) / d
                double alpha = GeometryService.GetTangentDeviationAngle(c1, r1, c2, r2);

                // Tangent points on circle 1 (upper: θ+α, lower: θ-α)
                double xt1_upper = c1.X + r1 * Math.Cos(theta + alpha);
                double yt1_upper = c1.Y + r1 * Math.Sin(theta + alpha);

                double xt1_lower = c1.X + r1 * Math.Cos(theta - alpha);
                double yt1_lower = c1.Y + r1 * Math.Sin(theta - alpha);

                // Tangent points on circle 2 (upper: θ+α, lower: θ-α)
                double xt2_upper = c2.X + r2 * Math.Cos(theta + alpha);
                double yt2_upper = c2.Y + r2 * Math.Sin(theta + alpha);

                double xt2_lower = c2.X + r2 * Math.Cos(theta - alpha);
                double yt2_lower = c2.Y + r2 * Math.Sin(theta - alpha);

                var points = new List<ProtectionPoint>
                {
                    // Upper tangent line: from circle 1 to circle 2
                    new ProtectionPoint(new Point3D(xt1_upper, yt1_upper, 0)),
                    new ProtectionPoint(new Point3D(xt2_upper, yt2_upper, 0), radius: r2),
                    // Lower tangent line: from circle 2 back to circle 1
                    new ProtectionPoint(new Point3D(xt2_lower, yt2_lower, 0)),
                    // Close the shape back at circle 1 lower tangent point
                    new ProtectionPoint(new Point3D(xt1_lower, yt1_lower, 0), radius: r1, isLastPoint: true)
                };

                return OperationResult<ProtectionArea>.Ok(new ProtectionArea(points));
            }
            catch (Exception ex)
            {
                return OperationResult<ProtectionArea>.Fail("An unexpected error occurred while building the protection area.", ex);
            }
        }

        private static double CalculateRadius(double h, LightningProtectionReliability reliability, double hx)
        {
            double h0 = CalculateConeHeight(h, reliability);
            
            if (hx >= h0)
                return 0;

            double r0 = CalculateConeBaseRadius(h, reliability);
            return r0 * (h0 - hx) / h0;
        }

        private static double CalculateConeHeight(double h, LightningProtectionReliability reliability)
        {
            switch (reliability)
            {
                case LightningProtectionReliability.P0_900:
                    // h0 = 0.87h for all ranges (0..150]
                    return 0.87 * h;

                case LightningProtectionReliability.P0_990:
                    // h0 = 0.8h for all ranges (0..150]
                    return 0.8 * h;

                case LightningProtectionReliability.P0_999:
                    if (h <= 30)
                        return 0.7 * h;
                    if (h <= 100)
                        return (0.75 - 4.28e-4 * (h - 30)) * h;
                    // 100 < h <= 150
                    return (0.72 - 1e-3 * (h - 100)) * h;

                default:
                    throw new ArgumentOutOfRangeException(nameof(reliability), "Unknown reliability level.");
            }
        }

        private static double CalculateConeBaseRadius(double h, LightningProtectionReliability reliability)
        {
            switch (reliability)
            {
                case LightningProtectionReliability.P0_900:
                    // r0 = 1.5h for all ranges (0..150]
                    return 1.5 * h;

                case LightningProtectionReliability.P0_990:
                    if (h <= 30)
                        return 0.95 * h;
                    if (h <= 100)
                        return (0.95 - 7.14e-3 * (h - 30)) * h;
                    // 100 < h <= 150
                    return (0.9 - 1e-3 * (h - 100)) * h;

                case LightningProtectionReliability.P0_999:
                    if (h <= 30)
                        return 0.7 * h;
                    if (h <= 100)
                        return (0.7 - 1.43e-3 * (h - 30)) * h;
                    // 100 < h <= 150
                    return (0.6 - 1e-3 * (h - 100)) * h;

                default:
                    throw new ArgumentOutOfRangeException(nameof(reliability), "Unknown reliability level.");
            }
        }
    }
}
