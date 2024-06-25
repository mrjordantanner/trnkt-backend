using System.Collections.Generic;

namespace Trnkt.Models
{
    public class FavoritesList
    {
        public string UserId {get; set; }
        public string ListId { get; set; }
        public string Name { get; set; }
        public List<Nft> Nfts { get; set; }
    }
}