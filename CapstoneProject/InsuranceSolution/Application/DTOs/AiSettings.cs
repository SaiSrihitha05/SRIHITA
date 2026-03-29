namespace Application.DTOs
{
    public class AiSettings
    {
        public string Model { get; set; } = "llama-3.1-8b-instant";
        public int MaxTokens { get; set; } = 2000;
        public double Temperature { get; set; } = 0.3;
        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }
}
