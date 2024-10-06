using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.Shared.Models.Response
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
