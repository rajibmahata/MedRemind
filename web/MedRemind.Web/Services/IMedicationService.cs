namespace MedRemind.Web.Services;

public interface IMedicationService
{
    Task<List<MedicationDto>> GetAllAsync();
    Task<MedicationDto?> GetByIdAsync(int id);
    Task<bool> UpdateAsync(int id, MedicationDto medication);
    Task<bool> DeleteAsync(int id);
}

public class MedicationDto
{
    public int Id { get; set; }
    public int PrescriptionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

