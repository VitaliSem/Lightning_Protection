using System.Collections.Generic;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.Block;

namespace LP.Core.Interfaces
{
    /// <summary>Source of the lightning protectors shown in the UI.</summary>
    public interface ILightningProtectorRepository
    {
        string SourceName { get; }

        /// <summary>Returns the lightning protectors of the source, ordered by number.</summary>
        OperationResult<IReadOnlyList<LightningProtectorListEntry>> GetAll();

        /// <summary>Shows the lightning protector with the given number; the message describes the outcome.</summary>
        OperationResult<string> Focus(int number);

        /// <summary>Adds a new lightning protector; returns the data of the added protector.</summary>
        OperationResult<LightningProtectorBlockData> Insert(LightningProtectorInsertDto dto);

        /// <summary>Lets the user pick a position on the source; fails when no position is picked.</summary>
        OperationResult<Point3D> PickPosition();
    }
}
