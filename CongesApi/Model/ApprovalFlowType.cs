using System.ComponentModel.DataAnnotations;

namespace CongesApi.Model
{
    public class ApprovalFlowType
    {
        [Key]                    // ✅ indispensable
        public string FlowType { get; set; }
    }
}
