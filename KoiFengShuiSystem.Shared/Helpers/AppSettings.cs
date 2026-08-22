namespace KoiFengShuiSystem.Shared.Helpers
{
    public class AppSettings
    {
        public string? Secret { get; set; }

        public string? Issuer { get; set; }

        public string? Audience { get; set; }

        public int? AccessTokenMinutes { get; set; }

    }
}
