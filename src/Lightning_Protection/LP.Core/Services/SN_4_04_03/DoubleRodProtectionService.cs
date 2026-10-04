using System;
using System.Collections.Generic;
using LP.Core.Enums;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.DoubleRod;
using LP.Core.Models.LightningProtectors.SingleRod;

namespace LP.Core.Services.SN_4_04_03
{
    public static class DoubleRodProtectionService
    {
        /// <summary>
        /// Calculates the combined protection zone parameters for a double rod protector.
        /// </summary>
        /// <param name="protector">The double rod lightning protector.</param>
        /// <param name="reliability">The required lightning protection reliability level.</param>
        /// <param name="hx">The height at which the protection is calculated.</param>
        /// <returns>OperationResult containing the DoubleRodProtectionArea at height hx.</returns>
        public static OperationResult<DoubleRodProtectionArea> Calculate(
            DoubleRodLightningProtector protector,
            LightningProtectionReliability reliability,
            double hx)
        {
            try
            {
                // -- Validation --
                if (protector == null)
                    return Fail("Protector cannot be null.");

                if (protector.Rod1 == null || protector.Rod2 == null)
                    return Fail("Both rod protectors must be provided.");

                if (protector.Rod1.Position == null || protector.Rod2.Position == null)
                    return Fail("Both rod positions must be provided.");

                if (protector.Rod1.Height <= 0 || protector.Rod2.Height <= 0)
                    return Fail("Both rod heights must be greater than zero.");

                if (protector.Rod1.Height > 150 || protector.Rod2.Height > 150)
                    return Fail("Rod heights must not exceed 150 m.");

                if (hx < 0)
                    return Fail("Calculation height hx cannot be negative.");

                if (hx >= Math.Min(protector.Rod1.Height, protector.Rod2.Height))
                    return Fail("Calculation height hx must be less than both rod heights.");

                // -- Step 1: Individual rod cone parameters --
                double h1 = protector.Rod1.Height;
                double h2 = protector.Rod2.Height;

                double h01 = SingleRodProtectionService.CalculateConeHeight(h1, reliability);
                double r01 = SingleRodProtectionService.CalculateConeBaseRadius(h1, reliability);
                double h02 = SingleRodProtectionService.CalculateConeHeight(h2, reliability);
                double r02 = SingleRodProtectionService.CalculateConeBaseRadius(h2, reliability);

                double h0, r0;
                h0 = (h01 + h02) / 2.0;
                r0 = (r01 + r02) / 2.0;

                // Individual rod protection radii at hx (used for non-combined case)
                var rod1Calc = SingleRodProtectionService.Calculate(protector.Rod1, reliability, hx);
                var rod2Calc = SingleRodProtectionService.Calculate(protector.Rod2, reliability, hx);

                if (!rod1Calc.IsSuccess)
                    return Fail(rod1Calc.Message, rod1Calc.Exception);

                if (!rod2Calc.IsSuccess)
                    return Fail(rod2Calc.Message, rod2Calc.Exception);

                double rx1 = rod1Calc.Result.Radius;
                double rx2 = rod2Calc.Result.Radius;

                // -- Step 2: Geometry --
                double L = GeometryService.Get2DDistance(protector.Rod1.Position, protector.Rod2.Position);
                double hForTable = Math.Min(h1, h2);
                double Lmax = CalculateLmax(hForTable, reliability);
                double Lc = CalculateLc(hForTable, reliability);

                // -- Step 3: Combined zone determination --
                if (L > Lmax)
                {
                    // Rods too far apart — two independent protection zones
                    return MakeResult(
                        rod1Calc.Result, rod2Calc.Result,
                        h0, r0, L, Lmax, Lc,
                        hc: h0, isCombined: false,
                        rx1, rx2, lx1: 0, lx2: 0, rcx: 0);
                }

                double hc;

                if (L <= Lc)
                    hc = h0;
                else
                    hc = ((Lmax - L) / (Lmax - Lc)) * h0;

                // -- Step 4: Protection at height hx --
                double rx, lx, rcx;
                double lx1, lx2;

                if (hx >= hc)
                {
                    // hx above the cooperative height — two separate arcs with tangent lines
                    rx = Math.Min(rx1, rx2);
                    lx1 = L * (h01 - hx) / (2.0 * (h01 - hc));
                    lx2 = L * (h02 - hx) / (2.0 * (h02 - hc));
                    lx = Math.Max(lx1, lx2);
                    rcx = 0;
                }
                else
                {
                    // hx below the cooperative height — full combined zone
                    lx1 = lx2 = L / 2.0;
                    lx = L / 2.0;
                    double rc = r0 * (h0 - hc) / h0;
                    rcx = rc * (hc - hx) / hc;
                    rx = rcx;
                }

                return MakeResult(
                    rod1Calc.Result, rod2Calc.Result,
                    h0, r0, L, Lmax, Lc, hc, isCombined: true,
                    rx1, rx2, lx1, lx2, rcx, lx, rx);
            }
            catch (Exception ex)
            {
                return OperationResult<DoubleRodProtectionArea>.Fail(
                    "An unexpected error occurred during double rod calculation.", ex);
            }
        }

