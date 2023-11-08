using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SME.DbContexts;
using SME.DTO;
using SME.DTO.PurchaseRequestDTO;
using SME.Helper;
using SME.Helper.Approval;
using SME.Helper.Approval.For;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace Procurement.FunctionalService
{
    public class ApprovalTransections
    {
        public readonly ReadDbContext _contextR;
        private readonly SalesOrder _approvalSO;
        private readonly ExpenseAdvance _approvalEA;
        private readonly PO _approvalPO;
        private readonly JV _approvalJV;
        private readonly INVENTORYIR _approvalINVENTORYIR;
        private readonly ProductionOrder _approvalProductionOrder;
        private readonly RFQ _approvalRFQ;
        private readonly PurchaseBill _approvalPurchaseBill;
        private readonly CustomerBill _approvalCustomerBill;
        private readonly SalesCollection _approvalSalesCollection;
        private readonly SalesReturn _approvalSalesReturn;
        private readonly InventoryRequisition _approvalInventoryRequisition;
        private readonly ItemConversion _approvalItemConversion;
        private readonly StockAdjustment _approvalStockAdjustment;
     



        public ApprovalTransections(ReadDbContext contextR, SalesOrder approvalSO, ExpenseAdvance approvalEA, PO approvalPO, JV approvalJV, INVENTORYIR approvalINVENTORYIR, ProductionOrder approvalProductionOrder, RFQ approvalRFQ, PurchaseBill approvalPurchaseBill, CustomerBill approvalCustomerBill, SalesCollection approvalSalesCollection, SalesReturn approvalSalesReturn, InventoryRequisition approvalInventoryRequisition, ItemConversion approvalItemConversion, StockAdjustment approvalStockAdjustment, IApproval approval)
        {
            _contextR = contextR;
            _approvalSO = approvalSO;
            _approvalEA = approvalEA;
            _approvalPO = approvalPO;
            _approvalJV = approvalJV;
            _approvalINVENTORYIR = approvalINVENTORYIR;
            _approvalProductionOrder = approvalProductionOrder;
            _approvalRFQ = approvalRFQ;
            _approvalPurchaseBill = approvalPurchaseBill;
            _approvalCustomerBill = approvalCustomerBill;
            _approvalSalesCollection = approvalSalesCollection;
            _approvalSalesReturn = approvalSalesReturn;
            _approvalInventoryRequisition = approvalInventoryRequisition;
            _approvalItemConversion = approvalItemConversion;
            _approvalStockAdjustment = approvalStockAdjustment;
           
        }


        public async Task<MessageHelper> CommonApproved(long AcountId, long BusinessUnitId, long UserId, long FeatureId, string FeatureName, List<SME.DTO.CommonApprovedDTO> obj, Next next)
        {
            try
            {
                MessageHelper msg = new MessageHelper();

                var userName = await _contextR.Users.Where(x => x.UserId == UserId).Select(x => x.UserName).FirstOrDefaultAsync();

                var appLog = new List<SME.DTO.ApprovalLog>();

                switch (FeatureName)
                {
                    case ModuleFeature.Sales_Order:
                        await _approvalSO.Approve(UserId, userName, obj, next);
                        msg = new MessageHelper { Message = "Sucessfully Approved", statuscode = 200 };
                        break;
                    case ModuleFeature.Expense:
                    case ModuleFeature.Advance:
                        await _approvalEA.Approve(UserId, userName, obj, next);
                        msg = new MessageHelper { Message = "Sucessfully Approved", statuscode = 200 };
                        break;
                    case ModuleFeature.Purchase_Order_Local:
                    case ModuleFeature.Purchase_Order_Foreign:
                    case ModuleFeature.Purchase_Order_Indenting:
                        await _approvalPO.Approve(UserId, userName, obj, next);
                        msg = new MessageHelper { Message = "Sucessfully Approved", statuscode = 200 };
                        break;
                    case ModuleFeature.Journal_Voucher:
                        await _approvalJV.Approve(UserId, userName, obj, next);
                        msg = new MessageHelper { Message = "Sucessfully Approved", statuscode = 200 };
                        break;
                    case ModuleFeature.Production_Order:
                        await _approvalProductionOrder.Approve(UserId, userName, obj, next);
                        msg = new MessageHelper { Message = "Sucessfully Approved", statuscode = 200 };
                        break;
                    case ModuleFeature.RFQ:
                        await _approvalRFQ.Approve(UserId, userName, obj, next);
                        msg = new MessageHelper { Message = "Sucessfully Approved", statuscode = 200 };
                        break;
                    case ModuleFeature.Purchase_Bill:
                        await _approvalPurchaseBill.Approve(UserId, userName, obj, next);
                        msg = new MessageHelper { Message = "Sucessfully Approved", statuscode = 200 };
                        break;
                    case ModuleFeature.Customer_Bill:
                        await _approvalCustomerBill.Approve(UserId, userName, obj, next);
                        msg = new MessageHelper { Message = "Sucessfully Approved", statuscode = 200 };
                        break;
                    case ModuleFeature.Sales_Collection:
                        await _approvalSalesCollection.Approve(UserId, userName, obj, next);
                        msg = new MessageHelper { Message = "Sucessfully Approved", statuscode = 200 };
                        break;
                    case ModuleFeature.Sales_Return:
                        await _approvalSalesReturn.Approve(UserId, userName, obj, next);
                        msg = new MessageHelper { Message = "Sucessfully Approved", statuscode = 200 };
                        break;
                    case ModuleFeature.Inventory_Requisition:
                        await _approvalInventoryRequisition.Approve(UserId, userName, obj, next);
                        msg = new MessageHelper { Message = "Sucessfully Approved", statuscode = 200 };
                        break;
                    case ModuleFeature.Inventory_Item_Conversion:
                        await _approvalItemConversion.Approve(UserId, userName, obj, next);
                        msg = new MessageHelper { Message = "Sucessfully Approved", statuscode = 200 };
                        break;
                    case ModuleFeature.Stock_Adjustment:
                        await _approvalStockAdjustment.Approve(UserId, userName, obj, next);
                        msg = new MessageHelper { Message = "Sucessfully Approved", statuscode = 200 };
                        break;
                    default:
                        throw new Exception("Approval Failed");
                }
                return msg;
            }
            catch (Exception ex)
            {
                throw;
            }
        }




        public async Task<MessageHelper> CommonReject(long AcountId, long BusinessUnitId, long UserId, long FeatureId, string FeatureName, List<CommonApprovedDTO> obj)
        {
            try
            {
                MessageHelper msg = new MessageHelper();


                var userName = await _contextR.Users.Where(x => x.UserId == UserId).Select(x => x.UserName).FirstOrDefaultAsync();

                switch (FeatureName)
                {
                    case ModuleFeature.Sales_Order:
                        await _approvalSO.RejectSalesOrder(UserId, userName, obj, FeatureName);
                        msg = new MessageHelper { Message = "Successfully Rejected", statuscode = 200 };
                        break;
                    case ModuleFeature.Expense:
                    case ModuleFeature.Advance:
                        await _approvalEA.RejectExpenseAdvance(UserId, userName, obj, FeatureName);
                        msg = new MessageHelper { Message = "Successfully Rejected", statuscode = 200 };
                        break;
                    case ModuleFeature.Purchase_Order_Local:
                    case ModuleFeature.Purchase_Order_Foreign:
                    case ModuleFeature.Purchase_Order_Indenting:
                        await _approvalPO.RejectPurchaseOrder(UserId, userName, obj, FeatureName);
                        msg = new MessageHelper { Message = "Successfully Rejected", statuscode = 200 };
                        break;
                    case ModuleFeature.Journal_Voucher:
                        await _approvalJV.RejectJournalVoucher(UserId, userName, obj, FeatureName);
                        msg = new MessageHelper { Message = "Successfully Rejected", statuscode = 200 };
                        break;
                    case ModuleFeature.Production_Order:
                        await _approvalProductionOrder.RejectProductionOrder(UserId, userName, obj, FeatureName);
                        msg = new MessageHelper { Message = "Successfully Rejected", statuscode = 200 };
                        break;
                    case ModuleFeature.RFQ:
                        await _approvalRFQ.RejectRFQ(UserId, userName, obj, FeatureName);
                        msg = new MessageHelper { Message = "Successfully Rejected", statuscode = 200 };
                        break;
                    case ModuleFeature.Purchase_Bill:
                        await _approvalPurchaseBill.RejectPurchaseBill(UserId, userName, obj, FeatureName);
                        msg = new MessageHelper { Message = "Successfully Rejected", statuscode = 200 };
                        break;
                    case ModuleFeature.Customer_Bill:
                        await _approvalCustomerBill.RejectCustomerBill(UserId, userName, obj, FeatureName);
                        msg = new MessageHelper { Message = "Successfully Rejected", statuscode = 200 };
                        break;
                    case ModuleFeature.Sales_Collection:
                        await _approvalSalesCollection.RejectSalesCollection(UserId, userName, obj, FeatureName);
                        msg = new MessageHelper { Message = "Successfully Rejected", statuscode = 200 };
                        break;
                    case ModuleFeature.Sales_Return:
                        await _approvalSalesReturn.RejectSalesReturn(UserId, userName, obj, FeatureName);
                        msg = new MessageHelper { Message = "Successfully Rejected", statuscode = 200 };
                        break;
                    case ModuleFeature.Inventory_Requisition:
                        await _approvalInventoryRequisition.RejectInventoryRequisition(UserId, userName, obj, FeatureName);
                        msg = new MessageHelper { Message = "Successfully Rejected", statuscode = 200 };
                        break;
                    case ModuleFeature.Inventory_Item_Conversion:
                        await _approvalItemConversion.RejectItemConversion(UserId, userName, obj, FeatureName);
                        msg = new MessageHelper { Message = "Successfully Rejected", statuscode = 200 };
                        break;
                    case ModuleFeature.Stock_Adjustment:
                        await _approvalStockAdjustment.RejectStockAdjustment(UserId, userName, obj, FeatureName);
                        msg = new MessageHelper { Message = "Successfully Rejected", statuscode = 200 };
                        break;
                    default:
                        throw new Exception("Reject Failed");
                        //return new Result<MessageHelper>(new ValidationException("Reject Failed"));
                }
                // Clear Approval Pipeline [ALL]
                foreach (var item in obj)
                {
                    await _approvalSO.ClearApprovalPipeline(item.ReffId, item.ReffCode);
                }
                return msg;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<Result<MessageHelper>> InventoryItemRequestApproval(long AcountId, long BusinessUnitId, long UserId, long FeatureId, string FeatureName, PurchaseRequestCommonDTO obj, Next next)
        {
            try
            {
                var userName = await _contextR.Users.Where(x => x.UserId == UserId).Select(x => x.UserName).FirstOrDefaultAsync();

                await _approvalINVENTORYIR.Approve(UserId, userName, obj, next);
                return new MessageHelper { Message = "Sucessfully Approved", statuscode = 200 };

                //if (FeatureName == ModuleFeature.Sales_Order)
                //{

                //}
                //else
                //{
                //    return new Result<MessageHelper>(new ValidationException("Approval Failed"));
                //}
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Result<MessageHelper>> InventoryItemRequestReject(long AcountId, long BusinessUnitId, long UserId, long FeatureId, string FeatureName, PurchaseRequestCommonDTO obj)
        {
            try
            {
                MessageHelper msg = new MessageHelper();


                var userName = await _contextR.Users.Where(x => x.UserId == UserId).Select(x => x.UserName).FirstOrDefaultAsync();


                await _approvalINVENTORYIR.RejectInventoryItemRequest(UserId, userName, obj, FeatureName);
                msg = new MessageHelper { Message = "Successfully Rejected", statuscode = 200 };

                // Clear Approval Pipeline [ALL]

                await _approvalSO.ClearApprovalPipeline(obj.header.RequestId, obj.header.RequestCode);

                return msg;
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public async Task<object> CommonApprovalLog(long TransectionId, string TransectionCode)
        {
            var data = await (
                from head in _contextR.TblApprovalTransectionLogHeads
                join row in _contextR.TblApprovalTransectionLogRows on head.IntId equals row.IntApprovalTransectionHeadLogId into rowG
                from row in rowG.DefaultIfEmpty()
                join appH in _contextR.TblApprovalTransectionHeaders on head.IntApprovalTransectionHeadId equals appH.IntApprovalTransectionId
                join usr in _contextR.Users on head.IntUserId equals usr.UserId
                join emp in _contextR.Employees on usr.EmployeeId equals emp.EmployeeId
                join dept in _contextR.Departments on emp.DepartmentId equals dept.DepartmentId
                join desig in _contextR.Designations on emp.DesignationId equals desig.DesignationId
                where head.IntReferenceId == TransectionId && head.StrReferenceCode == TransectionCode

                select new { head, row, appH, dept, desig, emp }
            ).ToListAsync();

            return data.GroupBy(x => x.head.IntId).Select(x => new SME.DTO.ApprovalLogDto
            {
                Head = new SME.DTO.ApprovalLogHead
                {
                    Id = x.First().head.IntId,
                    ApprovalTransectionHeadId = x.First().head.IntApprovalTransectionHeadId,
                    ApprovalTransectionRowId = x.First().head.IntApprovalTransectionRowId,
                    UserId = x.First().head.IntUserId,
                    UserName = x.First().head.StrUserName,
                    UserCode = x.First().emp.EnrollNumber,
                    ReferenceId = x.First().head.IntReferenceId,
                    ReferenceCode = x.First().head.StrReferenceCode,
                    Remarks = x.First().head.StrRemarks,
                    CreatedDate = x.First().head.DteCreatedDate,
                    AuthTypeName = _contextR.TblApprovalConfigRows.Where(y => y.ApprovalConfigId == x.First().appH.IntStageId && y.IntUserId == x.First().head.IntUserId).Select(y => y.StrAuthorizeType).FirstOrDefault(),
                    DepartmentName = x.First().dept.DepartmentName,
                    DesignationName = x.First().desig.DesignationName,
                    IntType = x.First().appH.ApprovalOrderTypeId,
                    StrType = x.First().appH.ApprovalOrderTypeName
                },
                Row = x.Any(y => y.row == null) ? default : x.Select(y => new SME.DTO.ApprovalLogRow
                {
                    RowId = y.row.IntRowId,
                    ApprovalTransectionHeadLogId = y.row.IntApprovalTransectionHeadLogId,
                    ItemId = y.row.IntItemId,
                    ItemName = y.row.StrItemName,
                    Remarks = y.row.StrRemarks,
                    Quantity = y.row.NumQuantity.Value,
                    Rate = y.row.MonRate.Value,
                    Value = y.row.MonValue.Value,
                    CreatedDate = y.row.DteCreatedDate,
                }).ToList()
            }).ToList();
        }


    }
}
