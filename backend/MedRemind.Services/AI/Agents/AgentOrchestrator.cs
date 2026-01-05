using Microsoft.SemanticKernel;
using MedRemind.Core.DTOs;
using MedRemind.Services.Prescriptions;

namespace MedRemind.Services.AI.Agents;

/// <summary>
/// Enhanced orchestrator with priority-based multi-AI parser system
/// Priority order: DeepSeek (1) ? OpenAI (2) ? Merge ? Validate ? Claude (3) if needed
/// Agent 1: OCRTextSaverAgent - Saves OCR text
/// Agent 2: Multi-Parser System - DeepSeek, OpenAI, Claude (priority-based)
/// Agent 3: Result Merger - Combines results from multiple parsers
/// Agent 4: Validation - Checks completeness
/// Agent 5: Database Storage - Stores final result
/// </summary>
public class AgentOrchestrator
{
    private readonly OCRTextSaverAgent _ocrSaverAgent;
    private readonly PrescriptionDataExtractionAgent _extractionAgent;
    private readonly ValidationAgent _validationAgent;
    private readonly OpenAIPrescriptionParserAgent _openAIParser;
    private readonly DeepSeekPrescriptionParserAgent? _deepSeekParser;
    private readonly ClaudePrescriptionParserAgent? _claudeParser;
    private readonly PrescriptionDeduplicationService _deduplicationService;
    private readonly PrescriptionResultMergerService _mergerService;
    private readonly PrescriptionValidationService _validationService;
    
    // Configuration
    private readonly bool _deepSeekEnabled;
    private readonly bool _claudeEnabled;
    private readonly bool _skipClaudeIfComplete;
    private readonly int _deepSeekPriority;
    private readonly int _openAIPriority;
    private readonly int _claudePriority;
    
    // Performance configuration
    private const int PARSER_TIMEOUT_SECONDS = 20; // 20 second timeout per parser

    public AgentOrchestrator(
        OCRTextSaverAgent ocrSaverAgent,
        PrescriptionDataExtractionAgent extractionAgent,
        ValidationAgent validationAgent,
        OpenAIPrescriptionParserAgent openAIParser,
        PrescriptionDeduplicationService deduplicationService,
        PrescriptionResultMergerService mergerService,
        PrescriptionValidationService validationService,
        DeepSeekPrescriptionParserAgent? deepSeekParser = null,
        ClaudePrescriptionParserAgent? claudeParser = null,
        bool deepSeekEnabled = false,
        bool claudeEnabled = false,
        bool skipClaudeIfComplete = true,
        int deepSeekPriority = 1,
        int openAIPriority = 2,
        int claudePriority = 3)
    {
        _ocrSaverAgent = ocrSaverAgent ?? throw new ArgumentNullException(nameof(ocrSaverAgent));
        _extractionAgent = extractionAgent ?? throw new ArgumentNullException(nameof(extractionAgent));
        _validationAgent = validationAgent ?? throw new ArgumentNullException(nameof(validationAgent));
        _openAIParser = openAIParser ?? throw new ArgumentNullException(nameof(openAIParser));
        _deduplicationService = deduplicationService ?? throw new ArgumentNullException(nameof(deduplicationService));
        _mergerService = mergerService ?? throw new ArgumentNullException(nameof(mergerService));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        
        _deepSeekParser = deepSeekParser;
        _claudeParser = claudeParser;
        
        _deepSeekEnabled = deepSeekEnabled && deepSeekParser != null;
        _claudeEnabled = claudeEnabled && claudeParser != null;
        _skipClaudeIfComplete = skipClaudeIfComplete;
        
        _deepSeekPriority = deepSeekPriority;
        _openAIPriority = openAIPriority;
        _claudePriority = claudePriority;
        
        System.Diagnostics.Debug.WriteLine("? Agent Orchestrator initialized:");
        System.Diagnostics.Debug.WriteLine($"   DeepSeek: {(_deepSeekEnabled ? $"? Priority {_deepSeekPriority}" : "? Disabled")}");
        System.Diagnostics.Debug.WriteLine($"   OpenAI: ? Priority {_openAIPriority}");
        System.Diagnostics.Debug.WriteLine($"   Claude: {(_claudeEnabled ? $"? Priority {_claudePriority}" : "? Disabled")}");
        System.Diagnostics.Debug.WriteLine($"   Skip Claude if complete: {_skipClaudeIfComplete}");
    }

