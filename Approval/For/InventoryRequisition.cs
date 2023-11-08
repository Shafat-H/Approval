using SME.DbContexts;
using SME.DTO;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SME.Models.Write;
using SME.DTO.Rfq;

namespace SME.Helper.Approval.For
{
    public class InventoryRequisition : Approval
    {
        private readonly WriteDbContext _contextW;
        public InventoryRequisition(WriteDbContext contextW) : base(contextW)
        {
            _contextW = contextW;
        }
        public async Task Approve(InventoryRequestHeader msg)
        {
            try
            {
                var detalis = _contextW.InventoryRequestHeaders.Where(x => x.InvRequestCode == msg.InvRequestCode).FirstOrDefault();
                var feature = ModuleFeature.Inventory_Requisition;
                var usereDetails = _contextW.Users.Where(x => x.UserId == detalis.ActionById).FirstOrDefault();
                var featureId = _contextW.TblApprovalModuleFeatures.Where(x => x.StrFeatureName == feature).FirstOrDefault();


                var approval = new ApprovalPostDto();

                if (featureId != null)
                {
                    var app = new ApprovalDto
                    {
                        AccountId = detalis.AccountId,
                        UnitId = detalis.BranchId,
                        FeatureId = featureId.IntFeatureId,
                        ModuleId = featureId.IntModuleId,
                        ActionBy = detalis.ActionById,
                        IntRequestEmployeeId = (from user in _contextW.Users
                                                join emp in _contextW.Employees on user.EmployeeId equals emp.EmployeeId
                                                where user.UserId == detalis.ActionById && user.IsActive == true && emp.IsActive == true
                                                select emp.EmployeeId).FirstOrDefault(),
                        ReferenceId = detalis.InvRequestId,
                        ReferenceCode = detalis.InvRequestCode,
                    };

                    approval = await CreateApproval(app);

                    if (approval.PipelineName == null)
                    {
                        detalis.ApproveById = usereDetails.UserId ;
                        await ApproveInventoryRequisition(detalis);
                    }
                }
                else
                {
                    detalis.ApproveById = usereDetails.UserId;
                    await ApproveInventoryRequisition(detalis);
                }
            }
            catch (Exception)
            {

                throw;
            }


            // Invoke SignalR Notification
            //await CreateNotificationInvoke(detalis, approval.UserList);
        }
        public async Task Approve(long UserId, string userName, List<CommonApprovedDTO> obj, Next next = Next.None)
        {
            foreach (var item in obj)
            {
                var detalis = await _contextW.InventoryRequestHeaders.Where(x => x.InvRequestCode == item.ReffCode).FirstOrDefaultAsync();
                detalis.ApproveById = UserId;

                var feature = ModuleFeature.Inventory_Requisition;
                var featureId = _contextW.TblApprovalModuleFeatures.Where(x => x.StrFeatureName == feature).FirstOrDefault();
                var approval = new ApprovalPostDto();

                if (featureId != null)
                {
                    var app = new ApprovalDto
                    {
                        AccountId = detalis.AccountId,
                        UnitId = detalis.BranchId,
                        FeatureId = featureId.IntFeatureId,
                        ModuleId = featureId.IntModuleId,
                        ActionBy = UserId,
                        ActionByName = userName,
                        IntRequestEmployeeId = await (from user in _contextW.Users
                                                      join emp in _contextW.Employees on user.EmployeeId equals emp.EmployeeId
                                                      where user.UserId == detalis.ActionById && user.IsActive == true && emp.IsActive == true
                                                      select emp.EmployeeId).FirstOrDefaultAsync(),
                        ReferenceId = detalis.InvRequestId,
                        ReferenceCode = detalis.InvRequestCode,
                    };

                    // approval = await CreateApproval(app);
                    approval = await CreateApproval(app, next);
                }

                if (approval.PipelineName == null || approval.UserList.Count == 0 || approval.UserList.Select(x => x.UserId).FirstOrDefault() == -1)
                {
                    detalis.ApproveById = UserId;
                    await ApproveInventoryRequisition(detalis);


                    approval.UserList.Clear();
                    //approval.UserList.Add(new NotifyUserDto { UserId = detalis.IntActionBy, Type = PageType.View });

                    // Invoke SignalR Notification
                    //await ApproveNotificationInvoke(detalis, approval.UserList);
                }
                else
                {
                    //if (!approval.UserList.Any(x => x.UserId == detalis.IntActionBy))
                    //{
                    //    approval.UserList.Add(new NotifyUserDto { UserId = detalis.IntActionBy });
                    //}
                    //// Invoke SignalR Notification
                    //await ApproveNotificationInvoke(detalis, approval.UserList);
                }

                // Post Approval For Log
                approval.IntReferenceId = detalis.InvRequestId;
                approval.StrReferenceCode = detalis.InvRequestCode;

                item.FeatureId = featureId.IntFeatureId;
                item.FeatureName = featureId.StrFeatureName;
                await ApprovalLogPost(item, approval);
            }
        }

