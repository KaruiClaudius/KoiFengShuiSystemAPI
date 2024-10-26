using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.Shared.Models.Response
{
    public class PondShapeRecommendation
    {
        public string ShapeName { get; set; }
        public string Description { get; set; }
        public bool IsRecommended { get; set; }

    }

    // Update FengShuiResponse to use the new class
    public class FengShuiResponse
    {
        public string? Element { get; set; }
        public string? Cung { get; set; }
        public List<string>? LuckyNumbers { get; set; }
        public List<string>? FishBreeds { get; set; }
        public List<string>? FishColors { get; set; }
        public List<PondShapeRecommendation>? SuggestedPonds { get; set; }
        public List<string>? SuggestedDirections { get; set; }
    }
}
