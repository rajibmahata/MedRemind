using Microsoft.EntityFrameworkCore;
using MedRemind.Core.Models;

namespace MedRemind.Core.Data;

public class MedRemindDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Medication> Medications { get; set; }
    public DbSet<Reminder> Reminders { get; set; }
    public DbSet<Prescription> Prescriptions { get; set; }
    public DbSet<VoiceRecording> VoiceRecordings { get; set; }
    public DbSet<DoseLog> DoseLogs { get; set; }
    public DbSet<AppSettings> AppSettings { get; set; }
    public DbSet<PrescriptionOCRResult> PrescriptionOCRResults { get; set; } // NEW

    public MedRemindDbContext(DbContextOptions<MedRemindDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PhoneNumber).IsUnique();
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(20);
        });

        // Medication configuration
        modelBuilder.Entity<Medication>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.IsActive });
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Dosage).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Unit).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Frequency).IsRequired().HasMaxLength(100);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Medications)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Prescription)
                .WithMany(p => p.Medications)
                .HasForeignKey(e => e.PrescriptionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Reminder configuration
        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.MedicationId, e.IsEnabled });

            entity.HasOne(e => e.Medication)
                .WithMany(m => m.Reminders)
                .HasForeignKey(e => e.MedicationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.VoiceRecording)
                .WithMany(v => v.Reminders)
                .HasForeignKey(e => e.VoiceRecordingId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Prescription configuration
        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Status });
            entity.Property(e => e.ImagePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.DoctorName).HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Prescriptions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PrescriptionOCRResult configuration (NEW)
        modelBuilder.Entity<PrescriptionOCRResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PrescriptionId);
            entity.HasIndex(e => e.OCRTextHash); // For duplicate detection
            entity.Property(e => e.OCRText).IsRequired();
            entity.Property(e => e.OCRTextHash).IsRequired().HasMaxLength(64); // SHA256 hash
            entity.Property(e => e.SelectedProvider).HasMaxLength(50);
            entity.Property(e => e.DoctorName).HasMaxLength(100);
            entity.Property(e => e.PatientName).HasMaxLength(100);

            entity.HasOne(e => e.Prescription)
                .WithMany()
                .HasForeignKey(e => e.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // VoiceRecording configuration
        modelBuilder.Entity<VoiceRecording>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);

            entity.HasOne(e => e.User)
                .WithMany(u => u.VoiceRecordings)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DoseLog configuration
        modelBuilder.Entity<DoseLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.MedicationId, e.ScheduledTime });
            entity.HasIndex(e => new { e.MedicationId, e.Status });
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);

            entity.HasOne(e => e.Medication)
                .WithMany(m => m.DoseLogs)
                .HasForeignKey(e => e.MedicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AppSettings configuration
        modelBuilder.Entity<AppSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.Theme).HasMaxLength(20);
            entity.Property(e => e.Language).HasMaxLength(50);

            entity.HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<AppSettings>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        await Database.EnsureCreatedAsync();
    }
}
