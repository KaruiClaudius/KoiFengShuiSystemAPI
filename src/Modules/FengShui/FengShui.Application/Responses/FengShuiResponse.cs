namespace KoiFengShuiSystem.Modules.FengShui.Application.Responses
{
    public class PondShapeRecommendation
    {
        public string ShapeName { get; set; }
        public string Description { get; set; }
        public bool IsRecommended { get; set; }
    }

    public class DirectionRecommendation
    {
        public string DirectionName { get; set; }
        public string Description { get; set; }
        public bool IsRecommended { get; set; }
    }

    public class FengShuiResponse
    {
        public string? Element { get; set; }
        public string? Cung { get; set; }
        public List<string>? LuckyNumbers { get; set; }
        public List<string>? FishBreeds { get; set; }
        public List<string>? FishColors { get; set; }
        public List<PondShapeRecommendation>? SuggestedPonds { get; set; }
        public List<DirectionRecommendation>? SuggestedDirections { get; set; }
    }
}
