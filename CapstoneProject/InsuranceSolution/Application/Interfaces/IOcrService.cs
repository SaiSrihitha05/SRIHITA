using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IOcrService
    {
        Task<string> ExtractTextAsync(string filePath);
    }
}
