using SME.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SME.Helper.Approval
{
    public interface IApproval
    {
        public ApprovalDto app { get; set; }
        public ApprovalPostDto approval { get; set; }

        public Task<ApprovalPostDto> CreateApproval(ApprovalDto app, Next next = Next.None);
        public Task<int> ClearApprovalPipeline(long RefId, string RefCode);
        public Task<(long, string)> RoleBackApprovalPipeline(long ApprovalId, long? UserId);
        Task<List<GetApprovelList>> GetApprovalUserList(long RefId, string RefCode);
    }
}