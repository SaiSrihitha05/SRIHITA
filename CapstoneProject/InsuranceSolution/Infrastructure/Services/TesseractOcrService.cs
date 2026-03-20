using Tesseract;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class TesseractOcrService : IOcrService
    {
        private readonly string _tessDataPath;
        private readonly ILogger<TesseractOcrService> _logger;

        public TesseractOcrService(IConfiguration configuration, ILogger<TesseractOcrService> logger)
        {
            // Default to 'tessdata' in the execution directory if not configured
            _tessDataPath = configuration["OcrSettings:TessDataPath"] 
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            
            _logger = logger;

            if (!Directory.Exists(_tessDataPath))
            {
                _logger.LogWarning("TessData directory not found at: {Path}. OCR will likely fail.", _tessDataPath);
            }
        }

        public async Task<string> ExtractTextAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Document file not found for OCR processing", filePath);
            }

            return await Task.Run(() =>
            {
                try
                {
                    using (var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default))
                    {
                        using (var img = Pix.LoadFromFile(filePath))
                        {
                            using (var page = engine.Process(img))
                            {
                                var text = page.GetText();
                                _logger.LogInformation("OCR extraction successful for {FilePath}. Text length: {Length}", filePath, text?.Length ?? 0);
                                return text ?? string.Empty;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OCR extraction failed for file: {FilePath}", filePath);
                    throw new Exception("Failed to extract text from the document. Please ensure the image is clear and contains readable text.", ex);
                }
            });
        }
    }
}
