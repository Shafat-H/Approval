using DocumentFormat.OpenXml.Wordprocessing;
using LanguageExt.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Procurement.FunctionalService;
using SME.DTO;
using SME.DTO.PurchaseRequestDTO;
using SME.Helper;
using SME.Helper.Approval;
using SME.StoreProcedure;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SME.Controllers
{
    [Route("sme/[controller]")]
    [ApiController]
    public class MultilayerApprovalController : ControllerBase
    {
        private readonly ApprovalTransections _Repository;
        private readonly IApproval _Approval;
        public MultilayerApprovalController(ApprovalTransections Repository, IApproval Approval)
        {
            _Repository = Repository;
            _Approval = Approval;
        }
        [HttpPut]
        [Route("CommonApproved")]
        [SwaggerOperation(Description = "Example {}")]
        public async Task<IActionResult> CommonApproved(long AcountId, long BusinessUnitId, long UserId, long FeatureId, string FeatureName, List<CommonApprovedDTO> obj, Next next = Next.None)
        {
           
            var dt = await _Repository.CommonApproved(AcountId, BusinessUnitId, UserId, FeatureId, FeatureName, obj, next);
            return Ok(JsonConvert.SerializeObject(dt));

        }

        [HttpPut]
        [Route("CommonReject")]
        [SwaggerOperation(Description = "Example {}")]
        public async Task<IActionResult> CommonReject(long AcountId, long BusinessUnitId, long UserId, long FeatureId, string FeatureName, List<CommonApprovedDTO> obj)
        {

            var dt = await _Repository.CommonReject(AcountId, BusinessUnitId, UserId, FeatureId, FeatureName, obj);
            return Ok(JsonConvert.SerializeObject(dt));

        }
        [HttpGet]
        [Route("CoomonApprovalList")]
        [SwaggerOperation(Description = "Example {}")]
        public async Task<ActionResult> CoomonApprovalList(long AcountId, long BusinessUnitId, long UserId, long FeatureId, string FeatureName, long PageNo, long PageSize, string Search, bool IsSummary = false, bool IsSupperApprover = false)
        {
            var dt = CommonApprovalList.ApprovalList(AcountId, BusinessUnitId, UserId, FeatureId, FeatureName, PageNo, PageSize, Search, IsSummary, IsSupperApprover);
            return Ok(JsonConvert.SerializeObject(dt));
        }

        [HttpPut]
        [Route("InventoryItemRequestApproval")]
        [SwaggerOperation(Description = "Example {}")]
        public async Task<IActionResult> InventoryItemRequestApproval(long AcountId, long BusinessUnitId, long UserId, long FeatureId, string FeatureName, PurchaseRequestCommonDTO obj, Next next)
        {
            var dt = await _Repository.InventoryItemRequestApproval(AcountId, BusinessUnitId, UserId, FeatureId, FeatureName, obj, next);
            return Ok(JsonConvert.SerializeObject(dt));
        }
        [HttpPut]
        [Route("InventoryItemRequestReject")]
        [SwaggerOperation(Description = "Example {}")]
        public async Task<IActionResult> InventoryItemRequestReject(long AcountId, long BusinessUnitId, long UserId, long FeatureId, string FeatureName, PurchaseRequestCommonDTO obj)
        {
            var dt = await _Repository.InventoryItemRequestReject(AcountId, BusinessUnitId, UserId, FeatureId, FeatureName, obj);
            return Ok(JsonConvert.SerializeObject(dt));
        }
        [HttpGet]
        [Route("GetApprovalUserList")]
        [SwaggerOperation(Description = "Example {}")]
        public async Task<IActionResult> GetApprovalUserList(long RefId, string RefCode)
        {
            var dt = await _Approval.GetApprovalUserList(RefId, RefCode);
            return Ok(JsonConvert.SerializeObject(dt));
        }
    }
}
