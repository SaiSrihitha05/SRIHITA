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
        private readonly IVertexAiService _vertexAiService;
        private readonly IUserRepository _userRepository;
        private readonly IPolicyMemberRepository _policyMemberRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<KycService> _logger;

        public KycService(
            IVertexAiService vertexAiService,
            IUserRepository userRepository,
            IPolicyMemberRepository policyMemberRepository,
            IWebHostEnvironment environment,
            ILogger<KycService> logger)
        {
            _vertexAiService = vertexAiService;
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

                // Call Vertex AI / Document AI
                var aiResult = await _vertexAiService.ExtractDocumentDetailsAsync(tempPath);
                
                if (File.Exists(tempPath)) File.Delete(tempPath);

                if (!aiResult.IsSuccess)
                {
                    return new KycResponseDto { IsSuccess = false, Message = aiResult.ErrorMessage, KycStatus = "Failed" };
                }

                var docType = dto.IdProofType.ToUpper().Replace(" ", "");
                
                var extractedId = (docType == "PAN" || docType == "PANCARD" ? aiResult.PanNumber : aiResult.AadharNumber)?.Replace(" ", "") ?? "";
                var extractedName = aiResult.Name?.Trim() ?? "";
                var extractedDob = aiResult.DateOfBirth?.Trim() ?? "";
                var extractedGender = aiResult.Gender?.Trim() ?? "";
                var extractedDateOfDeath = aiResult.DateOfDeath?.Trim() ?? "";
                
                var providedName = dto.FullName.Trim();
                var sanitizedProvidedId = dto.IdProofNumber.Replace(" ", "").Replace("-", "").ToUpper().Trim();

                bool idMatch = false;
                bool nameMatch = false;
                bool dateMatch = true;
                bool genderMatch = true;

                // 1. Name Matching (resilient to partials: "Srihitha" vs "Sai Srihitha")
                nameMatch = !string.IsNullOrEmpty(extractedName) && 
                            (extractedName.Equals(providedName, StringComparison.OrdinalIgnoreCase) ||
                             extractedName.Replace(" ", "").Contains(providedName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase) ||
                             providedName.Replace(" ", "").Contains(extractedName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));

                // 2. Document Specific Logic
                switch (docType)
                {
                    case "AADHAAR":
                    case "AADHAARCARD":
                    case "AADHAR":
                    case "AADHARCARD":
                        idMatch = string.Equals(extractedId, sanitizedProvidedId, StringComparison.OrdinalIgnoreCase);
                        // DOB Check
                        if (dto.DateOfBirth.HasValue && !string.IsNullOrEmpty(extractedDob))
                        {
                            dateMatch = NormalizeAndCompareDates(extractedDob, dto.DateOfBirth);
                        }
                        // Gender Check
                        if (!string.IsNullOrEmpty(dto.Gender) && !string.IsNullOrEmpty(extractedGender))
                        {
                            genderMatch = extractedGender.ToLower().StartsWith(dto.Gender.ToLower()[0].ToString());
                        }
                        break;

                    case "PAN":
                    case "PANCARD":
                        idMatch = string.Equals(extractedId, sanitizedProvidedId, StringComparison.OrdinalIgnoreCase);
                        break;

                    case "DEATH":
                    case "DEATHCERTIFICATE":
                        // Match Date of Death
                        if (dto.ExpectedDateOfDeath.HasValue && !string.IsNullOrEmpty(extractedDateOfDeath))
                        {
                            dateMatch = NormalizeAndCompareDates(extractedDateOfDeath, dto.ExpectedDateOfDeath);
                        }
                        idMatch = true; // No ID number usually for Death Cert in this flow
                        break;

                    case "INCOME":
                    case "INCOMECERTIFICATE":
                        idMatch = true; // Authority match or just Name
                        break;
                }

                if (idMatch && nameMatch && dateMatch && genderMatch)
                {
                    return new KycResponseDto
                    {
                        IsSuccess = true,
                        Message = $"KYC Verified successfully for {docType}.",
                        KycStatus = "Verified",
                        ExtractedName = extractedName,
                        ExtractedIdNumber = extractedId,
                        ExtractedDate = docType.Contains("DEATH") ? extractedDateOfDeath : extractedDob,
                        ExtractedGender = extractedGender,
                        ExtractedAuthority = aiResult.Authority ?? "",
                        ExtractedPlace = aiResult.PlaceOfDeath ?? "",
                        ConfidenceScore = 0.98
                    };
                }

                var reasons = new List<string>();
                if (!idMatch) reasons.Add("ID/Number mismatch");
                if (!nameMatch) reasons.Add("Name mismatch");
                if (!dateMatch) reasons.Add("Date (DOB/DOD) mismatch");
                if (!genderMatch) reasons.Add("Gender mismatch");

                return new KycResponseDto
                {
                    IsSuccess = false,
                    Message = $"Verification failed: {string.Join(", ", reasons)}",
                    KycStatus = "Failed",
                    ExtractedName = extractedName,
                    ExtractedIdNumber = extractedId,
                    ExtractedDate = docType.Contains("DEATH") ? extractedDateOfDeath : extractedDob,
                    ExtractedGender = extractedGender,
                    ExtractedAuthority = aiResult.Authority ?? "",
                    ExtractedPlace = aiResult.PlaceOfDeath ?? ""
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KYC processing failed");
                return new KycResponseDto { IsSuccess = false, Message = ex.Message, KycStatus = "Failed" };
            }
        }


        public async Task<KycResponseDto> VerifyDeathCertificateAsync(IFormFile file, string certificateNumber, string? dateOfDeath = null, string? deceasedName = null, string? placeOfDeath = null)
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

                var aiResult = await _vertexAiService.ExtractDocumentDetailsAsync(tempPath);
                if (File.Exists(tempPath)) File.Delete(tempPath);

                if (!aiResult.IsSuccess)
                {
                    return new KycResponseDto { IsSuccess = false, Message = aiResult.ErrorMessage, KycStatus = "Failed" };
                }

                // --- 1. Validate Certificate Number ---
                var extractedId = aiResult.AadharNumber?.Replace(" ", "").Replace("-", "").ToUpper() ?? "";
                var sanitizedProvidedId = certificateNumber.Replace(" ", "").Replace("-", "").ToUpper().Trim();
                bool numberMatch = string.Equals(extractedId, sanitizedProvidedId, StringComparison.OrdinalIgnoreCase) ||
                                  (aiResult.RawText?.Contains(sanitizedProvidedId, StringComparison.OrdinalIgnoreCase) ?? false);

                // --- 2. Validate Date of Death ---
                bool dateMatch = true;
                string foundDate = aiResult.DateOfDeath ?? "Not Found";
                if (!string.IsNullOrEmpty(dateOfDeath) && !string.IsNullOrEmpty(aiResult.DateOfDeath))
                {
                    if (DateTime.TryParse(dateOfDeath, out DateTime inputDate))
                    {
                        dateMatch = NormalizeAndCompareDates(aiResult.DateOfDeath, inputDate);
                    }
                    else
                    {
                        // Fallback to loose contains if input string isn't standard date
                        dateMatch = aiResult.DateOfDeath.Contains(dateOfDeath);
                    }
                }
                else if (!string.IsNullOrEmpty(dateOfDeath))
                {
                    dateMatch = false; // Input provided but not found on doc
                }

                // --- 3. Validate Deceased Name ---
                var extractedName = aiResult.Name?.Trim() ?? "";
                bool nameMatch = !string.IsNullOrEmpty(extractedName) && 
                                (!string.IsNullOrEmpty(deceasedName) && 
                                 (extractedName.Contains(deceasedName, StringComparison.OrdinalIgnoreCase) ||
                                  deceasedName.Contains(extractedName, StringComparison.OrdinalIgnoreCase)));

                // --- 4. Validate Place of Death ---
                bool placeMatch = true;
                if (!string.IsNullOrEmpty(placeOfDeath) && !string.IsNullOrEmpty(aiResult.PlaceOfDeath))
                {
                    placeMatch = aiResult.PlaceOfDeath.Contains(placeOfDeath, StringComparison.OrdinalIgnoreCase) ||
                                 placeOfDeath.Contains(aiResult.PlaceOfDeath, StringComparison.OrdinalIgnoreCase);
                }

                if (numberMatch && dateMatch && nameMatch && placeMatch)
                {
                    _logger.LogInformation("Death Certificate Verification SUCCESS for Name: {Name}", extractedName);
                    return new KycResponseDto
                    {
                        IsSuccess = true,
                        Message = "Death Certificate verified successfully.",
                        KycStatus = "Verified",
                        ExtractedIdNumber = extractedId,
                        ExtractedName = extractedName,
                        ExtractedDate = foundDate,
                        ExtractedPlace = aiResult.PlaceOfDeath
                    };
                }

                string mismatchMsg = !numberMatch ? "Certificate number mismatch." : "";
                if (!dateMatch) mismatchMsg += (string.IsNullOrEmpty(mismatchMsg) ? "" : " ") + "Date of death mismatch.";
                if (!nameMatch) mismatchMsg += (string.IsNullOrEmpty(mismatchMsg) ? "" : " ") + "Deceased name mismatch.";
                if (!placeMatch) mismatchMsg += (string.IsNullOrEmpty(mismatchMsg) ? "" : " ") + "Place of death mismatch.";

                return new KycResponseDto
                {
                    IsSuccess = false,
                    Message = mismatchMsg,
                    KycStatus = "Failed",
                    ExtractedIdNumber = extractedId,
                    ExtractedName = extractedName,
                    ExtractedDate = foundDate,
                    ExtractedPlace = aiResult.PlaceOfDeath ?? ""
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Death certificate verification failed");
                return new KycResponseDto { IsSuccess = false, Message = ex.Message, KycStatus = "Failed" };
            }
        }

        public async Task<KycResponseDto> VerifyNomineeIdentityAsync(IFormFile file, string expectedName)
        {
            try
            {
                var tempFileName = $"NOMINEE_ID_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var tempPath = Path.Combine(_environment.WebRootPath, "uploads", "claims", tempFileName);

                if (!Directory.Exists(Path.GetDirectoryName(tempPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var aiResult = await _vertexAiService.ExtractDocumentDetailsAsync(tempPath);
                if (File.Exists(tempPath)) File.Delete(tempPath);

                if (!aiResult.IsSuccess)
                    return new KycResponseDto { IsSuccess = false, Message = aiResult.ErrorMessage ?? "AI Extraction failed", KycStatus = "Failed" };

                var extractedName = aiResult.Name?.Trim() ?? "";
                var providedName = expectedName.Trim();

                bool nameMatch = !string.IsNullOrEmpty(extractedName) && 
                                (extractedName.Contains(providedName, StringComparison.OrdinalIgnoreCase) ||
                                 providedName.Contains(extractedName, StringComparison.OrdinalIgnoreCase));

                if (nameMatch)
                {
                    return new KycResponseDto 
                    { 
                        IsSuccess = true, 
                        Message = "Nominee identity verified.", 
                        KycStatus = "Verified",
                        ExtractedName = extractedName
                    };
                }

                return new KycResponseDto
                {
                    IsSuccess = false,
                    Message = $"Name mismatch (Extracted: '{extractedName}' vs Nominee: '{providedName}')",
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
        private bool NormalizeAndCompareDates(string docDate, DateTime? inputDate)
        {
            if (!inputDate.HasValue || string.IsNullOrEmpty(docDate)) return false;

            // 1. Try Direct Parsing
            if (DateTime.TryParse(docDate, out DateTime parsedDocDate))
            {
                return parsedDocDate.Year == inputDate.Value.Year &&
                       parsedDocDate.Month == inputDate.Value.Month &&
                       parsedDocDate.Day == inputDate.Value.Day;
            }

            // 2. Fallback: Extract numbers using Regex (handles noise like "15-01-2026" or "15/01/2026")
            // This is a common pattern for Foundation Model OCR output that might include raw text
            var matches = System.Text.RegularExpressions.Regex.Matches(docDate, @"\d+");
            if (matches.Count >= 3)
            {
                var numbers = matches.Select(m => int.Parse(m.Value)).ToList();
                
                // Try DD MM YYYY or YYYY MM DD
                bool match1 = numbers.Contains(inputDate.Value.Day) && 
                              numbers.Contains(inputDate.Value.Month) && 
                              numbers.Contains(inputDate.Value.Year);

                return match1;
            }

            // 3. String-based fallback (last resort)
            var yearStr = inputDate.Value.Year.ToString();
            var monthStr = inputDate.Value.Month.ToString("D2");
            var dayStr = inputDate.Value.Day.ToString("D2");

            return docDate.Contains(yearStr) && 
                   (docDate.Contains(monthStr) || docDate.Contains(inputDate.Value.ToString("MMM", System.Globalization.CultureInfo.InvariantCulture))) && 
                   docDate.Contains(dayStr);
        }
    }
}
