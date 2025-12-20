using SQLite;
using MedRemind.Models;

namespace MedRemind.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _database;

    public async Task InitializeAsync()
    {
        if (_database != null)
            return;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "medremind.db3");
        _database = new SQLiteAsyncConnection(dbPath);

        await _database.CreateTableAsync<Medication>();
        await _database.CreateTableAsync<Prescription>();
        await _database.CreateTableAsync<Reminder>();
    }

    // Medication operations
    public async Task<List<Medication>> GetMedicationsAsync()
    {
        await InitializeAsync();
        return await _database!.Table<Medication>()
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<Medication?> GetMedicationAsync(int id)
    {
        await InitializeAsync();
        return await _database!.Table<Medication>()
            .Where(m => m.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<int> SaveMedicationAsync(Medication medication)
    {
        await InitializeAsync();
        if (medication.Id != 0)
            return await _database!.UpdateAsync(medication);
        return await _database!.InsertAsync(medication);
    }

    public async Task<int> DeleteMedicationAsync(Medication medication)
    {
        await InitializeAsync();
        medication.IsActive = false;
        return await _database!.UpdateAsync(medication);
    }

    // Prescription operations
    public async Task<List<Prescription>> GetPrescriptionsAsync()
    {
        await InitializeAsync();
        return await _database!.Table<Prescription>()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Prescription?> GetPrescriptionAsync(int id)
    {
        await InitializeAsync();
        return await _database!.Table<Prescription>()
            .Where(p => p.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<int> SavePrescriptionAsync(Prescription prescription)
    {
        await InitializeAsync();
        if (prescription.Id != 0)
            return await _database!.UpdateAsync(prescription);
        return await _database!.InsertAsync(prescription);
    }

    public async Task<int> DeletePrescriptionAsync(Prescription prescription)
    {
        await InitializeAsync();
        return await _database!.DeleteAsync(prescription);
    }

    // Reminder operations
    public async Task<List<Reminder>> GetRemindersForMedicationAsync(int medicationId)
    {
        await InitializeAsync();
        return await _database!.Table<Reminder>()
            .Where(r => r.MedicationId == medicationId)
            .OrderBy(r => r.Time)
            .ToListAsync();
    }

    public async Task<List<Reminder>> GetActiveRemindersAsync()
    {
        await InitializeAsync();
        return await _database!.Table<Reminder>()
            .Where(r => r.IsEnabled)
            .ToListAsync();
    }

    public async Task<int> SaveReminderAsync(Reminder reminder)
    {
        await InitializeAsync();
        if (reminder.Id != 0)
            return await _database!.UpdateAsync(reminder);
        return await _database!.InsertAsync(reminder);
    }

    public async Task<int> DeleteReminderAsync(Reminder reminder)
    {
        await InitializeAsync();
        return await _database!.DeleteAsync(reminder);
    }
}
