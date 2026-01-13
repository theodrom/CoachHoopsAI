
namespace CoachHoopsAI.Infrastructure.AI
{
    public class OpenAiOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string? Model { get; set; }
        public string? BaseUrl { get; set; }
    }
}
