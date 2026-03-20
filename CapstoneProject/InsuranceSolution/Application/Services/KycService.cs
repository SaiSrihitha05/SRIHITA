using Application.DTOs;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Application.Services
{
    public class KycService : IKycService
    {
        private readonly IOcrService _ocrService;
        private readonly IUserRepository _userRepository;
        private readonly IPolicyMemberRepository _policyMemberRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<KycService> _logger;

        public KycService(
            IOcrService ocrService,
            IUserRepository userRepository,
            IPolicyMemberRepository policyMemberRepository,
            IWebHostEnvironment environment,
            ILogger<KycService> logger)
        {
            _ocrService = ocrService;
            _userRepository = userRepository;
            _policyMemberRepository = policyMemberRepository;
            _environment = environment;
            _logger = logger;
        }

        public async Task<KycResponseDto> ProcessCustomerKycAsync(ProcessKycDto dto)
        {
            var result = await PerformKycVerification(dto);

            if (dto.TargetId > 0)
            {
                var user = await _userRepository.GetByIdAsync(dto.TargetId.Value);
                if (user != null)
                {
                    user.IdProofType = dto.IdProofType;
                    user.IdProofNumber = dto.IdProofNumber;
                    user.IdProofDocumentPath = await SaveKycDocument(dto);
                    user.IsKycVerified = result.IsSuccess;
                    user.KycVerificationStatus = result.KycStatus;
                    user.ExtractedName = result.ExtractedName;
                    user.ExtractedIdNumber = result.ExtractedIdNumber;
                    user.KycVerifiedAt = result.IsSuccess ? DateTime.UtcNow : null;

                    _userRepository.Update(user);
                    await _userRepository.SaveChangesAsync();
                }
            }

            return result;
        }

        public async Task<KycResponseDto> ProcessMemberKycAsync(ProcessKycDto dto)
        {
            var result = await PerformKycVerification(dto);

            if (dto.TargetId > 0)
            {
                var member = await _policyMemberRepository.GetByIdAsync(dto.TargetId.Value);
                if (member != null)
                {
                    member.IdProofType = dto.IdProofType;
                    member.IdProofNumber = dto.IdProofNumber;
                    member.IdProofDocumentPath = await SaveKycDocument(dto);
                    member.IsKycVerified = result.IsSuccess;
                    member.KycVerificationStatus = result.KycStatus;
                    member.ExtractedName = result.ExtractedName;
                    member.ExtractedIdNumber = result.ExtractedIdNumber;
                    member.KycVerifiedAt = result.IsSuccess ? DateTime.UtcNow : (DateTime?)null;

                    _policyMemberRepository.Update(member);
                    await _policyMemberRepository.SaveChangesAsync();
                }
            }

            return result;
        }

        private async Task<KycResponseDto> PerformKycVerification(ProcessKycDto dto)
        {
            try
            {
                var tempFileName = $"TEMP_{Guid.NewGuid()}{Path.GetExtension(dto.File.FileName)}";
                var tempPath = Path.Combine(_environment.WebRootPath, "uploads", "kyc", tempFileName);
                
                if (!Directory.Exists(Path.GetDirectoryName(tempPath))) 
                    Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await dto.File.CopyToAsync(stream);
                }

                var extractedText = await _ocrService.ExtractTextAsync(tempPath);
                if (File.Exists(tempPath)) File.Delete(tempPath);

                var extractedId = ExtractIdNumber(extractedText, dto.IdProofType).Trim();
                var extractedName = ExtractName(extractedText).Trim();
                var providedName = dto.FullName.Trim();
                var sanitizedProvidedId = dto.IdProofNumber.Replace(" ", "").Replace("-", "").ToUpper().Trim();

                // 1. Strict Format Validation
                if (!ValidateId(dto.IdProofType, sanitizedProvidedId))
                {
                    return new KycResponseDto
                    {
                        IsSuccess = false,
                        Message = $"Invalid {dto.IdProofType} format. Please check the ID number.",
                        KycStatus = "Failed",
                        ExtractedName = extractedName,
                        ExtractedIdNumber = extractedId
                    };
                }

                // 2. OCR Match Validation
                bool idMatch = string.Equals(extractedId, sanitizedProvidedId, StringComparison.OrdinalIgnoreCase);
                bool nameMatch = !string.IsNullOrEmpty(extractedName) && 
                                 (extractedName.Contains(providedName, StringComparison.OrdinalIgnoreCase) || 
                                  providedName.Contains(extractedName, StringComparison.OrdinalIgnoreCase));

                if (idMatch && nameMatch)
                {
                    return new KycResponseDto
                    {
                        IsSuccess = true,
                        Message = "KYC Verified successfully.",
                        KycStatus = "Verified",
                        ExtractedName = extractedName,
                        ExtractedIdNumber = extractedId,
                        ConfidenceScore = 0.95
                    };
                }

                var mismatchDetails = !idMatch 
                    ? $"ID mismatch (Extracted: '{extractedId}' vs Provided: '{sanitizedProvidedId}')" 
                    : $"Name mismatch (Extracted: '{extractedName}' vs Provided: '{providedName}')";

                return new KycResponseDto
                {
                    IsSuccess = false,
                    Message = $"Verification failed: {mismatchDetails}",
                    KycStatus = "Failed",
                    ExtractedName = extractedName ?? "Not Found",
                    ExtractedIdNumber = extractedId ?? "Not Found"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KYC processing failed");
                return new KycResponseDto { IsSuccess = false, Message = ex.Message, KycStatus = "Failed" };
            }
        }

        public async Task<KycResponseDto> VerifyNomineeIdentityAsync(IFormFile file, string expectedName)
        {
            try
            {
                var tempFileName = $"NOMINEE_ID_{Guid.NewGuid()}{Path.GetExtension(file.Name)}";
                var tempPath = Path.Combine(_environment.WebRootPath, "uploads", "kyc", tempFileName);

                if (!Directory.Exists(Path.GetDirectoryName(tempPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var extractedText = await _ocrService.ExtractTextAsync(tempPath);
                if (File.Exists(tempPath)) File.Delete(tempPath);

                var extractedName = ExtractName(extractedText).Trim();
                var providedName = expectedName.Trim();

                bool nameMatch = !string.IsNullOrEmpty(extractedName) &&
                                 (extractedName.Contains(providedName, StringComparison.OrdinalIgnoreCase) ||
                                  providedName.Contains(extractedName, StringComparison.OrdinalIgnoreCase));

                if (nameMatch)
                {
                    return new KycResponseDto
                    {
                        IsSuccess = true,
                        Message = "Nominee identity verified successfully.",
                        KycStatus = "Verified",
                        ExtractedName = extractedName
                    };
                }

                return new KycResponseDto
                {
                    IsSuccess = false,
                    Message = $"Nominee identity mismatch. Extracted name: '{extractedName}'",
                    KycStatus = "Failed",
                    ExtractedName = extractedName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nominee verification failed");
                return new KycResponseDto { IsSuccess = false, Message = ex.Message, KycStatus = "Failed" };
            }
        }

        public async Task<KycResponseDto> VerifyDeathCertificateAsync(IFormFile file, string certificateNumber, string? dateOfDeath = null, string? deceasedName = null)
        {
            try
            {
                var tempFileName = $"DEATH_CERT_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var tempPath = Path.Combine(_environment.WebRootPath, "uploads", "claims", tempFileName);

                if (!Directory.Exists(Path.GetDirectoryName(tempPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var extractedText = await _ocrService.ExtractTextAsync(tempPath);
                if (File.Exists(tempPath)) File.Delete(tempPath);

                bool isDeathCert = extractedText.Contains("Death Certificate", StringComparison.OrdinalIgnoreCase) ||
                                  extractedText.Contains("Certificate of Death", StringComparison.OrdinalIgnoreCase) ||
                                  extractedText.Contains("Births and Deaths", StringComparison.OrdinalIgnoreCase) ||
                                  extractedText.Contains("DeathVerification", StringComparison.OrdinalIgnoreCase) ||
                                  extractedText.Contains("Death Report", StringComparison.OrdinalIgnoreCase);

                // --- 1. Validate Certificate Number ---
                var numberPattern = @"\b[A-Z0-9\-/]{6,20}\b";
                var matches = Regex.Matches(extractedText, numberPattern, RegexOptions.IgnoreCase);
                
                bool numberMatch = false;
                string foundNumber = "Not Found";
                var sanitizedProvided = certificateNumber.Replace(" ", "").Replace("-", "").ToUpper().Trim();

                foreach (Match match in matches)
                {
                    var sanitizedFound = match.Value.Replace(" ", "").Replace("-", "").ToUpper().Trim();
                    if (string.Equals(sanitizedFound, sanitizedProvided, StringComparison.OrdinalIgnoreCase))
                    {
                        numberMatch = true;
                        foundNumber = match.Value;
                        break;
                    }
                }

                // --- 2. Validate Date of Death (if provided) ---
                bool dateMatch = true; 
                string foundDate = "Not Found";
                DateTime? providedDate = null;

                if (!string.IsNullOrEmpty(dateOfDeath))
                {
                    if (DateTime.TryParse(dateOfDeath, out DateTime d)) providedDate = d;
                }
                
                if (providedDate.HasValue)
                {
                    dateMatch = false;
                    // Extended date patterns: DD-MMM-YYYY (e.g., 30-JAN-2021) or DD-MM-YYYY or YYYY-MM-DD
                    var datePattern = @"\b(0[1-9]|[12][0-9]|3[01])[-/.](0[1-9]|1[012]|JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)[-/.](19|20)\d\d\b|\b(19|20)\d\d[-/.](0[1-9]|1[012])[-/.](0[1-9]|[12][0-9]|3[01])\b";
                    var dateMatches = Regex.Matches(extractedText, datePattern, RegexOptions.IgnoreCase);
                    
                    foreach (Match m in dateMatches)
                    {
                        var dateStr = m.Value;
                        // Robustness: Replace common OCR misreads in dates (O -> 0, I -> 1)
                        dateStr = Regex.Replace(dateStr, @"(?<=\d)O|O(?=\d)", "0", RegexOptions.IgnoreCase);
                        dateStr = Regex.Replace(dateStr, @"(?<=\d)I|I(?=\d)", "1", RegexOptions.IgnoreCase);

                        if (DateTime.TryParse(dateStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime parsedDate) ||
                            DateTime.TryParse(dateStr, new System.Globalization.CultureInfo("en-GB"), System.Globalization.DateTimeStyles.None, out parsedDate) ||
                            DateTime.TryParse(dateStr, new System.Globalization.CultureInfo("en-US"), System.Globalization.DateTimeStyles.None, out parsedDate))
                        {
                            if (parsedDate.Date == providedDate.Value.Date)
                            {
                                dateMatch = true;
                                foundDate = parsedDate.ToShortDateString();
                                break;
                            }
                        }
                    }
                }

                // --- 3. Validate Deceased Name (if provided) ---
                bool nameMatch = true;
                string foundName = "Not Found";

                if (!string.IsNullOrEmpty(deceasedName))
                {
                    nameMatch = false;
                    var namePattern = @"(?:Name|Full Name|Deceased Name|Name of Deceased)\s*[:\-]?\s*([A-Z\s]{10,50})";
                    var nameMatches = Regex.Matches(extractedText, namePattern, RegexOptions.IgnoreCase);

                    var providedName = deceasedName.Trim();

                    foreach (Match m in nameMatches)
                    {
                        var extractedName = m.Groups[1].Value.Trim();
                        if (extractedName.Contains(providedName, StringComparison.OrdinalIgnoreCase) ||
                            providedName.Contains(extractedName, StringComparison.OrdinalIgnoreCase))
                        {
                            nameMatch = true;
                            foundName = extractedName;
                            break;
                        }
                    }
                }

                if (numberMatch && dateMatch && nameMatch)
                {
                    return new KycResponseDto
                    {
                        IsSuccess = true,
                        Message = "Death Certificate verified successfully.",
                        KycStatus = "Verified",
                        ExtractedIdNumber = foundNumber,
                        ExtractedName = foundName
                    };
                }

                string mismatchMsg = !numberMatch ? "Certificate number mismatch." : "";
                if (!dateMatch) mismatchMsg += (string.IsNullOrEmpty(mismatchMsg) ? "" : " ") + "Date of death mismatch.";
                if (!nameMatch) mismatchMsg += (string.IsNullOrEmpty(mismatchMsg) ? "" : " ") + "Deceased name mismatch.";

                return new KycResponseDto
                {
                    IsSuccess = false,
                    Message = isDeathCert ? mismatchMsg : "Document does not appear to be a Death Certificate.",
                    KycStatus = "Failed",
                    ExtractedIdNumber = matches.Count > 0 ? matches[0].Value : "Not Found"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Death certificate verification failed");
                return new KycResponseDto { IsSuccess = false, Message = ex.Message, KycStatus = "Failed" };
            }
        }

        public bool ValidateId(string idType, string idNumber)
        {
            idNumber = idNumber.Trim().ToUpper();
            idType = idType.ToUpper().Replace(" ", "");

            return idType switch
            {
                var t when t.Contains("AADHAAR") || t.Contains("AADHAR") => Regex.IsMatch(idNumber, @"^[2-9]\d{11}$"),
                var t when t.Contains("PAN") => Regex.IsMatch(idNumber, @"^[A-Z]{5}[0-9]{4}[A-Z]$"),
                _ => false
            };
        }

        private string ExtractIdNumber(string text, string type)
        {
            string pattern = type.ToLower() switch
            {
                var t when t.Contains("aadhaar") || t.Contains("aadhar") => @"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}\b",
                var t when t.Contains("pan") => @"\b[A-Z]{5}\d{4}[A-Z]\b",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(pattern)) return string.Empty;

            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            return match.Success ? match.Value.Replace(" ", "").Replace("-", "").ToUpper() : string.Empty;
        }

        private string ExtractName(string text)
        {
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("Name", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(':');
                    if (parts.Length > 1) return parts[1].Trim();
                }
            }

            var candidate = lines
                .Select(l => l.Trim())
                .Where(l => l.Length > 3 && l.All(c => char.IsUpper(c) || char.IsWhiteSpace(c)))
                .OrderByDescending(l => l.Length)
                .FirstOrDefault();

            return candidate ?? string.Empty;
        }

        private async Task<string> SaveKycDocument(ProcessKycDto dto)
        {
            var root = _environment.WebRootPath;
            var folder = Path.Combine(root, "uploads", "kyc");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var targetIdStr = dto.TargetId?.ToString() ?? "NEW";
            var fileName = $"KYC_{dto.IdProofType}_{targetIdStr}_{Guid.NewGuid()}{Path.GetExtension(dto.File.FileName)}";
            var filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.File.CopyToAsync(stream);
            }

            return $"uploads/kyc/{fileName}";
        }
    }
}
