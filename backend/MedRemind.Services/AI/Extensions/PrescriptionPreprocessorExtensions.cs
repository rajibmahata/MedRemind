using System;
using System.Collections.Generic;
using System.Text;

namespace MedRemind.Services.AI.Extensions
{
    public static class PrescriptionPreprocessorExtensions
    {
        public static string ExtractPrescriptionEssentials(this string ocrText)
        {
            var preprocessor = new PrescriptionOcrTextPreprocessor();
            return preprocessor.Preprocess(ocrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Minimal);
        }

        public static string CleanPrescriptionText(this string ocrText)
        {
            var preprocessor = new PrescriptionOcrTextPreprocessor();
            return preprocessor.Preprocess(ocrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Balanced);
        }

        public static (string cleanText, int reductionPercent) GetOptimizedPrescriptionText(this string ocrText)
        {
            var preprocessor = new PrescriptionOcrTextPreprocessor();
            var cleanText = preprocessor.Preprocess(ocrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Aggressive);

            var originalLength = ocrText.Length;
            var cleanLength = cleanText.Length;
            var reductionPercent = originalLength > 0 ?
                (int)((originalLength - cleanLength) / (double)originalLength * 100) : 0;

            return (cleanText, reductionPercent);
        }

        public static bool IsPrescriptionTooLong(this string text, int threshold = 1000)
        {
            return text.Length > threshold;
        }

        public static int EstimateTokenCount(this string text)
        {
            // Rough estimate: ~4 chars per token for English
            return (int)Math.Ceiling(text.Length / 4.0);
        }
    }
}
