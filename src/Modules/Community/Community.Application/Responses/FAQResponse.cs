namespace KoiFengShuiSystem.Modules.Community.Application.Responses
{
    public class FAQResponse
    {
        public int FAQId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public DateTime CreateAt { get; set; }
    }
}
