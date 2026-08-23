namespace KoiFengShuiSystem.Modules.FengShui.Application.Responses
{
    public class PartnerShopResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string LinkUrl { get; set; } = string.Empty;
        public string? Note { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
