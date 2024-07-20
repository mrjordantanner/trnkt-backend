using System.Collections.Generic;

namespace Trnkt.Models
{
    public class Nft
    {
        public string Identifier { get; set; }
        public string Collection { get; set; }
        public string Contract { get; set; }
        public string TokenStandard { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string MetadataUrl { get; set; }
        public string OpenseaUrl { get; set; }
        public string UpdatedAt { get; set; }
        public bool IsDisabled { get; set; }
        public bool IsNsfw { get; set; }
        public string AnimationUrl { get; set; }
        public string DisplayAnimationUrl { get; set; }
        public string DisplayImageUrl { get; set; }
        public bool IsSuspicious { get; set; }
        public string Creator { get; set; }
        public List<Trait> Traits { get; set; }
        public List<Owner> Owners { get; set; }
        public Rarity Rarity { get; set; }
    }
}