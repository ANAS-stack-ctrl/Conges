using Microsoft.EntityFrameworkCore;
using CongesApi.Model;
using System.Threading;
using System.Threading.Tasks;

namespace CongesApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Tables principales
        public DbSet<User> Users { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<LeavePolicy> LeavePolicies { get; set; }
        public DbSet<Approval> Approvals { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<LeaveBalanceAdjustment> LeaveBalanceAdjustments { get; set; }
        public DbSet<Holiday> Holidays { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // Lookup Tables (ENUM simulés)
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<ApprovalLevel> ApprovalLevels { get; set; }
        public DbSet<LeaveStatus> LeaveStatuses { get; set; }
        public DbSet<ApprovalFlowType> ApprovalFlowTypes { get; set; }
        public DbSet<NotificationType> NotificationTypes { get; set; }
        public DbSet<HalfDayPeriodType> HalfDayPeriodTypes { get; set; }
        public DbSet<DocumentCategory> DocumentCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Lookup tables – clés primaires explicites
            modelBuilder.Entity<ApprovalFlowType>().HasKey(a => a.FlowType);
            modelBuilder.Entity<UserRole>().HasKey(r => r.Role);
            modelBuilder.Entity<ApprovalLevel>().HasKey(l => l.Level);
            modelBuilder.Entity<LeaveStatus>().HasKey(s => s.Status);
            modelBuilder.Entity<NotificationType>().HasKey(n => n.Type);
            modelBuilder.Entity<HalfDayPeriodType>().HasKey(p => p.PeriodType);
            modelBuilder.Entity<DocumentCategory>().HasKey(d => d.Category);

            // Clés explicites
            modelBuilder.Entity<AuditLog>().HasKey(a => a.LogId);
            modelBuilder.Entity<LeaveBalanceAdjustment>().HasKey(a => a.AdjustmentId);

            // Relation User -> UserRole
            modelBuilder.Entity<User>()
                .HasOne(u => u.UserRole)
                .WithMany()
                .HasForeignKey(u => u.Role)
                .HasPrincipalKey(r => r.Role);

            // Relations LeaveType → ApprovalFlowType et LeavePolicy
            modelBuilder.Entity<LeaveType>()
                .HasOne(t => t.ApprovalFlowType)
                .WithMany()
                .HasForeignKey(t => t.ApprovalFlow)
                .HasPrincipalKey(a => a.FlowType);

            modelBuilder.Entity<LeaveType>()
                .HasOne(t => t.Policy)
                .WithMany()
                .HasForeignKey(t => t.PolicyId);

            // Relations LeaveRequest → User et LeaveType
            modelBuilder.Entity<LeaveRequest>()
                .HasOne(r => r.User)
                .WithMany(u => u.LeaveRequests)
                .HasForeignKey(r => r.UserId);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(r => r.LeaveType)
                .WithMany()
                .HasForeignKey(r => r.LeaveTypeId);

            // Relation Approval → User (ApprovedBy)
            modelBuilder.Entity<Approval>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Approval>()
                .HasOne(a => a.LeaveRequest)
                .WithMany(lr => lr.Approvals)
                .HasForeignKey(a => a.LeaveRequestId);

            modelBuilder.Entity<Document>()
                .HasOne(d => d.UploadedBy)
                .WithMany()
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Document>()
                .HasOne(d => d.LeaveRequest)
                .WithMany()
                .HasForeignKey(d => d.LeaveRequestId);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId);

            modelBuilder.Entity<LeaveBalanceAdjustment>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveBalanceAdjustment>()
                .HasOne(l => l.LeaveRequest)
                .WithMany()
                .HasForeignKey(l => l.LeaveRequestId);

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveBalanceAdjustment>()
                .Property(p => p.NewBalance)
                .HasPrecision(18, 2);

            modelBuilder.Entity<LeaveBalanceAdjustment>()
                .Property(p => p.OldBalance)
                .HasPrecision(18, 2);

            modelBuilder.Entity<User>()
                .Property(p => p.CurrentLeaveBalance)
                .HasPrecision(18, 2);
        }

        // ❌ Supprimé : plus de hash automatique ici !
    }
}