    /// <summary>
    /// Execute complete prescription processing workflow with priority-based multi-parser system
    /// </summary>
    public async Task<PrescriptionProcessingResult> ProcessPrescriptionAsync(
        string ocrText,
        string prescriptionFileName,
        int prescriptionId,
        CancellationToken cancellationToken = default)
    {
        var result = new PrescriptionProcessingResult
        {
            PrescriptionFileName = prescriptionFileName,
            StartTime = DateTime.UtcNow
        };

        try
        {
            System.Diagnostics.Debug.WriteLine("\n?? Agent Orchestrator: Starting PRIORITY-BASED Multi-Parser workflow");
            System.Diagnostics.Debug.WriteLine($"   Prescription: {prescriptionFileName}");
            System.Diagnostics.Debug.WriteLine($"   Prescription ID: {prescriptionId}");
            System.Diagnostics.Debug.WriteLine($"   OCR text: {ocrText?.Length ?? 0} characters");

            // Check if OCR text is valid
            if (string.IsNullOrWhiteSpace(ocrText) || ocrText.Length < 50)
            {
                System.Diagnostics.Debug.WriteLine("?? OCR text is empty or too short");
                result.Success = false;
                result.ErrorMessage = "OCR text extraction failed. Image needs to be processed with OpenAI Vision directly.";
                result.EndTime = DateTime.UtcNow;
                result.ProcessingTime = result.EndTime.Value - result.StartTime;
                return result;
            }

            // ============================================
            // STEP 1: Save OCR Text (Agent 1)
            // ============================================
            System.Diagnostics.Debug.WriteLine("\n?? STEP 1: OCR Text Saver Agent");
            
            var saveResult = await _ocrSaverAgent.SaveOCRTextAsync(ocrText, prescriptionFileName);
            result.OCRSaveResult = saveResult;

            if (!saveResult.Success)
            {
                result.Success = false;
                result.ErrorMessage = $"Failed to save OCR text: {saveResult.ErrorMessage}";
                System.Diagnostics.Debug.WriteLine($"? Step 1 Failed: {result.ErrorMessage}");
                return result;
            }

            System.Diagnostics.Debug.WriteLine($"? Step 1 Complete: Saved to {saveResult.SavedFilePath}");

            // ============================================
            // STEP 2: Priority-Based Multi-Parser System
            // ============================================
            System.Diagnostics.Debug.WriteLine("\n?? STEP 2: Priority-Based Multi-Parser System");

            // Create priority list
            var parsers = new List<(int priority, string name, Func<Task<PrescriptionParseResult>> parser)>();
            
            if (_deepSeekEnabled)
            {
                parsers.Add((_deepSeekPriority, "DeepSeek", async () => await _deepSeekParser!.ParsePrescriptionTextAsync(ocrText, cancellationToken)));
            }
            
            parsers.Add((_openAIPriority, "OpenAI", async () => await _openAIParser.ParsePrescriptionTextAsync(ocrText, cancellationToken)));
            
            if (_claudeEnabled)
            {
                parsers.Add((_claudePriority, "Claude", async () => await _claudeParser!.ParsePrescriptionTextAsync(ocrText, cancellationToken)));
            }

            // Sort by priority (1 = first)
            parsers = parsers.OrderBy(p => p.priority).ToList();

            System.Diagnostics.Debug.WriteLine($"   Parser order: {string.Join(" ? ", parsers.Select(p => $"{p.name}({p.priority})"))}");

            // Execute parsers and merge results
            PrescriptionParseResult? mergedResult = null;
            var parserResults = new Dictionary<string, PrescriptionParseResult>();
            var providersUsed = new List<string>();

            for (int i = 0; i < parsers.Count; i++)
            {
                var (priority, name, parser) = parsers[i];
                
                System.Diagnostics.Debug.WriteLine($"\n   Parser {i + 1}/{parsers.Count}: {name} (Priority {priority})");
                
                var parserStartTime = DateTime.UtcNow;
                CancellationTokenSource? parserCts = null;
                
                try
                {
                    // Add per-parser timeout to prevent hanging
                    parserCts = new CancellationTokenSource(TimeSpan.FromSeconds(PARSER_TIMEOUT_SECONDS));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, parserCts.Token);
                    
                    var parserResult = await parser();
                    
                    var parserElapsed = DateTime.UtcNow - parserStartTime;
                    System.Diagnostics.Debug.WriteLine($"   ?? {name} completed in {parserElapsed.TotalSeconds:F2}s");
                    
                    parserResults[name] = parserResult;
                    providersUsed.Add(name);

                    System.Diagnostics.Debug.WriteLine($"   ? {name}: {parserResult.Medications.Count} medications");

                    // Merge with previous results
                    if (mergedResult == null)
                    {
                        mergedResult = parserResult;
                    }
                    else
                    {
                        mergedResult = _mergerService.MergeResults(
                            mergedResult, 
                            parserResult,
                            string.Join("+", providersUsed.Take(providersUsed.Count - 1)),
                            name);
                    }

                    // Check if result is complete after first two parsers (DeepSeek + OpenAI)
                    if (i >= 1) // After at least 2 parsers
                    {
                        var validation = _validationService.ValidateCompleteness(mergedResult);
                        
                        if (validation.IsComplete && _skipClaudeIfComplete)
                        {
                            System.Diagnostics.Debug.WriteLine($"\n   ? Result is complete! Skipping remaining parsers.");
                            System.Diagnostics.Debug.WriteLine($"      Missing items: {validation.GetMissingItems()}");
                            break;
                        }
                        else if (!validation.IsComplete)
                        {
                            System.Diagnostics.Debug.WriteLine($"   ?? Result incomplete. Missing: {validation.GetMissingItems()}");
                            System.Diagnostics.Debug.WriteLine($"      Continuing to next parser...");
                        }
                    }
                }
                catch (OperationCanceledException) when (parserCts?.IsCancellationRequested == true)
                {
                    var parserElapsed = DateTime.UtcNow - parserStartTime;
                    System.Diagnostics.Debug.WriteLine($"   ?? {name} timed out after {parserElapsed.TotalSeconds:F2}s (limit: {PARSER_TIMEOUT_SECONDS}s)");
                    // Continue to next parser
                }
                catch (Exception ex)
                {
                    var parserElapsed = DateTime.UtcNow - parserStartTime;
                    System.Diagnostics.Debug.WriteLine($"   ? {name} failed after {parserElapsed.TotalSeconds:F2}s: {ex.Message}");
                    // Continue to next parser
                }
                finally
                {
                    parserCts?.Dispose();
                }
            }

            if (mergedResult == null || !mergedResult.Medications.Any())
            {
                result.Success = false;
                result.ErrorMessage = "All AI parsers failed or returned no medications";
                return result;
            }

            // ============================================
            // STEP 3: Final Validation
            // ============================================
            System.Diagnostics.Debug.WriteLine("\n?? STEP 3: Final Validation");
            
            var finalValidation = _validationService.ValidateCompleteness(mergedResult);
            System.Diagnostics.Debug.WriteLine($"   Final completeness: {(finalValidation.IsComplete ? "? COMPLETE" : "?? INCOMPLETE")}");

            // ============================================
            // STEP 4: Store in Database
            // ============================================
            System.Diagnostics.Debug.WriteLine("\n?? STEP 4: Storing result in database");

            try
            {
                var ocrResultId = await _deduplicationService.StoreOCRResultAsync(
                    prescriptionId: prescriptionId,
                    ocrText: ocrText,
                    openAIResponse: parserResults.ContainsKey("OpenAI") 
                        ? System.Text.Json.JsonSerializer.Serialize(parserResults["OpenAI"])
                        : null,
                    claudeResponse: parserResults.ContainsKey("Claude")
                        ? System.Text.Json.JsonSerializer.Serialize(parserResults["Claude"])
                        : null,
                    selectedResponse: System.Text.Json.JsonSerializer.Serialize(mergedResult),
                    selectedProvider: string.Join("+", providersUsed),
                    comparisonScore: finalValidation.AverageConfidence,
                    comparisonReason: $"Multi-parser merge. Providers: {string.Join(", ", providersUsed)}. Complete: {finalValidation.IsComplete}",
                    medicationCount: mergedResult.Medications.Count,
                    doctorName: mergedResult.Doctor?.Name,
                    patientName: mergedResult.Patient?.Name,
                    prescriptionDate: mergedResult.PrescriptionDate,
                    processingTime: DateTime.UtcNow - result.StartTime,
                    processingAttempts: providersUsed.Count
                );

                System.Diagnostics.Debug.WriteLine($"? Stored in database with ID: {ocrResultId}");
                result.DatabaseId = ocrResultId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"?? Database storage failed: {ex.Message}");
                // Continue processing even if storage fails
            }