        /// <summary>
        /// Builds the ProtectionArea outline for a double rod protector.
        /// When combined, produces a 6-point polygon; when not combined, two separate circles.
        /// </summary>
        /// <param name="area">The DoubleRodProtectionArea from Calculate().</param>
        /// <returns>OperationResult containing the ProtectionArea.</returns>
        public static OperationResult<ProtectionArea> GetProtectionArea(DoubleRodProtectionArea area)
        {
            try
            {
                if (area == null)
                    return OperationResult<ProtectionArea>.Fail("Protection area cannot be null.");

                Point3D c1 = area.Rod1Area.Center;
                Point3D c2 = area.Rod2Area.Center;

                if (!area.IsCombined)
                {
                    // Two separate circles — return individual rod protection points
                    return OperationResult<ProtectionArea>.Ok(new ProtectionArea
                    {
                        Points = new List<ProtectionPoint>
                        {
                            new ProtectionPoint(c1, radius: area.Rod1Area.Radius),
                            new ProtectionPoint(c2, radius: area.Rod2Area.Radius, isLastPoint: true)
                        }
                    });
                }

                // -- Combined case --

                // θ — angle of the line of centres relative to the horizontal X axis
                double theta = GeometryService.GetAngleToHorizontal(c1, c2);

                // Perpendicular unit vector (rotated 90° counter-clockwise)
                double perpX = -Math.Sin(theta);
                double perpY = Math.Cos(theta);

                // Midpoint between the two rod centres
                double midX = (c1.X + c2.X) / 2.0;
                double midY = (c1.Y + c2.Y) / 2.0;

                double rx1 = area.Rod1Area.Radius;
                double rx2 = area.Rod2Area.Radius;
                double rx = area.Rx;
                double lx1 = area.Lx1;
                double lx2 = area.Lx2;
                double lx = area.Lx;
                double rcx = area.Rcx;

                if (rcx == 0)
                {
                    // Case hx >= hc: two separate semi-circles with tangent lines to the midpoint.
                    return OperationResult<ProtectionArea>.Ok(
                        BuildHxGeHcProtectionArea(c1, c2, perpX, perpY, rx1, rx2, lx1, lx2));
                }

                // Case hx < hc: full combined zone — 6 points, midpoints have radius rcx.
                return OperationResult<ProtectionArea>.Ok(
                    BuildHxLtHcProtectionArea(c1, c2, midX, midY, perpX, perpY, rx, lx, rcx));
            }
            catch (Exception ex)
            {
                return OperationResult<ProtectionArea>.Fail(
                    "An unexpected error occurred while building the protection area.", ex);
            }
        }

        /// <summary>
        /// Builds the 6-point protection area for the hx &gt;= hc case (rcx = 0):
        /// two separate semi-circles with tangent lines to the midpoint.
        /// Midpoints are tangent line endpoints (no radius).
        /// </summary>
        private static ProtectionArea BuildHxGeHcProtectionArea(
            Point3D c1, Point3D c2,
            double perpX, double perpY,
            double rx1, double rx2,
            double lx1, double lx2)
        {
            // Upper points
            double x1_upper = c1.X + perpX * rx1;
            double y1_upper = c1.Y + perpY * rx1;

            double x2_upper = c2.X + perpX * rx2;
            double y2_upper = c2.Y + perpY * rx2;

            double x_mid_upper;
            double y_mid_upper;
            if (lx1 >= lx2)
            {
                x_mid_upper = c1.X + perpX * lx1;
                y_mid_upper = c1.Y + perpY * lx1;
            }
            else
            {
                x_mid_upper = c2.X - perpX * lx2;
                y_mid_upper = c2.Y - perpY * lx2;
            }

            // Lower points
            double x1_lower = c1.X - perpX * rx1;
            double y1_lower = c1.Y - perpY * rx1;

            double x2_lower = c2.X - perpX * rx2;
            double y2_lower = c2.Y - perpY * rx2;

            double x_mid_lower;
            double y_mid_lower;
            if (lx1 >= lx2)
            {
                x_mid_lower = c1.X - perpX * lx1;
                y_mid_lower = c1.Y - perpY * lx1;
            }
            else
            {
                x_mid_lower = c2.X + perpX * lx2;
                y_mid_lower = c2.Y + perpY * lx2;
            }

            var points = new List<ProtectionPoint>
            {
                // Semi-circle for rod1: arc from rod1 upper to rod1 lower
                new ProtectionPoint(new Point3D(x1_upper, y1_upper, 0), radius: rx1),
                new ProtectionPoint(new Point3D(x1_lower, y1_lower, 0)),
                // Tangent line to rod2 lower
                new ProtectionPoint(new Point3D(x_mid_lower, y_mid_lower, 0)),
                // Semi-circle for rod2: arc from rod2 lower to rod2 upper
                new ProtectionPoint(new Point3D(x2_lower, y2_lower, 0), radius: rx2),
                new ProtectionPoint(new Point3D(x2_upper, y2_upper, 0)),
                // Tangent line back to rod1 upper (closing)
                new ProtectionPoint(new Point3D(x_mid_upper, y_mid_upper, 0), isLastPoint: true)
            };

            return new ProtectionArea(points);
        }

