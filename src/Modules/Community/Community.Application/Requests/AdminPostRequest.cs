using Microsoft.AspNetCore.Http;

namespace KoiFengShuiSystem.Modules.Community.Application.Requests
{
    public class AdminPostRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int AccountId { get; set; }
        public string Status { get; set; }
        public int ElementId { get; set; }
        public List<IFormFile> Images { get; set; }
    }
}
