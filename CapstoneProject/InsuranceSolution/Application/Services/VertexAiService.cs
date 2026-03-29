using Application.Interfaces;
using Google.Cloud.DocumentAI.V1;
using Google.Apis.Auth.OAuth2;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class VertexAiService : IVertexAiService
    {
        private readonly string _processorName;
        private readonly IConfiguration _configuration;
        private readonly ILogger<VertexAiService> _logger;

        public VertexAiService(IConfiguration configuration, ILogger<VertexAiService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _processorName = configuration["VertexAi:ProcessorName"] 
                ?? "projects/589138064689/locations/us/processors/d653dd7ec7f45a8c/processorVersions/pretrained-foundation-model-v1.5-pro-2025-06-20";
        }

        private async Task<DocumentProcessorServiceClient> CreateClientAsync()
        {
            var credentialPath = _configuration["VertexAi:CredentialPath"];
            
            if (string.IsNullOrEmpty(credentialPath))
            {
                _logger.LogWarning("No VertexAi:CredentialPath found. Falling back to Application Default Credentials.");
                return await DocumentProcessorServiceClient.CreateAsync();
            }

            // Resolve path relative to content root if needed
            string fullPath = credentialPath;
            if (!Path.IsPathRooted(fullPath))
            {
                fullPath = Path.Combine(Directory.GetCurrentDirectory(), credentialPath);
                
                if (!File.Exists(fullPath))
                {
                    // Try assembly location as fallback
                    fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, credentialPath);
                }
            }

            if (!File.Exists(fullPath))
            {
                _logger.LogError("Credential file not found at: {Path}. Using default credentials.", fullPath);
                return await DocumentProcessorServiceClient.CreateAsync();
            }

            _logger.LogInformation("Initializing DocumentProcessorServiceClient with credentials from: {Path}", fullPath);
            
            using (var stream = File.OpenRead(fullPath))
            {
                return await new DocumentProcessorServiceClientBuilder
                {
                    GoogleCredential = GoogleCredential.FromStream(stream)
                }.BuildAsync();
            }
        }

        public async Task<VertexAiExtractionResult> ExtractDocumentDetailsAsync(string filePath)
        {
            try
            {
                var client = await CreateClientAsync();

                var rawDocument = new RawDocument
                {
                    Content = ByteString.CopyFrom(File.ReadAllBytes(filePath)),
                    MimeType = "image/png"
                };

                var request = new ProcessRequest
                {
                    Name = _processorName,
                    RawDocument = rawDocument
                };

                var response = await client.ProcessDocumentAsync(request);
                var doc = response.Document;

                var result = new VertexAiExtractionResult { IsSuccess = true, RawText = doc.Text };

                foreach (var entity in doc.Entities)
                {
                    var type = entity.Type.ToLower();
                    var text = entity.MentionText?.Trim();

                    if (type == "name") result.Name = text;
                    else if (type == "aadhar_no" || type == "id_number" || type == "death_cert_no" || type == "cert_no" || type == "certificate_no" || type == "certificate_number") result.AadharNumber = text?.Replace(" ", "");
                    else if (type == "pan_no") result.PanNumber = text?.ToUpper();
                    else if (type == "date" || type == "dob" || type == "date_of_birth") result.DateOfBirth = text;
                    else if (type == "gender" || type == "sex") result.Gender = text;
                    else if (type == "date_of_death" || type == "death_date") result.DateOfDeath = text;
                    else if (type == "place_of_death" || type == "place" || type == "death_place" || type == "place_of_occurrence") result.PlaceOfDeath = text;
                    else if (type == "authority" || type == "issuing_authority") result.Authority = text;
                }

                _logger.LogInformation("Vertex AI Extraction Success: Name={Name}, ID={ID}", result.Name, result.AadharNumber);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vertex AI Extraction Failed for file: {FilePath}", filePath);
                return new VertexAiExtractionResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Document AI processing failed." 
                };
            }
        }

        public async Task<string> ExtractTextAsync(string filePath)
        {
            try
            {
                var client = await CreateClientAsync();
                var rawDocument = new RawDocument
                {
                    Content = ByteString.CopyFrom(File.ReadAllBytes(filePath)),
                    MimeType = "image/png"
                };

                var request = new ProcessRequest { Name = _processorName, RawDocument = rawDocument };
                var response = await client.ProcessDocumentAsync(request);
                return response.Document.Text ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vertex AI Raw Text Extraction Failed");
                return string.Empty;
            }
        }
    }
}
