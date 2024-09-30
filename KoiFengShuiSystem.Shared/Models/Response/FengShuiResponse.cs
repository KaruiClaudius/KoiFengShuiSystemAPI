using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.Shared.Models.Response
{
    public class FengShuiResponse
    {
        public string? Element { get; set; }
        public List<string>? LuckyNumbers { get; set; }
        public List<string>? FishBreeds { get; set; }
        public List<string>? FishColors { get; set; }
        public List<string>? SuggestedPonds { get; set; }
        public List<string>? SuggestedDirections { get; set; }
    }
}
