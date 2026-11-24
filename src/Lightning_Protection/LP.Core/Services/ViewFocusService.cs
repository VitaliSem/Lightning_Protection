using System;
using LP.Core.Constants;
using LP.Core.Models.Base;

namespace LP.Core.Services
{
    public static class ViewFocusService
    {
        /// <summary>
        /// Calculates a view window centred on the given extents, sized so that the extents fill a fixed share
        /// of the view while keeping the view's aspect ratio.
        /// </summary>
        public static OperationResult<ViewWindow> CalculateFocusWindow(ViewFocusDto dto)
        {
            try
            {
                if (dto == null || dto.Min == null || dto.Max == null)
                    return OperationResult<ViewWindow>.Fail("Extents to focus on cannot be null.");

                if (!IsFinite(dto.AspectRatio) || dto.AspectRatio <= 0)
                    return OperationResult<ViewWindow>.Fail("View aspect ratio must be a positive number.");

                if (!IsFinite(dto.Min.X) || !IsFinite(dto.Min.Y) || !IsFinite(dto.Max.X) || !IsFinite(dto.Max.Y))
                    return OperationResult<ViewWindow>.Fail("Extents coordinates must be finite numbers.");

                if (dto.Min.X > dto.Max.X || dto.Min.Y > dto.Max.Y)
                    return OperationResult<ViewWindow>.Fail("Extents minimum point must not exceed the maximum point.");

                var extentsWidth = dto.Max.X - dto.Min.X;
                var extentsHeight = dto.Max.Y - dto.Min.Y;

                var height = Math.Max(extentsHeight, extentsWidth / dto.AspectRatio) / ViewFocusConstants.FillFraction;

                if (height <= 0)
                    height = ViewFocusConstants.MinViewHeight;

                return OperationResult<ViewWindow>.Ok(new ViewWindow
                {
                    CenterX = (dto.Min.X + dto.Max.X) / 2,
                    CenterY = (dto.Min.Y + dto.Max.Y) / 2,
                    Width = height * dto.AspectRatio,
                    Height = height,
                });
            }
            catch (Exception ex)
            {
                return OperationResult<ViewWindow>.Fail("An unexpected error occurred while calculating the view window.", ex);
            }
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
