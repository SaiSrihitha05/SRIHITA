using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IVertexAiService
    {
        Task<VertexAiExtractionResult> ExtractDocumentDetailsAsync(string filePath);
        Task<string> ExtractTextAsync(string filePath);
    }

    public class VertexAiExtractionResult
    {
        public string? Name { get; set; }
        public string? AadharNumber { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? PanNumber { get; set; }
        public string? DateOfDeath { get; set; }
        public string? PlaceOfDeath { get; set; }
        public string? Authority { get; set; }
        public string? RawText { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