        public async Task RejectInventoryRequisition(long UserId, string userName, List<CommonApprovedDTO> obj, string FeatureName)
        {

            foreach (var item in obj)
            {
                var detalis = await _contextW.InventoryRequestHeaders.Where(x => x.InvRequestCode == item.ReffCode).FirstOrDefaultAsync();


                var featureId = _contextW.TblApprovalModuleFeatures.Where(x => x.StrFeatureName == FeatureName).FirstOrDefault();
                var approval = new ApprovalPostDto();

                if (featureId != null)
                {
                    var app = new ApprovalDto
                    {
                        AccountId = detalis.AccountId,
                        UnitId = detalis.BranchId,
                        FeatureId = featureId.IntFeatureId,
                        ModuleId = featureId.IntModuleId,
                        ActionBy = UserId,
                        ActionByName = userName,
                        IntRequestEmployeeId = await (from user in _contextW.Users
                                                      join emp in _contextW.Employees on user.EmployeeId equals emp.EmployeeId
                                                      where user.UserId == detalis.ActionById && user.IsActive == true && emp.IsActive == true
                                                      select emp.EmployeeId).FirstOrDefaultAsync(),
                        ReferenceId = detalis.InvRequestId,
                        ReferenceCode = detalis.InvRequestCode,
                    };

                    approval = await RejectApproval(app);
                }

                detalis.IsActive = false;
                detalis.ActionById = UserId;
                detalis.LastActionDateTime = DateTime.Now;

                _contextW.InventoryRequestHeaders.Update(detalis);
                await _contextW.SaveChangesAsync();


                // Post Approval For Log
                approval.IntReferenceId = detalis.InvRequestId;
                approval.StrReferenceCode = detalis.InvRequestCode;

                CommonApprovedDTO items = new CommonApprovedDTO();
                items.FeatureId = featureId.IntFeatureId;
                items.FeatureName = featureId.StrFeatureName;
                items.Remarks = item.Remarks;

                await ApprovalLogPost(items, approval);



                // Invoke SignalR Notification

                //await RejectNotificationInvoke(detalis, new List<NotifyUserDto> { new NotifyUserDto { UserId = detalis.IntActionBy } });

            }
        }

        private async Task ApproveInventoryRequisition(InventoryRequestHeader data)
        {
            data.IsApprove = true;
            data.ApproveDate = DateTime.Now;

            _contextW.InventoryRequestHeaders.Update(data);
            await _contextW.SaveChangesAsync();

            var row = await Task.FromResult((from a in _contextW.InventoryRequestRows
                                             where a.InvRequestId == data.InvRequestId && a.IsActive == true
                                             select a).ToList());
            row.ForEach(x => x.ApproveQuantity = x.RequestQuantity);

            _contextW.InventoryRequestRows.UpdateRange(row);
            await _contextW.SaveChangesAsync();
        }
        private async Task ApprovalLogPost(CommonApprovedDTO item, ApprovalPostDto approval)
        {
            var logHead = new TblApprovalTransectionLogHead
            {
                IntApprovalTransectionHeadId = approval.UserHeadId,
                IntApprovalTransectionRowId = approval.UserRowId,
                IntUserId = approval.ApproveUserId,
                StrUserName = approval.ApproveUserName,
                IntReferenceId = approval.IntReferenceId,
                StrReferenceCode = approval.StrReferenceCode,
                StrRemarks = item.Remarks,
                DteCreatedDate = DateTime.Now,
                IntType = item.FeatureId,
                StrType = item.FeatureName
            };

            await _contextW.TblApprovalTransectionLogHeads.AddAsync(logHead);
            await _contextW.SaveChangesAsync();
        }
    }
}
