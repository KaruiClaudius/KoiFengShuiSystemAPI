namespace KoiFengShuiSystem.DataAccess.Models;

public class Recommendation
{
    public int RecommendationId { get; set; }

    public int AccountId { get; set; }

    public int BreedId { get; set; }

    public int PondId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual KoiBreed Breed { get; set; } = null!;

    public virtual FishPond Pond { get; set; } = null!;
}
