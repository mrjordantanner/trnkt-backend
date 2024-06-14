using System.Collections.Generic;

namespace Trnkt.Models
{
    public class NftCollection
    {
        public string Collection { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string BannerImageUrl { get; set; }
        public string Owner { get; set; }
        public string SafelistStatus { get; set; }
        public string Category { get; set; }
        public bool IsDisabled { get; set; }
        public bool IsNsfw { get; set; }
        public bool TraitOffersEnabled { get; set; }
        public bool CollectionOffersEnabled { get; set; }
        public string OpenseaUrl { get; set; }
        public string ProjectUrl { get; set; }
        public string WikiUrl { get; set; }
        public string DiscordUrl { get; set; }
        public string TelegramUrl { get; set; }
        public string TwitterUsername { get; set; }
        public string InstagramUsername { get; set; }
        public List<Contract> Contracts { get; set; }
    }
}