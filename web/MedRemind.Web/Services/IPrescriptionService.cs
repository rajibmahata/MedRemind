namespace MedRemind.Web.Services;

public interface IPrescriptionService
{
    Task<UploadResponse> UploadPrescriptionAsync(Stream fileStream, string fileName);
    Task<List<PrescriptionDto>> GetAllAsync();
    Task<PrescriptionDto?> GetByIdAsync(int id);
}

public class UploadResponse
{
    public bool Success { get; set; }
    public int PrescriptionId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class PrescriptionDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<MedicationDto> Medications { get; set; } = new();
}

