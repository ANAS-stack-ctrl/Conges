using System;
using System.ComponentModel.DataAnnotations;

namespace CongesApi.Model
{
    public class Document
    {
        [Key]
        public int DocumentId { get; set; }

        public string StoragePath { get; set; }
        public string FileType { get; set; }
        public string FileName { get; set; }

        public bool IsVerified { get; set; }
        public bool IsRequired { get; set; }

        public DateTime UploadDate { get; set; }
        public DateTime? VerificationDate { get; set; }

        public int LeaveRequestId { get; set; }
        public LeaveRequest LeaveRequest { get; set; }

        public int UploadedByUserId { get; set; }
        public User UploadedBy { get; set; }

        public int? VerifiedByUserId { get; set; }
        public string Category { get; set; }
        public DocumentCategory DocumentCategory { get; set; }
    }
}
