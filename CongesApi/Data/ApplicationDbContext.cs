using Microsoft.EntityFrameworkCore;
using CongesApi.Model;

namespace CongesApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ───────────────────────────────────
        // Tables principales
        // ───────────────────────────────────
        public DbSet<User> Users { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<LeavePolicy> LeavePolicies { get; set; }
        public DbSet<Approval> Approvals { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<LeaveBalance> LeaveBalances { get; set; }
        public DbSet<LeaveBalanceAdjustment> LeaveBalanceAdjustments { get; set; }
        public DbSet<LeaveBalanceMovement> LeaveBalanceMovements { get; set; }
        public DbSet<Holiday> Holidays { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<BlackoutPeriod> BlackoutPeriods { get; set; }
        // Lookup / enums simulés
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<ApprovalLevel> ApprovalLevels { get; set; }
        public DbSet<LeaveStatus> LeaveStatuses { get; set; }
        public DbSet<ApprovalFlowType> ApprovalFlowTypes { get; set; }
        public DbSet<NotificationType> NotificationTypes { get; set; }
        public DbSet<HalfDayPeriodType> HalfDayPeriodTypes { get; set; }
        public DbSet<DocumentCategory> DocumentCategories { get; set; }
        public DbSet<PdfTemplate> PdfTemplates { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ───────────────────────────────────
            // Lookup tables – clés primaires explicites
            // ───────────────────────────────────
            modelBuilder.Entity<UserRole>().HasKey(r => r.Role);
            modelBuilder.Entity<ApprovalLevel>().HasKey(l => l.Level);
            modelBuilder.Entity<LeaveStatus>().HasKey(s => s.Status);
            modelBuilder.Entity<ApprovalFlowType>().HasKey(a => a.FlowType);
            modelBuilder.Entity<NotificationType>().HasKey(n => n.Type);
            modelBuilder.Entity<HalfDayPeriodType>().HasKey(p => p.PeriodType);
            modelBuilder.Entity<DocumentCategory>().HasKey(d => d.Category);

            // ───────────────────────────────────
            // Entités avec clés explicites
            // ───────────────────────────────────
            modelBuilder.Entity<AuditLog>().HasKey(a => a.LogId);
            modelBuilder.Entity<LeaveBalanceAdjustment>().HasKey(a => a.AdjustmentId);

            // ───────────────────────────────────
            // User → UserRole
            // ───────────────────────────────────
            modelBuilder.Entity<User>()
                .HasOne(u => u.UserRole)
                .WithMany()
                .HasForeignKey(u => u.Role)
                .HasPrincipalKey(r => r.Role);
         
            // ───────────────────────────────────
            // LeaveType (nom de table réel + relations)
            // ───────────────────────────────────
            modelBuilder.Entity<LeaveType>()
                .ToTable("LeaveType")
                .HasKey(lt => lt.LeaveTypeId);

            modelBuilder.Entity<LeaveType>()
                .HasOne(t => t.ApprovalFlowType)
                .WithMany()
                .HasForeignKey(t => t.ApprovalFlow)
                .HasPrincipalKey(a => a.FlowType);

            modelBuilder.Entity<LeaveType>()
                .HasOne(t => t.Policy)
                .WithMany()
                .HasForeignKey(t => t.PolicyId);

            // ───────────────────────────────────
            // LeaveRequest → User & LeaveType
            // ───────────────────────────────────
            modelBuilder.Entity<LeaveRequest>()
                .HasOne(r => r.User)
                .WithMany(u => u.LeaveRequests)
                .HasForeignKey(r => r.UserId);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(r => r.LeaveType)
                .WithMany()
                .HasForeignKey(r => r.LeaveTypeId);

            // ───────────────────────────────────
            // Approval → LeaveRequest & (ApprovedBy) User
            // ───────────────────────────────────
            modelBuilder.Entity<Approval>()
                .HasOne(a => a.LeaveRequest)
                .WithMany(lr => lr.Approvals)
                .HasForeignKey(a => a.LeaveRequestId);

            modelBuilder.Entity<Approval>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.ApprovedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // ───────────────────────────────────
            // Document → User (uploader) & LeaveRequest
            // ───────────────────────────────────
            modelBuilder.Entity<Document>()
                .HasOne(d => d.UploadedBy)
                .WithMany()
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Document>()
                .HasOne(d => d.LeaveRequest)
                .WithMany()
                .HasForeignKey(d => d.LeaveRequestId);

            // ───────────────────────────────────
            // Notification → User
            // ───────────────────────────────────
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId);

            // ───────────────────────────────────
            // LeaveBalanceAdjustment → User & LeaveRequest
            // ───────────────────────────────────
            modelBuilder.Entity<LeaveBalanceAdjustment>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveBalanceAdjustment>()
                .HasOne(l => l.LeaveRequest)
                .WithMany()
                .HasForeignKey(l => l.LeaveRequestId);

            // ───────────────────────────────────
            // LeaveBalance (nom & précision décimale)
            // ───────────────────────────────────
            modelBuilder.Entity<LeaveBalance>()
                .ToTable("LeaveBalance")
                .Property(p => p.CurrentBalance)
                .HasPrecision(18, 2);

            // Ajustements – précision
            modelBuilder.Entity<LeaveBalanceAdjustment>()
                .Property(p => p.NewBalance)
                .HasPrecision(18, 2);

            modelBuilder.Entity<LeaveBalanceAdjustment>()
                .Property(p => p.OldBalance)
                .HasPrecision(18, 2);

            modelBuilder.Entity<BlackoutPeriod>()
           .HasKey(b => b.BlackoutPeriodId);

            modelBuilder.Entity<BlackoutPeriod>()
                .Property(b => b.ScopeType).HasMaxLength(20);

            modelBuilder.Entity<BlackoutPeriod>()
                .Property(b => b.EnforceMode).HasMaxLength(20);

            modelBuilder.Entity<BlackoutPeriod>()
                .HasOne(b => b.LeaveType)
                .WithMany()
                .HasForeignKey(b => b.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BlackoutPeriod>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            // ───────────────────────────────────
            // LeaveBalanceMovement (journal des débits/crédits)
            // ───────────────────────────────────
            modelBuilder.Entity<LeaveBalanceMovement>(e =>
            {
                e.ToTable("LeaveBalanceMovements");
                e.HasKey(m => m.MovementId);

                e.Property(m => m.Quantity).HasPrecision(18, 2);
                e.Property(m => m.Reason).HasMaxLength(100);

                // FK – on garde les mouvements même si l'utilisateur ou le type est supprimé
                e.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne<LeaveType>()
                    .WithMany()
                    .HasForeignKey(m => m.LeaveTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Pour la traçabilité on préfère Restrict (évite de perdre l'historique)
                e.HasOne<LeaveRequest>()
                    .WithMany()
                    .HasForeignKey(m => m.LeaveRequestId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Index utiles
                e.HasIndex(m => new { m.UserId, m.LeaveTypeId });
                e.HasIndex(m => m.LeaveRequestId);
            });
        }
    }
}
