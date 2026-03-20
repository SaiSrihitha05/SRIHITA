using Application.DTOs;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class BrevoEmailService : Application.Interfaces.IEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly Application.DTOs.BrevoSettings _settings;
        private readonly ILogger<BrevoEmailService> _logger;

        public BrevoEmailService(HttpClient httpClient, IOptions<Application.DTOs.BrevoSettings> settings, ILogger<BrevoEmailService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(Application.DTOs.EmailRequest request)
        {
            try
            {
                var payload = new
                {
                    sender = new { name = _settings.SenderName, email = _settings.SenderEmail },
                    to = new[] { new { email = request.ToEmail, name = request.ToName } },
                    subject = request.Subject,
                    htmlContent = request.HtmlContent,
                    attachment = request.Attachments?.ConvertAll(a => new
                    {
                        content = Convert.ToBase64String(a.Content),
                        name = a.Name
                    })
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("api-key", _settings.ApiKey);
                _httpClient.DefaultRequestHeaders.Add("accept", "application/json");

                var response = await _httpClient.PostAsync("https://api.brevo.com/v3/smtp/email", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Email sent successfully to {ToEmail}", request.ToEmail);
                    return true;
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send email to {ToEmail}. Status: {Status}, Error: {Error}", 
                    request.ToEmail, response.StatusCode, error);
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while sending email to {ToEmail}", request.ToEmail);
                return false;
            }
        }
    }
}