        /// <summary>
        /// Builds the 6-point protection area for the hx &lt; hc case (rcx &gt; 0):
        /// full combined zone where midpoints have radius rcx.
        /// </summary>
        private static ProtectionArea BuildHxLtHcProtectionArea(
            Point3D c1, Point3D c2,
            double midX, double midY,
            double perpX, double perpY,
            double rx, double lx, double rcx)
        {
            double x1_upper = c1.X + perpX * rx;
            double y1_upper = c1.Y + perpY * rx;

            double x_mid_upper = midX + perpX * lx;
            double y_mid_upper = midY + perpY * lx;

            double x2_upper = c2.X + perpX * rx;
            double y2_upper = c2.Y + perpY * rx;

            double x2_lower = c2.X - perpX * rx;
            double y2_lower = c2.Y - perpY * rx;

            double x_mid_lower = midX - perpX * lx;
            double y_mid_lower = midY - perpY * lx;

            double x1_lower = c1.X - perpX * rx;
            double y1_lower = c1.Y - perpY * rx;

            var points = new List<ProtectionPoint>
            {
                new ProtectionPoint(new Point3D(x1_upper, y1_upper, 0), radius: rx),
                new ProtectionPoint(new Point3D(x_mid_upper, y_mid_upper, 0), radius: rcx),
                new ProtectionPoint(new Point3D(x2_upper, y2_upper, 0), radius: rx),
                new ProtectionPoint(new Point3D(x2_lower, y2_lower, 0), radius: rx),
                new ProtectionPoint(new Point3D(x_mid_lower, y_mid_lower, 0), radius: rcx),
                new ProtectionPoint(new Point3D(x1_lower, y1_lower, 0), radius: rx, isLastPoint: true)
            };

            return new ProtectionArea(points);
        }

        /// <summary>Calculates Lmax — the maximum effective distance between rods.</summary>
        private static double CalculateLmax(double h, LightningProtectionReliability reliability)
        {
            switch (reliability)
            {
                case LightningProtectionReliability.P0_900:
                    if (h <= 30) return 5.75 * h;
                    if (h <= 100) return (5.75 - 3.57e-3 * (h - 30)) * h;
                    return 5.5 * h;

                case LightningProtectionReliability.P0_990:
                    if (h <= 30) return 4.75 * h;
                    if (h <= 100) return (4.75 - 3.57e-3 * (h - 30)) * h;
                    return 4.5 * h;

                case LightningProtectionReliability.P0_999:
                    if (h <= 30) return 4.25 * h;
                    if (h <= 100) return (4.25 - 3.57e-3 * (h - 30)) * h;
                    return 4.0 * h;

                default:
                    throw new ArgumentOutOfRangeException(nameof(reliability), "Unknown reliability level.");
            }
        }

        /// <summary>Calculates Lc — the critical distance below which no sag occurs.</summary>
        private static double CalculateLc(double h, LightningProtectionReliability reliability)
        {
            switch (reliability)
            {
                case LightningProtectionReliability.P0_900:
                    return 2.5 * h;

                case LightningProtectionReliability.P0_990:
                    if (h <= 30) return 2.25 * h;
                    if (h <= 100) return (2.25 - 1.007e-2 * (h - 30)) * h;
                    return 1.5 * h;

                case LightningProtectionReliability.P0_999:
                    if (h <= 30) return 2.25 * h;
                    if (h <= 100) return (2.25 - 1.007e-2 * (h - 30)) * h;
                    return 1.5 * h;

                default:
                    throw new ArgumentOutOfRangeException(nameof(reliability), "Unknown reliability level.");
            }
        }

        private static OperationResult<DoubleRodProtectionArea> MakeResult(
            SingleRodProtectionArea rod1Area,
            SingleRodProtectionArea rod2Area,
            double h0, double r0, double L, double Lmax, double Lc,
            double hc, bool isCombined,
            double rx1, double rx2, double lx1, double lx2, double rcx,
            double lx = 0, double rx = 0)
        {
            return OperationResult<DoubleRodProtectionArea>.Ok(
                new DoubleRodProtectionArea(
                    rod1Area, rod2Area,
                    h0, r0, L, Lmax, Lc, hc, isCombined,
                    rx > 0 ? rx : Math.Min(rx1, rx2),
                    lx > 0 ? lx : Math.Max(lx1, lx2),
                    rcx, lx1, lx2));
        }

        private static OperationResult<DoubleRodProtectionArea> Fail(string message, Exception exception = null)
            => OperationResult<DoubleRodProtectionArea>.Fail(message, exception);
    }
}