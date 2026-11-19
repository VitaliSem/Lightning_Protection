using System.Collections.Generic;

namespace LP.Core.Models.LightningProtectors.Block
{
    public class LightningProtectorBlockCreateDto
    {
        public IEnumerable<string> ExistingNumbers { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public double HeightM { get; set; }
        public double? BaseHeightM { get; set; }
        public string Description { get; set; }
    }
}
