namespace Trnkt.Models
{
    public class Rarity
    {
        public string StrategyVersion { get; set; }
        public int Rank { get; set; }
        public int Score { get; set; }
        public string CalculatedAt { get; set; }
        public int MaxRank { get; set; }
        public int TotalSupply { get; set; }
        public RankingFeatures RankingFeatures { get; set; }
    }
}