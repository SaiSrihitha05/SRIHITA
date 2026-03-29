using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IAiService
    {
        Task<string> GetAiResponseAsync(string systemPrompt, string userPrompt, List<string>? history = null);
    }
}
