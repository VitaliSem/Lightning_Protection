namespace LP.Core.Interfaces
{
    /// <summary>Source of the lightning protectors shown in the UI.</summary>
    public interface ILightningProtectorRepository
    {
        string SourceName { get; }
    }
}
