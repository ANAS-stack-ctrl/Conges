using Microsoft.EntityFrameworkCore;
using CongesApi.Model;

namespace CongesApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ───────── Tables principales
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

        // ───────── Hiérarchies
        public DbSet<Hierarchy> Hierarchies => Set<Hierarchy>();
        public DbSet<HierarchyMember> HierarchyMembers => Set<HierarchyMember>();
        public DbSet<HierarchyApprovalPolicy> HierarchyApprovalPolicies => Set<HierarchyApprovalPolicy>();
        public DbSet<ApprovalDelegation> ApprovalDelegations => Set<ApprovalDelegation>();

        // ───────── Lookups
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<ApprovalLevel> ApprovalLevels { get; set; }
        public DbSet<LeaveStatus> LeaveStatuses { get; set; }
        public DbSet<ApprovalFlowType> ApprovalFlowTypes { get; set; }
        public DbSet<NotificationType> NotificationTypes { get; set; }
        public DbSet<HalfDayPeriodType> HalfDayPeriodTypes { get; set; }
        public DbSet<DocumentCategory> DocumentCategories { get; set; }
        public DbSet<PdfTemplate> PdfTemplates { get; set; } = default!;
        public DbSet<ManagerAssignment> ManagerAssignments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ───────── Lookups (PK explicites)
            modelBuilder.Entity<UserRole>().HasKey(r => r.Role);
            modelBuilder.Entity<ApprovalLevel>().HasKey(l => l.Level);
            modelBuilder.Entity<LeaveStatus>().HasKey(s => s.Status);
            modelBuilder.Entity<ApprovalFlowType>().HasKey(a => a.FlowType);
            modelBuilder.Entity<NotificationType>().HasKey(n => n.Type);
            modelBuilder.Entity<HalfDayPeriodType>().HasKey(p => p.PeriodType);
            modelBuilder.Entity<DocumentCategory>().HasKey(d => d.Category);

            // ───────── Entités avec PK explicites
            modelBuilder.Entity<AuditLog>().HasKey(a => a.LogId);
            modelBuilder.Entity<LeaveBalanceAdjustment>().HasKey(a => a.AdjustmentId);

            // ───────── User → UserRole
            modelBuilder.Entity<User>()
                .HasOne(u => u.UserRole)
                .WithMany()
                .HasForeignKey(u => u.Role)
                .HasPrincipalKey(r => r.Role);

            // ───────── LeaveType
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

            // ───────── LeaveRequest relations
            modelBuilder.Entity<LeaveRequest>()
                .HasOne(r => r.User)
                .WithMany(u => u.LeaveRequests)
                .HasForeignKey(r => r.UserId);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(r => r.LeaveType)
                .WithMany()
                .HasForeignKey(r => r.LeaveTypeId);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(r => r.Hierarchy)
                .WithMany()
                .HasForeignKey(r => r.HierarchyId)
                .OnDelete(DeleteBehavior.SetNull);

            // ───────── Approval
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

            // ───────── Document
            modelBuilder.Entity<Document>()
                .HasOne(d => d.UploadedBy)
                .WithMany()
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Document>()
                .HasOne(d => d.LeaveRequest)
                .WithMany()
                .HasForeignKey(d => d.LeaveRequestId);

            // ───────── Notification
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId);

            // ───────── LeaveBalance / Adjustments
            modelBuilder.Entity<LeaveBalance>()
                .ToTable("LeaveBalance")
                .Property(p => p.CurrentBalance)
                .HasPrecision(18, 2);

            modelBuilder.Entity<LeaveBalanceAdjustment>()
                .Property(p => p.NewBalance).HasPrecision(18, 2);
            modelBuilder.Entity<LeaveBalanceAdjustment>()
                .Property(p => p.OldBalance).HasPrecision(18, 2);

            modelBuilder.Entity<LeaveBalanceAdjustment>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveBalanceAdjustment>()
                .HasOne(l => l.LeaveRequest)
                .WithMany()
                .HasForeignKey(l => l.LeaveRequestId);

            // ───────── BlackoutPeriod
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

            // ───────── LeaveBalanceMovement (journal)
            modelBuilder.Entity<LeaveBalanceMovement>(e =>
            {
                e.ToTable("LeaveBalanceMovements");
                e.HasKey(m => m.MovementId);

                e.Property(m => m.Quantity).HasPrecision(18, 2);
                e.Property(m => m.Reason).HasMaxLength(100);

                e.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne<LeaveType>()
                    .WithMany()
                    .HasForeignKey(m => m.LeaveTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne<LeaveRequest>()
                    .WithMany()
                    .HasForeignKey(m => m.LeaveRequestId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(m => new { m.UserId, m.LeaveTypeId });
                e.HasIndex(m => m.LeaveRequestId);
            });

            // ───────── Hiérarchies (entête)
            modelBuilder.Entity<Hierarchy>(e =>
            {
                e.HasKey(h => h.HierarchyId);
                e.Property(h => h.Name).HasMaxLength(120).IsRequired();
                e.Property(h => h.Code).HasMaxLength(40);
                e.Property(h => h.Description).HasMaxLength(500);
                e.HasIndex(h => h.Name);
            });

            // ───────── Hiérarchies (membres)
            modelBuilder.Entity<HierarchyMember>(e =>
            {
                e.HasKey(m => m.HierarchyMemberId);
                e.Property(m => m.Role).HasMaxLength(30);
                e.HasIndex(m => m.UserId).IsUnique();

                e.HasOne(m => m.Hierarchy)
                    .WithMany(h => h.Members)
                    .HasForeignKey(m => m.HierarchyId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(m => m.User)
                    .WithMany()
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ───────── Politique d’approbation par hiérarchie
            modelBuilder.Entity<HierarchyApprovalPolicy>(e =>
            {
                e.HasKey(p => p.PolicyId);
                e.Property(p => p.PeerSelectionMode).HasMaxLength(20);
                e.HasOne(p => p.Hierarchy)
                 .WithOne(h => h.ApprovalPolicy)
                 .HasForeignKey<HierarchyApprovalPolicy>(p => p.HierarchyId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ───────── Délégations d’approbation
            modelBuilder.Entity<ApprovalDelegation>(e =>
            {
                e.HasKey(d => d.DelegationId);

                e.HasOne(d => d.FromUser)
                 .WithMany()
                 .HasForeignKey(d => d.FromUserId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(d => d.ToUser)
                 .WithMany()
                 .HasForeignKey(d => d.ToUserId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(d => d.Hierarchy)
                 .WithMany()
                 .HasForeignKey(d => d.HierarchyId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // ───────── ManagerAssignment (complet)
            modelBuilder.Entity<ManagerAssignment>(entity =>
            {
                entity.HasKey(x => x.ManagerAssignmentId);

                entity.HasOne(x => x.Hierarchy)
                      .WithMany() // ou .WithMany(h => h.ManagerAssignments) si tu ajoutes la collection sur Hierarchy
                      .HasForeignKey(x => x.HierarchyId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Employee)
                      .WithMany()
                      .HasForeignKey(x => x.EmployeeUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Manager)
                      .WithMany()
                      .HasForeignKey(x => x.ManagerUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Un employé ne peut avoir qu’une affectation Active par hiérarchie
                entity.HasIndex(x => new { x.HierarchyId, x.EmployeeUserId, x.Active })
                      .IsUnique();
            });
        }
    }
}