            // ============================================
            // FINAL: Set result
            // ============================================
            result.ParseResult = mergedResult;
            result.Success = true;
            result.MatchScore = finalValidation.AverageConfidence;
            result.TotalAttempts = providersUsed.Count;
            result.SelectedProvider = string.Join(" + ", providersUsed);

            System.Diagnostics.Debug.WriteLine($"\n? ORCHESTRATOR: Workflow Complete");
            System.Diagnostics.Debug.WriteLine($"   Providers used: {result.SelectedProvider}");
            System.Diagnostics.Debug.WriteLine($"   Patient: {result.ParseResult.Patient?.Name ?? "N/A"}");
            System.Diagnostics.Debug.WriteLine($"   Doctor: {result.ParseResult.Doctor?.Name ?? "N/A"}");
            System.Diagnostics.Debug.WriteLine($"   Medications: {result.ParseResult.Medications.Count}");
            System.Diagnostics.Debug.WriteLine($"   Confidence: {result.MatchScore:P0}");

            result.EndTime = DateTime.UtcNow;
            result.ProcessingTime = result.EndTime.Value - result.StartTime;

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? ORCHESTRATOR ERROR: {ex.Message}");
            result.Success = false;
            result.ErrorMessage = $"Orchestration failed: {ex.Message}";
            result.EndTime = DateTime.UtcNow;
            result.ProcessingTime = result.EndTime.Value - result.StartTime;
            return result;
        }
    }
}

/// <summary>
/// Complete result from multi-parser prescription processing workflow
/// </summary>
public class PrescriptionProcessingResult
{
    public bool Success { get; set; }
    public string? PrescriptionFileName { get; set; }
    public OCRSaveResult? OCRSaveResult { get; set; }
    public ValidationResult? ValidationResult { get; set; }
    public PrescriptionParseResult? ParseResult { get; set; }
    public double MatchScore { get; set; }
    public int TotalAttempts { get; set; }
    public string? WarningMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    
    // Database storage
    public string? SelectedProvider { get; set; } // e.g., "DeepSeek + OpenAI" or "DeepSeek + OpenAI + Claude"
    public int? DatabaseId { get; set; } // PrescriptionOCRResult ID
}
