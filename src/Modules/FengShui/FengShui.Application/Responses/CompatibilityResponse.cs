namespace KoiFengShuiSystem.Modules.FengShui.Application.Responses
{
    public class CompatibilityResponse
    {
        public double OverallCompatibilityScore { get; set; }
        public double DirectionScore { get; set; }
        public double ShapeScore { get; set; }
        public Dictionary<string, double> ColorScores { get; set; }
        public double QuantityScore { get; set; }
        public List<string> Recommendations { get; set; }
    }
}
