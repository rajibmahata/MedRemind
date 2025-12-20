namespace MedRemind.Services;

public interface IOcrService
{
    Task<string> ExtractTextFromImageAsync(string imagePath);
}

// Simple mock implementation - in a real app, you would integrate with Azure Computer Vision, Google Vision API, or ML Kit
public class MockOcrService : IOcrService
{
    public Task<string> ExtractTextFromImageAsync(string imagePath)
    {
        // In a production app, this would use actual OCR technology
        // For now, return mock data to demonstrate the feature
        var mockPrescription = @"Dr. John Smith, MD
123 Medical Center Drive
Phone: (555) 123-4567

Date: " + DateTime.Now.ToString("MM/dd/yyyy") + @"

Patient: [Patient Name]

Rx:
1. Amoxicillin 500mg
   Take 1 capsule by mouth three times daily
   Duration: 7 days

2. Ibuprofen 400mg
   Take 1 tablet by mouth every 6-8 hours as needed for pain
   Duration: As needed

Refills: 0
Signature: Dr. John Smith";

        return Task.FromResult(mockPrescription);
    }
}
