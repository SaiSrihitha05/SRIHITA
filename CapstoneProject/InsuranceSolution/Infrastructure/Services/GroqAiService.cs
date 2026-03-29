using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class GroqAiService : IAiService
    {
        private readonly Kernel _kernel;
        private readonly ILogger<GroqAiService> _logger;

        public GroqAiService(Kernel kernel, ILogger<GroqAiService> logger)
        {
            _kernel = kernel;
            _logger = logger;
        }

        public async Task<string> GetAiResponseAsync(string systemPrompt, string userPrompt, List<string>? history = null)
        {
            try
            {
                var chatService = _kernel.GetRequiredService<IChatCompletionService>();
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage(systemPrompt);

                if (history != null)
                {
                    foreach (var msg in history)
                    {
                        if (msg.StartsWith("Customer:", StringComparison.OrdinalIgnoreCase))
                            chatHistory.AddUserMessage(msg.Replace("Customer:", "").Trim());
                        else if (msg.StartsWith("AI:", StringComparison.OrdinalIgnoreCase))
                            chatHistory.AddAssistantMessage(msg.Replace("AI:", "").Trim());
                    }
                }

                chatHistory.AddUserMessage(userPrompt);

                _logger.LogInformation("Sending request to Groq AI...");
                var response = await chatService.GetChatMessageContentAsync(chatHistory, kernel: _kernel);
                
                string content = response.Content ?? string.Empty;
                _logger.LogInformation("Groq AI Response received (Length: {Length})", content.Length);
                
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Groq AI Service failed to respond.");
                throw;
            }
        }
    }
}
