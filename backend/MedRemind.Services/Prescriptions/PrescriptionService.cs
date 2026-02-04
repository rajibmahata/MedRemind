using Microsoft.EntityFrameworkCore;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Models;
using MedRemind.Core.Enums;

namespace MedRemind.Services.Prescriptions;

public class PrescriptionService
{
    private readonly IUnitOfWork _unitOfWork;

    public PrescriptionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Prescription> AddPrescriptionAsync(Prescription prescription)
    {
        var prescriptionRepo = _unitOfWork.Repository<Prescription>();
        await prescriptionRepo.AddAsync(prescription);
        await _unitOfWork.SaveChangesAsync();
        return prescription;
    }

    public async Task<Prescription?> GetPrescriptionByIdAsync(int prescriptionId)
    {
        var prescriptionRepo = _unitOfWork.Repository<Prescription>();
        return await prescriptionRepo.GetByIdAsync(prescriptionId);
    }

    public async Task<IEnumerable<Prescription>> GetPrescriptionsByUserAsync(int userId)
    {
        var prescriptionRepo = _unitOfWork.Repository<Prescription>();
        return await prescriptionRepo.FindAsync(p => p.UserId == userId);
    }

    public async Task UpdatePrescriptionStatusAsync(
        int prescriptionId,
        string status,
        string? statusMessage = null)
    {
        var prescriptionRepo = _unitOfWork.Repository<Prescription>();
        var prescription = await prescriptionRepo.GetByIdAsync(prescriptionId);

        if (prescription != null)
        {
            prescription.Status = status;
            prescription.ProcessedAt = DateTime.UtcNow;

            await prescriptionRepo.UpdateAsync(prescription);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<bool> DeletePrescriptionAsync(int prescriptionId)
    {
        var prescriptionRepo = _unitOfWork.Repository<Prescription>();
        var prescription = await prescriptionRepo.GetByIdAsync(prescriptionId);

        if (prescription == null)
            return false;

        await prescriptionRepo.DeleteAsync(prescription);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Prescription>> GetPrescriptionsByStatusAsync(int userId, string status)
    {
        var prescriptionRepo = _unitOfWork.Repository<Prescription>();
        return await prescriptionRepo.FindAsync(p => p.UserId == userId && p.Status == status);
    }

    public async Task<IEnumerable<Prescription>> GetRecentPrescriptionsAsync(int userId, int count)
    {
        var prescriptionRepo = _unitOfWork.Repository<Prescription>();
        var prescriptions = await prescriptionRepo.FindAsync(p => p.UserId == userId);
        return prescriptions.OrderByDescending(p => p.CreatedAt).Take(count);
    }

    public async Task<int> CountPrescriptionsAsync(int userId)
    {
        var prescriptionRepo = _unitOfWork.Repository<Prescription>();
        return await prescriptionRepo.CountAsync(p => p.UserId == userId);
    }

    public async Task UpdatePrescriptionAsync(Prescription prescription)
    {
        var prescriptionRepo = _unitOfWork.Repository<Prescription>();
        await prescriptionRepo.UpdateAsync(prescription);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<Prescription?> GetPrescriptionWithMedicationsAsync(int prescriptionId)
    {
        var prescriptionRepo = _unitOfWork.Repository<Prescription>();
        var prescription = await prescriptionRepo.GetByIdAsync(prescriptionId);
        
        if (prescription != null)
        {
            var medicationRepo = _unitOfWork.Repository<Medication>();
            var medications = await medicationRepo.FindAsync(m => m.PrescriptionId == prescriptionId);
            prescription.Medications = medications.ToList();
        }

        return prescription;
    }
}
