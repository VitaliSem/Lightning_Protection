using System.Collections.Generic;

namespace LP.Core.Models.LightningProtectors.Block
{
    public class LightningProtectorTypeOption
    {
        public static IReadOnlyList<LightningProtectorTypeOption> All { get; } = new[]
        {
            new LightningProtectorTypeOption("Air rod",
                "A vertical spike-type element mounted directly onto a roof, facade, pipe stand, tripod, or substation gantry."),
            new LightningProtectorTypeOption("Air mast",
                "A free-standing supporting structure (tubular or lattice) installed on its own foundation or base."),
            new LightningProtectorTypeOption("Isolated terminal",
                "A rod or mast fitted with an insulating spacer to maintain the required separation distance from conductive parts of the structure."),
            new LightningProtectorTypeOption("Combined mast",
                "A dual-purpose structure (such as a floodlight mast, lighting pole, or tower) equipped with an air-termination rod at the top."),
            new LightningProtectorTypeOption("Suspension point",
                "An attachment node used to anchor a catenary air-termination wire to a mast, pole, or building structure."),
        };

        public string Name { get; }
        public string Description { get; }

        public LightningProtectorTypeOption(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public override string ToString() => Name;
    }
}
