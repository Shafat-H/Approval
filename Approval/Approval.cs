
using Microsoft.EntityFrameworkCore;
using SME.DbContexts;
using SME.DTO;
using SME.Models.Write;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SME.Helper.Approval
{
    public class Approval : IApproval
    {
        private readonly WriteDbContext _contextR;
        public Approval(WriteDbContext contextR)
        {
            _contextR = contextR;
        }
        // Incomming
        public ApprovalDto app { get; set; }
        // Outgoing
        public ApprovalPostDto approval { get; set; }

        public async Task<int> ClearApprovalPipeline(long RefId, string RefCode)
        {
            var result = 0;

            var apps = await _contextR.TblApprovalTransectionHeaders.Where(x => x.IntReferenceId == RefId && x.StrReferenceCode == RefCode && x.IsApproved == false && x.IsActive == true).ToListAsync();

            apps.ForEach(x => { x.IsActive = false; x.IsApproved = true; });
            _contextR.TblApprovalTransectionHeaders.UpdateRange(apps);
            result += await _contextR.SaveChangesAsync();

            return result;
        }
        public async Task<List<GetApprovelList>> GetApprovalUserList(long RefId, string RefCode)
        {
            var UserList = await (from aph in _contextR.TblApprovalTransectionHeaders
                                  join apr in _contextR.TblApprovalTransectionRows on aph.IntApprovalTransectionId equals apr.IntApprovalTransectionId
                                  join stg in _contextR.TblApprovalConfigHeaders on aph.IntStageId equals stg.ApprovalConfigId
                                  join user in _contextR.Users on apr.IntUserId equals user.UserId
                                  join ebi in _contextR.Employees on user.EmployeeId equals ebi.EmployeeId
                                  join edept in _contextR.Departments on ebi.DepartmentId equals edept.DepartmentId into temp
                                  from edept in temp.DefaultIfEmpty()
                                  join ed in _contextR.Designations on ebi.DesignationId equals ed.DesignationId
                                  where aph.IntReferenceId == RefId && aph.StrReferenceCode == RefCode && aph.IsActive == true && aph.IsLinemanger == false && aph.IsSupervisor == false
                                  select new GetApprovelList
                                  {
                                      ApprovalType = ApprovalUserType.User,
                                      RequestEmployeeId = aph.IntRequestEmployeeId.Value,
                                      ApprovalTransectionId = aph.IntApprovalTransectionId,
                                      ApprovalTransectionRowId = apr.IntRowId,
                                      UserId = apr.IntUserId,
                                      UserName = apr.StrUserName,
                                      UserNameDesignationName = $"{apr.StrUserName} ({ed.DesignationName}, {edept.DepartmentName ?? ""})",
                                      StageName = stg.StrStageName,
                                      OrderTypeId = aph.ApprovalOrderTypeId,
                                      OrderTypeName = aph.ApprovalOrderTypeName,
                                      SequenceId = apr.IntSequenceId,
                                      IsApprove = apr.IsReject == true ? 2 : apr.IsApprove == true ? 1 : 0,

                                      Comments = _contextR.TblApprovalTransectionLogHeads.Where(x => x.IntApprovalTransectionHeadId == aph.IntApprovalTransectionId && x.IntApprovalTransectionRowId == apr.IntRowId && x.IntUserId == apr.IntUserId).Select(x => x.StrRemarks).FirstOrDefault(),

                                      //   Date = _contextR.TblApprovalTransectionLogHeads.Where(x => x.IntApprovalTransectionHeadId == aph.IntApprovalTransectionId && x.IntApprovalTransectionRowId == apr.IntRowId && x.IntUserId == apr.IntUserId).Select(x => x.DteCreatedDate).FirstOrDefault()
                                  }).ToListAsync();

            var SupperUserList = await (from aph in _contextR.TblApprovalTransectionHeaders
                                        join apr in _contextR.TblApprovalTransectionRows on aph.IntApprovalTransectionId equals apr.IntApprovalTransectionId
                                        join stg in _contextR.TblApprovalConfigHeaders on aph.IntStageId equals stg.ApprovalConfigId

                                        where aph.IntReferenceId == RefId && aph.StrReferenceCode == RefCode && aph.IsActive == true && (aph.IsLinemanger == true || aph.IsSupervisor == true)
                                        select new GetApprovelList
                                        {
                                            ApprovalType = aph.IsLinemanger == true ? ApprovalUserType.LineManager : aph.IsSupervisor == true ? ApprovalUserType.Supervisor : ApprovalUserType.User,
                                            RequestEmployeeId = aph.IntRequestEmployeeId.Value,
                                            ApprovalTransectionId = aph.IntApprovalTransectionId,
                                            ApprovalTransectionRowId = apr.IntRowId,
                                            UserId = apr.IntUserId,
                                            UserName = apr.StrUserName,
                                            StageName = stg.StrStageName,
                                            OrderTypeId = aph.ApprovalOrderTypeId,
                                            OrderTypeName = aph.ApprovalOrderTypeName,
                                            SequenceId = apr.IntSequenceId,
                                            IsApprove = apr.IsReject == true ? 2 : apr.IsApprove == true ? 1 : 0,

                                            Comments = _contextR.TblApprovalTransectionLogHeads.Where(x => x.IntApprovalTransectionHeadId == aph.IntApprovalTransectionId && x.IntApprovalTransectionRowId == apr.IntRowId && x.IntUserId == apr.IntUserId).Select(x => x.StrRemarks).FirstOrDefault(),

                                            // Date = _contextR.TblApprovalTransectionLogHeads.Where(x => x.IntApprovalTransectionHeadId == aph.IntApprovalTransectionId && x.IntApprovalTransectionRowId == apr.IntRowId && x.IntUserId == apr.IntUserId).Select(x => x.DteCreatedDate).FirstOrDefault()
                                        }).ToListAsync();

            foreach (var user in SupperUserList)
            {
                if (user.ApprovalType == ApprovalUserType.LineManager)
                {
                    var linemanager = await (
                        from r_User in _contextR.Employees
                        join l_User in _contextR.Employees on r_User.LineManagerId equals l_User.EmployeeId
                        join emp_Dpt in _contextR.Departments on l_User.DepartmentId equals emp_Dpt.DepartmentId into temp
                        from emp_Dpt in temp.DefaultIfEmpty()
                        join emp_Des in _contextR.Designations on l_User.DesignationId equals emp_Des.DesignationId

                        where r_User.EmployeeId == user.RequestEmployeeId
                        select new { l_User, emp_Dpt, emp_Des }
                    ).FirstOrDefaultAsync();

                    user.UserName = user.UserName == ApprovalUserTypeName.LineManager ? linemanager.l_User.EmployeeName : user.UserName;
                    user.UserNameDesignationName = $"{user.UserName} ({linemanager.emp_Des.DesignationName}, {linemanager.emp_Dpt.DepartmentName ?? ""})";
                }
                else if (user.ApprovalType == ApprovalUserType.Supervisor)
                {
                    var supervisor = await (
                        from r_User in _contextR.Employees
                        join s_User in _contextR.Employees on r_User.SupervisorId equals s_User.EmployeeId
                        join emp_Dpt in _contextR.Departments on s_User.DepartmentId equals emp_Dpt.DepartmentId into temp
                        from emp_Dpt in temp.DefaultIfEmpty()
                        join emp_Des in _contextR.Designations on s_User.DesignationId equals emp_Des.DesignationId

                        where r_User.EmployeeId == user.RequestEmployeeId
                        select new { s_User, emp_Dpt, emp_Des }
                    ).FirstOrDefaultAsync();

                    user.UserName = user.UserName == ApprovalUserTypeName.Supervisor ? supervisor.s_User.EmployeeName : user.UserName;
                    user.UserNameDesignationName = $"{user.UserName} ({supervisor.emp_Des.DesignationName}, {supervisor.emp_Dpt.DepartmentName ?? ""})";
                }
                else
                {
                    user.UserName = "N/A";
                    user.UserNameDesignationName = "N/A";
                }
            }

            UserList.AddRange(SupperUserList);

            return UserList.Distinct().OrderBy(x => x.ApprovalTransectionId).ThenBy(x => x.SequenceId).ToList();
        }

        public async Task<ApprovalPostDto> CreateApproval(ApprovalDto app, Next next = Next.None)
        {
            // Skipper
            if (next != Next.None)
                return await PassApproval(app, next);

            try
            {
                approval = new ApprovalPostDto();

                var pipeline = _contextR.TblApprovalPipelines.Where(a => a.IntAccountId == app.AccountId && a.IntUnitId == app.UnitId && a.IntActivityFeatureId == app.FeatureId && a.IsActive == true).FirstOrDefault();

                if (pipeline == null)
                    return approval;

                pipeline.IntPipelineId = pipeline.IntPipelineId;
                long stage, OrderType;
                bool isSupervisor = false, isLinemanager = false;

                var approvalHeadQuery = _contextR.TblApprovalTransectionHeaders.Where(x => x.IntAccountId == app.AccountId && x.IntUnitId == app.UnitId && x.FeatureId == pipeline.IntActivityFeatureId && x.IntPipeLineId == pipeline.IntPipelineId && x.IntReferenceId == app.ReferenceId && x.StrReferenceCode == app.ReferenceCode && x.IsActive == true && x.IsApproved == false);

                var approvalHead = approvalHeadQuery.OrderBy(x => x.IntApprovalTransectionId).FirstOrDefault();

                // init approval setup
                if (approvalHead == null)
                {
                    var firstStage = _contextR.TblApprovalConfigHeaders.Where(x => x.IntPipelineId == pipeline.IntPipelineId && x.IsActive == true).OrderBy(x => x.ApprovalConfigId).FirstOrDefault();

                    if (firstStage == null)
                        return approval;

                    approval.PipelineName = pipeline.StrPipelineName;
                    stage = firstStage.ApprovalConfigId;
                    OrderType = ApprovalOrder.GetApprovelOrderId(firstStage);
                    isSupervisor = firstStage.IsSupervisor;
                    isLinemanager = firstStage.IsLinemanger;
                }
                // update exisiting approval setup
                else
                {
                    stage = approvalHead.IntStageId.Value;

                    approval.ApprovalId = approvalHead.IntApprovalTransectionId;
                    approval.StageId = stage;
                    approval.PipelineId = pipeline.IntPipelineId;
                    approval.PipelineName = pipeline.StrPipelineName;
                    approval.ApproveUserId = app.ActionBy;
                    approval.ApproveUserName = app.ActionByName;

                    if (approvalHead.IsSupervisor == true || approvalHead.IsLinemanger == true)
                    {
                        // here ActionBy is user whom approving
                        var userApproval = _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId).FirstOrDefault();

                        userApproval.IsApprove = true;
                        userApproval.IntUserId = app.ActionBy;
                        userApproval.StrUserName = app.ActionByName;

                        _contextR.TblApprovalTransectionRows.UpdateRange(userApproval);
                        await _contextR.SaveChangesAsync();

                        approval.UserRowId = userApproval.IntRowId;
                        approval.UserHeadId = userApproval.IntApprovalTransectionId;
                        approvalHead.IsApproved = true;
                    }
                    else if (approvalHead.ApprovalOrderTypeId == (int)ApprovalOrderType.Any_Person)
                    {
                        // here ActionBy is user whom approving
                        var userApproval = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId && x.IntUserId == app.ActionBy && x.IsApprove == false).FirstOrDefaultAsync();

                        userApproval.IsApprove = true;
                        _contextR.TblApprovalTransectionRows.Update(userApproval);
                        await _contextR.SaveChangesAsync();

                        approval.UserRowId = userApproval.IntRowId;
                        approval.UserHeadId = userApproval.IntApprovalTransectionId;

                        var userLimit = await _contextR.TblApprovalConfigHeaders.Where(x => x.ApprovalConfigId == stage).Select(x => x.IntAnyUsers).FirstOrDefaultAsync();
                        var userApprovalCount = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId && x.IsApprove == true).CountAsync();

                        // Approval Pending for others
                        if (userLimit > userApprovalCount)
                        {
                            approval.UserList.AddRange(
                                await _contextR.TblApprovalTransectionRows
                                .Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId && x.IsApprove == false)
                                .Select(x => new NotifyUserDtos { UserId = x.IntUserId.Value, Type = PageType.Approve }).ToListAsync()
                                );

                            return approval;
                        }

                        approvalHead.IsApproved = true;
                    }
                    else if (approvalHead.ApprovalOrderTypeId == (int)ApprovalOrderType.Any_Order)
                    {
                        // here ActionBy is user whom approving
                        var userApproval = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId && x.IntUserId == app.ActionBy && x.IsApprove == false).FirstOrDefaultAsync();

                        userApproval.IsApprove = true;
                        _contextR.TblApprovalTransectionRows.Update(userApproval);
                        await _contextR.SaveChangesAsync();

                        approval.UserRowId = userApproval.IntRowId;
                        approval.UserHeadId = userApproval.IntApprovalTransectionId;

                        var userApprovalList = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId && x.IsApprove == false)
                        .Select(x => new NotifyUserDtos { UserId = x.IntUserId.Value, Type = PageType.Approve }).ToListAsync();

                        // Approval Pending for others
                        if (userApprovalList.Count() > 0)
                        {
                            approval.UserList.AddRange(userApprovalList);
                            return approval;
                        }

                        approvalHead.IsApproved = true;
                    }
                    else if (approvalHead.ApprovalOrderTypeId == (int)ApprovalOrderType.In_Sequence)
                    {
                        // here ActionBy is user whom approving
                        var userApproval = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId && x.IntUserId == app.ActionBy && x.IsApprove == false).FirstOrDefaultAsync();

                        userApproval.IsApprove = true;
                        _contextR.TblApprovalTransectionRows.Update(userApproval);
                        await _contextR.SaveChangesAsync();

                        approval.UserRowId = userApproval.IntRowId;
                        approval.UserHeadId = userApproval.IntApprovalTransectionId;

                        var userList = await _contextR.TblApprovalConfigRows.Where(x => x.ApprovalConfigId == stage && x.IsActive == true && x.IntSequenceId > userApproval.IntSequenceId).ToListAsync();

                        if (userList.Count > 0)
                        {
                            var user = (from a in userList
                                        where a.ApprovalConfigId == stage && a.IsActive == true && a.IntSequenceId > userApproval.IntSequenceId
                                        orderby a.IntSequenceId ascending
                                        select new TblApprovalTransectionRow()
                                        {
                                            IntApprovalTransectionId = userApproval.IntApprovalTransectionId,
                                            IntUserId = (long)a.IntUserId,
                                            StrUserName = a.StrUserName,
                                            IsApprove = false,
                                            IntSequenceId = a.IntSequenceId
                                        }).FirstOrDefault();

                            await _contextR.TblApprovalTransectionRows.AddAsync(user);
                            await _contextR.SaveChangesAsync();

                            approval.UserList.Add(new NotifyUserDtos { UserId = user.IntUserId.Value, Type = PageType.Approve });

                            return approval;
                        }

                        approvalHead.IsApproved = true;
                    }
                    else
                        return approval;

                    // Set Next Stage
                    var firstStage = await _contextR.TblApprovalConfigHeaders.Where(x => x.IntPipelineId == pipeline.IntPipelineId && x.ApprovalConfigId > stage && x.IsActive == true).OrderBy(x => x.ApprovalConfigId).FirstOrDefaultAsync();

                    // if all approval stage complete
                    if (firstStage == null)
                    {
                        approval.UserList.Add(new NotifyUserDtos { UserId = -1 });
                        return approval;
                    }

                    stage = firstStage.ApprovalConfigId;
                    OrderType = ApprovalOrder.GetApprovelOrderId(firstStage);
                    isSupervisor = firstStage.IsSupervisor;
                    isLinemanager = firstStage.IsLinemanger;
                }

                // Insert New Stage For Approval
                var aph = new TblApprovalTransectionHeader
                {
                    IntPipeLineId = pipeline.IntPipelineId,
                    IsApproved = false,
                    IntAccountId = pipeline.IntAccountId.Value,
                    IntUnitId = pipeline.IntUnitId.Value,
                    IntRequestEmployeeId = app.IntRequestEmployeeId,
                    DteLastActionDateTime = DateTime.Now,
                    DteServerDateTime = DateTime.Now,
                    IsActive = true,
                    ApprovalOrderTypeId = OrderType,
                    ApprovalOrderTypeName = ApprovalOrder.GetApprovelOrderName(OrderType),
                    ModuleId = pipeline.IntModuleId,
                    ModuleName = pipeline.StrModuleName,
                    FeatureId = pipeline.IntActivityFeatureId,
                    FeatureName = string.IsNullOrWhiteSpace(app.FeatureName) ? pipeline.StrActivityFeatureName : app.FeatureName,
                    IntReferenceId = app.ReferenceId,
                    StrReferenceCode = app.ReferenceCode,
                    IntStageId = stage,
                    IsSupervisor = isSupervisor,
                    IsLinemanger = isLinemanager
                };

                var arow = new List<TblApprovalTransectionRow>();

                // isSupervisor isLinemanager
                if (isSupervisor || isLinemanager)
                {
                    arow = await (from a in _contextR.TblApprovalConfigRows
                                  where a.ApprovalConfigId == stage && a.IsActive == true
                                  select new TblApprovalTransectionRow()
                                  {
                                      IntApprovalTransectionId = aph.IntApprovalTransectionId,
                                      IntUserId = a.IntUserId,
                                      StrUserName = a.StrUserName,
                                      //   StrUserName = isSupervisor ? "Supervisor" : "Line-Manager",
                                      IsApprove = false
                                  }).ToListAsync();
                }
                // For In_Sequence, Post 1st User To Approval
                else if (OrderType == (int)ApprovalOrderType.In_Sequence)
                {
                    var user = (from a in _contextR.TblApprovalConfigRows
                                where a.ApprovalConfigId == stage && a.IsActive == true
                                orderby a.RowId ascending
                                select new TblApprovalTransectionRow()
                                {
                                    IntApprovalTransectionId = aph.IntApprovalTransectionId,
                                    IntUserId = (long)a.IntUserId,
                                    StrUserName = a.StrUserName,
                                    IsApprove = false
                                }).FirstOrDefault();

                    arow.Add(user);
                }
                else
                {
                    arow = (from a in _contextR.TblApprovalConfigRows
                            where a.ApprovalConfigId == stage && a.IsActive == true
                            select new TblApprovalTransectionRow()
                            {
                                IntApprovalTransectionId = aph.IntApprovalTransectionId,
                                IntUserId = (long)a.IntUserId,
                                StrUserName = a.StrUserName,
                                IsApprove = false
                            }).ToList();
                }

                // if stage missing any user list
                if (arow.Count <= 0)
                {
                    // to avoid stage user missing risk
                    approval.PipelineName = null;
                    approval.UserList.Add(new NotifyUserDtos { UserId = -1 });
                    return approval;
                }

                var Sequence = 1;

                _contextR.TblApprovalTransectionHeaders.Add(aph);
                _contextR.SaveChanges();

                arow.ForEach(x => { x.IntSequenceId = Sequence++; x.IntApprovalTransectionId = aph.IntApprovalTransectionId; });

                _contextR.TblApprovalTransectionRows.AddRange(arow);
                _contextR.SaveChanges();

                if (isSupervisor)
                {
                    var supervisor = (
                        from emp in _contextR.Employees
                        join user in _contextR.Users on emp.SupervisorId equals user.EmployeeId
                        where emp.EmployeeId == app.IntRequestEmployeeId
                        select user.UserId
                    ).ToList();

                    approval.UserList.AddRange(supervisor.Select(x => new NotifyUserDtos { UserId = x, Type = PageType.Approve }).ToList());
                    return approval;
                }
                else if (isLinemanager)
                {
                    var linemanager = (
                        from emp in _contextR.Employees
                        join user in _contextR.Users on emp.LineManagerId equals user.EmployeeId
                        where emp.EmployeeId == app.IntRequestEmployeeId
                        select user.UserId
                    ).ToList();

                    approval.UserList.AddRange(linemanager.Select(x => new NotifyUserDtos { UserId = x, Type = PageType.Approve }).ToList());
                    return approval;
                }
                else
                {
                    approval.UserList.AddRange(arow.Select(x => new NotifyUserDtos { UserId = x.IntUserId.Value, Type = PageType.Approve }).ToList());
                    return approval;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private async Task<ApprovalPostDto> PassApproval(ApprovalDto app, Next next = Next.User)
        {
            try
            {
                approval = new ApprovalPostDto();

                var pipeline = await _contextR.TblApprovalPipelines.Where(a => a.IntAccountId == app.AccountId && a.IntUnitId == app.UnitId && a.IntActivityFeatureId == app.FeatureId && a.IsActive == true).FirstOrDefaultAsync();

                if (pipeline == null)
                    return approval;

                pipeline.IntPipelineId = pipeline.IntPipelineId;
                long stage, OrderType;
                bool isSupervisor = false, isLinemanager = false;

                var approvalHeadQuery = _contextR.TblApprovalTransectionHeaders.Where(x => x.IntUnitId == app.UnitId && x.FeatureId == pipeline.IntActivityFeatureId && x.IntPipeLineId == pipeline.IntPipelineId && x.IntReferenceId == app.ReferenceId && x.StrReferenceCode == app.ReferenceCode && x.IsActive == true && x.IsApproved == false);

                var approvalHead = await approvalHeadQuery.OrderBy(x => x.IntApprovalTransectionId).FirstOrDefaultAsync();

                // init approval setup
                if (approvalHead == null)
                    return approval;
                // update exisiting approval setup
                else
                {
                    stage = approvalHead.IntStageId.Value;

                    approval.ApprovalId = approvalHead.IntApprovalTransectionId;
                    approval.StageId = stage;
                    approval.PipelineId = pipeline.IntPipelineId;
                    approval.PipelineName = pipeline.StrPipelineName;
                    approval.ApproveUserId = app.ActionBy;
                    approval.ApproveUserName = app.ActionByName;

                    if (next == Next.Clear)
                    {
                        // here ActionBy is user whom approving
                        var userApproval = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId)
                        .OrderByDescending(x => x.IntRowId)
                        .Select(x => new TblApprovalTransectionRow
                        {
                            IntApprovalTransectionId = x.IntApprovalTransectionId,
                            IntUserId = app.ActionBy,
                            StrUserName = app.ActionByName,
                            NumThreshold = x.NumThreshold,
                            IntSequenceId = x.IntSequenceId + 1,
                            IsApprove = true,
                        }).FirstOrDefaultAsync();

                        await _contextR.TblApprovalTransectionRows.AddAsync(userApproval);
                        await _contextR.SaveChangesAsync();

                        approval.UserRowId = userApproval.IntRowId;
                        approval.UserHeadId = userApproval.IntApprovalTransectionId;
                        approvalHead.IsApproved = true;

                        approval.UserList.Add(new NotifyUserDtos { UserId = -1 });

                        _contextR.TblApprovalTransectionHeaders.Update(approvalHead);
                        await _contextR.SaveChangesAsync();

                        return approval;
                    }
                    else if (approvalHead.IsSupervisor == true || approvalHead.IsLinemanger == true)
                    {
                        // here ActionBy is user whom approving
                        var lineOrSupManager = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId).OrderByDescending(x => x.IntRowId).FirstOrDefaultAsync();

                        if (approvalHead.IsLinemanger == true)
                        {
                            var linemanager = await (
                                from r_User in _contextR.TblEmployeeBasicInfos
                                join l_User in _contextR.TblEmployeeBasicInfos on r_User.IntLineManagerId equals l_User.IntEmployeeId
                                join user in _contextR.TblUsers on l_User.IntEmployeeId equals user.IntUserReferenceId

                                where r_User.IntEmployeeId == approvalHead.IntRequestEmployeeId
                                select user
                            ).FirstOrDefaultAsync();

                            lineOrSupManager.IntUserId = linemanager.IntUserId;
                            lineOrSupManager.StrUserName = linemanager.StrUserName;
                        }
                        else if (approvalHead.IsSupervisor == true)
                        {
                            var linemanager = await (
                                from r_User in _contextR.TblEmployeeBasicInfos
                                join l_User in _contextR.TblEmployeeBasicInfos on r_User.IntSupervisorId equals l_User.IntEmployeeId
                                join user in _contextR.TblUsers on l_User.IntEmployeeId equals user.IntUserReferenceId

                                where r_User.IntEmployeeId == approvalHead.IntRequestEmployeeId
                                select user
                            ).FirstOrDefaultAsync();

                            lineOrSupManager.IntUserId = linemanager.IntUserId;
                            lineOrSupManager.StrUserName = linemanager.StrUserName;
                        }

                        var userApproval = new TblApprovalTransectionRow
                        {
                            IntApprovalTransectionId = lineOrSupManager.IntApprovalTransectionId,
                            IntUserId = app.ActionBy,
                            StrUserName = app.ActionByName,
                            NumThreshold = lineOrSupManager.NumThreshold,
                            IntSequenceId = lineOrSupManager.IntSequenceId + 1,
                            IsApprove = true,
                        };

                        _contextR.TblApprovalTransectionRows.Update(lineOrSupManager);
                        await _contextR.TblApprovalTransectionRows.AddAsync(userApproval);
                        await _contextR.SaveChangesAsync();

                        approval.UserRowId = userApproval.IntRowId;
                        approval.UserHeadId = userApproval.IntApprovalTransectionId;
                        approvalHead.IsApproved = true;
                    }
                    else if (approvalHead.ApprovalOrderTypeId == (int)ApprovalOrderType.Any_Person)
                    {
                        // here ActionBy is user whom approving
                        var userApproval = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId)
                        .OrderByDescending(x => x.IntRowId)
                        .Select(x => new TblApprovalTransectionRow
                        {
                            IntApprovalTransectionId = x.IntApprovalTransectionId,
                            IntUserId = app.ActionBy,
                            StrUserName = app.ActionByName,
                            NumThreshold = x.NumThreshold,
                            IntSequenceId = x.IntSequenceId + 1,
                            IsApprove = true,
                        }).FirstOrDefaultAsync();

                        await _contextR.TblApprovalTransectionRows.AddAsync(userApproval);
                        await _contextR.SaveChangesAsync();

                        approval.UserRowId = userApproval.IntRowId;
                        approval.UserHeadId = userApproval.IntApprovalTransectionId;

                        approvalHead.IsApproved = true;
                    }
                    else if (approvalHead.ApprovalOrderTypeId == (int)ApprovalOrderType.Any_Order)
                    {
                        // here ActionBy is user whom approving
                        var userApproval = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId)
                        .OrderByDescending(x => x.IntRowId)
                        .Select(x => new TblApprovalTransectionRow
                        {
                            IntApprovalTransectionId = x.IntApprovalTransectionId,
                            IntUserId = app.ActionBy,
                            StrUserName = app.ActionByName,
                            NumThreshold = x.NumThreshold,
                            IntSequenceId = x.IntSequenceId + 1,
                            IsApprove = true,
                        }).FirstOrDefaultAsync();

                        await _contextR.TblApprovalTransectionRows.AddAsync(userApproval);
                        await _contextR.SaveChangesAsync();

                        approval.UserRowId = userApproval.IntRowId;
                        approval.UserHeadId = userApproval.IntApprovalTransectionId;

                        approvalHead.IsApproved = true;
                    }
                    else if (approvalHead.ApprovalOrderTypeId == (int)ApprovalOrderType.In_Sequence)
                    {
                        // here ActionBy is user whom approving

                        var userApproval = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId)
                        .OrderByDescending(x => x.IntRowId)
                        .Select(x => new TblApprovalTransectionRow
                        {
                            IntApprovalTransectionId = x.IntApprovalTransectionId,
                            IntUserId = app.ActionBy,
                            StrUserName = app.ActionByName,
                            NumThreshold = x.NumThreshold,
                            IntSequenceId = x.IntSequenceId + 1,
                            IsApprove = true,
                        }).FirstOrDefaultAsync();

                        await _contextR.TblApprovalTransectionRows.AddAsync(userApproval);
                        await _contextR.SaveChangesAsync();

                        approval.UserRowId = userApproval.IntRowId;
                        approval.UserHeadId = userApproval.IntApprovalTransectionId;


                        approvalHead.IsApproved = true;
                    }
                    else
                        return approval;

                    // Set Next Stage
                    var firstStage = await _contextR.TblApprovalConfigHeaders.Where(x => x.IntPipelineId == pipeline.IntPipelineId && x.ApprovalConfigId > stage && x.IsActive == true).OrderBy(x => x.ApprovalConfigId).FirstOrDefaultAsync();

                    // if all approval stage complete
                    if (firstStage == null)
                    {
                        approval.UserList.Add(new NotifyUserDtos { UserId = -1 });

                        _contextR.TblApprovalTransectionHeaders.Update(approvalHead);
                        await _contextR.SaveChangesAsync();

                        return approval;
                    }

                    stage = firstStage.ApprovalConfigId;
                    OrderType = ApprovalOrder.GetApprovelOrderId(firstStage);
                    isSupervisor = firstStage.IsSupervisor;
                    isLinemanager = firstStage.IsLinemanger;
                }

                // Insert New Stage For Approval
                var aph = new TblApprovalTransectionHeader
                {
                    IntPipeLineId = pipeline.IntPipelineId,
                    IsApproved = false,
                    IntAccountId = pipeline.IntAccountId.Value,
                    IntUnitId = pipeline.IntUnitId.Value,
                    IntRequestEmployeeId = app.IntRequestEmployeeId,
                    DteLastActionDateTime = DateTime.Now,
                    DteServerDateTime = DateTime.Now,
                    IsActive = true,
                    ApprovalOrderTypeId = OrderType,
                    ApprovalOrderTypeName = ApprovalOrder.GetApprovelOrderName(OrderType),
                    ModuleId = pipeline.IntModuleId,
                    ModuleName = pipeline.StrModuleName,
                    FeatureId = pipeline.IntActivityFeatureId,
                    FeatureName = string.IsNullOrWhiteSpace(app.FeatureName) ? pipeline.StrActivityFeatureName : app.FeatureName,
                    IntReferenceId = app.ReferenceId,
                    StrReferenceCode = app.ReferenceCode,
                    IntStageId = stage,
                    IsSupervisor = isSupervisor,
                    IsLinemanger = isLinemanager
                };

                var arow = new List<TblApprovalTransectionRow>();

                // isSupervisor isLinemanager
                if (isSupervisor || isLinemanager)
                {
                    arow = await (from a in _contextR.TblApprovalConfigRows
                                  where a.ApprovalConfigId == stage && a.IsActive == true
                                  select new TblApprovalTransectionRow()
                                  {
                                      IntApprovalTransectionId = aph.IntApprovalTransectionId,
                                      IntUserId = a.IntUserId,
                                      StrUserName = a.StrUserName,
                                      //   StrUserName = isSupervisor ? "Supervisor" : "Line-Manager",
                                      IsApprove = false
                                  }).ToListAsync();
                }
                // For In_Sequence, Post 1st User To Approval
                else if (OrderType == (int)ApprovalOrderType.In_Sequence)
                {
                    var user = await (from a in _contextR.TblApprovalConfigRows
                                      where a.ApprovalConfigId == stage && a.IsActive == true
                                      orderby a.RowId ascending
                                      select new TblApprovalTransectionRow()
                                      {
                                          IntApprovalTransectionId = aph.IntApprovalTransectionId,
                                          IntUserId = (long)a.IntUserId,
                                          StrUserName = a.StrUserName,
                                          IsApprove = false
                                      }).FirstOrDefaultAsync();

                    arow.Add(user);
                }
                else
                {
                    arow = await (from a in _contextR.TblApprovalConfigRows
                                  where a.ApprovalConfigId == stage && a.IsActive == true
                                  select new TblApprovalTransectionRow()
                                  {
                                      IntApprovalTransectionId = aph.IntApprovalTransectionId,
                                      IntUserId = (long)a.IntUserId,
                                      StrUserName = a.StrUserName,
                                      IsApprove = false
                                  }).ToListAsync();
                }

                // if stage missing any user list
                if (arow.Count <= 0)
                {
                    // to avoid stage user missing risk
                    approval.PipelineName = null;
                    approval.UserList.Add(new NotifyUserDtos { UserId = -1 });
                    return approval;
                }

                var Sequence = 1;

                await _contextR.TblApprovalTransectionHeaders.AddAsync(aph);
                await _contextR.SaveChangesAsync();

                arow.ForEach(x => { x.IntSequenceId = Sequence++; x.IntApprovalTransectionId = aph.IntApprovalTransectionId; });

                await _contextR.TblApprovalTransectionRows.AddRangeAsync(arow);
                await _contextR.SaveChangesAsync();

                if (isSupervisor)
                {
                    var supervisor = await (
                        from emp in _contextR.TblEmployeeBasicInfos
                        join user in _contextR.TblUsers on emp.IntSupervisorId equals user.IntUserReferenceId
                        where emp.IntEmployeeId == app.IntRequestEmployeeId
                        select user.IntUserId
                    ).ToListAsync();

                    approval.UserList.AddRange(supervisor.Select(x => new NotifyUserDtos { UserId = x, Type = PageType.Approve }).ToList());
                    return approval;
                }
                else if (isLinemanager)
                {
                    var linemanager = await (
                        from emp in _contextR.TblEmployeeBasicInfos
                        join user in _contextR.TblUsers on emp.IntLineManagerId equals user.IntUserReferenceId
                        where emp.IntEmployeeId == app.IntRequestEmployeeId
                        select user.IntUserId
                    ).ToListAsync();

                    approval.UserList.AddRange(linemanager.Select(x => new NotifyUserDtos { UserId = x, Type = PageType.Approve }).ToList());
                    return approval;
                }
                else
                {
                    approval.UserList.AddRange(arow.Select(x => new NotifyUserDtos { UserId = x.IntUserId.Value, Type = PageType.Approve }).ToList());
                    return approval;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<(long, string)> RoleBackApprovalPipeline(long ApprovalId, long? UserId)
        {
            var approvalHead = await _contextR.TblApprovalTransectionHeaders.Where(x => x.IntApprovalTransectionId == ApprovalId).FirstOrDefaultAsync();

            if (approvalHead == null)
                return default;

            var referenceId = approvalHead.IntReferenceId.Value;
            var referenceCode = approvalHead.StrReferenceCode;

            // Restore Previous Approval
            var approvalRow = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == ApprovalId && (!UserId.HasValue || x.IntUserId == UserId)).ToListAsync();

            approvalHead.IsActive = true;
            approvalHead.IsApproved = false;

            approvalRow.ForEach(x => x.IsApprove = false);

            _contextR.TblApprovalTransectionHeaders.Update(approvalHead);
            _contextR.TblApprovalTransectionRows.UpdateRange(approvalRow);

            // Remove If Any New Approval Event Trigger
            var approvalHeadRemove = await _contextR.TblApprovalTransectionHeaders.Where(x => x.IntApprovalTransectionId > ApprovalId && x.IntReferenceId == referenceId && x.StrReferenceCode == referenceCode && x.IsActive == true).FirstOrDefaultAsync();

            if (approvalHeadRemove != null)
            {
                approvalHeadRemove.IsActive = false;
                approvalHeadRemove.IsApproved = false;

                _contextR.TblApprovalTransectionHeaders.Update(approvalHeadRemove);
            }

            await _contextR.SaveChangesAsync();

            return (referenceId, referenceCode);
        }

        public async Task<ApprovalPostDto> RejectApproval(ApprovalDto app)
        {
            try
            {
                approval = new ApprovalPostDto();

                var pipeline = await _contextR.TblApprovalPipelines.Where(a => a.IntUnitId == app.UnitId && a.IntActivityFeatureId == app.FeatureId && a.IsActive == true).FirstOrDefaultAsync();

                if (pipeline == null)
                    return approval;

                pipeline.IntPipelineId = pipeline.IntPipelineId;
                long stage, OrderType;
                bool isSupervisor = false, isLinemanager = false;

                var approvalHeadQuery = _contextR.TblApprovalTransectionHeaders.Where(x => x.IntUnitId == app.UnitId && x.FeatureId == pipeline.IntActivityFeatureId && x.IntPipeLineId == pipeline.IntPipelineId && x.IntReferenceId == app.ReferenceId && x.StrReferenceCode == app.ReferenceCode && x.IsActive == true && x.IsApproved == false);

                var approvalHead = await approvalHeadQuery.OrderBy(x => x.IntApprovalTransectionId).FirstOrDefaultAsync();

                // init approval setup
                if (approvalHead == null)
                {
                    var firstStage = await _contextR.TblApprovalConfigHeaders.Where(x => x.IntPipelineId == pipeline.IntPipelineId && x.IsActive == true).OrderBy(x => x.ApprovalConfigId).FirstOrDefaultAsync();

                    if (firstStage == null)
                        return approval;

                    approval.PipelineName = pipeline.StrPipelineName;
                    stage = firstStage.ApprovalConfigId;
                    OrderType = ApprovalOrder.GetApprovelOrderId(firstStage);
                    isSupervisor = firstStage.IsSupervisor;
                    isLinemanager = firstStage.IsLinemanger;
                }
                // update existing approval setup
                else
                {
                    stage = approvalHead.IntStageId.Value;

                    approval.ApprovalId = approvalHead.IntApprovalTransectionId;
                    approval.StageId = stage;
                    approval.PipelineId = pipeline.IntPipelineId;
                    approval.PipelineName = pipeline.StrPipelineName;
                    approval.ApproveUserId = app.ActionBy;
                    approval.ApproveUserName = app.ActionByName;

                    if (approvalHead.IsSupervisor == true || approvalHead.IsLinemanger == true)
                    {
                        // here ActionBy is user whom approving
                        var userApproval = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId).FirstOrDefaultAsync();

                        userApproval.IsReject = true;
                        userApproval.IntUserId = app.ActionBy;
                        userApproval.StrUserName = app.ActionByName;

                        _contextR.TblApprovalTransectionRows.UpdateRange(userApproval);
                        await _contextR.SaveChangesAsync();

                        approval.UserRowId = userApproval.IntRowId;
                        approval.UserHeadId = userApproval.IntApprovalTransectionId;
                        approvalHead.IsApproved = true;
                    }
                    else if (approvalHead.ApprovalOrderTypeId == (int)ApprovalOrderType.Any_Person)
                    {
                        // here ActionBy is user whom approving
                        var userApproval = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId && x.IntUserId == app.ActionBy && x.IsApprove == false).FirstOrDefaultAsync();
                        if (userApproval == null)
                        {
                            if (pipeline.StrUserType == "User Group")
                            {
                                var userList = await (from a in _contextR.TblUserGroupHeaders
                                                      join b in _contextR.TblUserGroupRows on a.IntUserGroupId equals b.IntUserGroupId
                                                      where b.IsActive == true
                                                      && a.IntUserGroupId == pipeline.IntSuperUserId
                                                      select b.IntUserId).ToListAsync();

                                if (userList.Contains(app.ActionBy))
                                {

                                    var countSequence = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId).Select(x => x.IntSequenceId).CountAsync();

                                    var entity = new TblApprovalTransectionRow()
                                    {

                                        IntApprovalTransectionId = approvalHead.IntApprovalTransectionId,
                                        IntUserId = app.ActionBy,
                                        StrUserName = app.ActionByName,
                                        NumThreshold = null,
                                        IntSequenceId = countSequence + 1,
                                        IsApprove = false,
                                        IsReject = true,
                                    };
                                    _contextR.TblApprovalTransectionRows.Add(entity);
                                    await _contextR.SaveChangesAsync();

                                    approval.UserRowId = entity.IntRowId;
                                    approval.UserHeadId = entity.IntApprovalTransectionId;

                                }

                            }
                            else
                            {

                                // For supper Approval
                                if (pipeline.IntSuperUserId == app.ActionBy)
                                {
                                    var countSequence = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId).Select(x => x.IntSequenceId).CountAsync();

                                    var entity = new TblApprovalTransectionRow()
                                    {

                                        IntApprovalTransectionId = approvalHead.IntApprovalTransectionId,
                                        IntUserId = app.ActionBy,
                                        StrUserName = app.ActionByName,
                                        NumThreshold = null,
                                        IntSequenceId = countSequence + 1,
                                        IsApprove = false,
                                        IsReject = true,
                                    };
                                    _contextR.TblApprovalTransectionRows.Add(entity);
                                    await _contextR.SaveChangesAsync();

                                    approval.UserRowId = entity.IntRowId;
                                    approval.UserHeadId = entity.IntApprovalTransectionId;
                                }
                            }
                        }
                        else
                        {
                            userApproval.IsReject = true;
                            _contextR.TblApprovalTransectionRows.Update(userApproval);
                            await _contextR.SaveChangesAsync();

                            approval.UserRowId = userApproval.IntRowId;
                            approval.UserHeadId = userApproval.IntApprovalTransectionId;
                        }

                        var userLimit = await _contextR.TblApprovalConfigHeaders.Where(x => x.ApprovalConfigId == stage).Select(x => x.IntAnyUsers).FirstOrDefaultAsync();
                        var userApprovalCount = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId && x.IsApprove == true).CountAsync();

                        // Approval Pending for others
                        //if (userLimit > userApprovalCount)
                        //{
                        //    approval.UserList.AddRange(
                        //        await _contextR.TblApprovalTransectionRows
                        //        .Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId && x.IsApprove == false)
                        //        .Select(x => new NotifyUserDtos { UserId = x.IntUserId.Value, Type = PageType.Reject }).ToListAsync()
                        //        );

                        //    return approval;
                        //}

                        approvalHead.IsApproved = true;
                        _contextR.TblApprovalTransectionHeaders.Update(approvalHead);
                        await _contextR.SaveChangesAsync();
                    }
                    else if (approvalHead.ApprovalOrderTypeId == (int)ApprovalOrderType.Any_Order)
                    {
                        // here ActionBy is user whom approving
                        var userApproval = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId && x.IntUserId == app.ActionBy && x.IsApprove == false).FirstOrDefaultAsync();
                        if (userApproval == null)
                        {
                            if (pipeline.StrUserType == "User Group")
                            {
                                var userList = await (from a in _contextR.TblUserGroupHeaders
                                                      join b in _contextR.TblUserGroupRows on a.IntUserGroupId equals b.IntUserGroupId
                                                      where b.IsActive == true
                                                      select b.IntUserId).ToListAsync();

                                if (userList.Contains(app.ActionBy))
                                {
                                    if (pipeline.IntSuperUserId == app.ActionBy)
                                    {
                                        var countSequence = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId).Select(x => x.IntSequenceId).CountAsync();

                                        var entity = new TblApprovalTransectionRow()
                                        {

                                            IntApprovalTransectionId = approvalHead.IntApprovalTransectionId,
                                            IntUserId = app.ActionBy,
                                            StrUserName = app.ActionByName,
                                            NumThreshold = null,
                                            IntSequenceId = countSequence + 1,
                                            IsApprove = false,
                                            IsReject = true,
                                        };
                                        _contextR.TblApprovalTransectionRows.Add(entity);
                                        await _contextR.SaveChangesAsync();

                                        approval.UserRowId = entity.IntRowId;
                                        approval.UserHeadId = entity.IntApprovalTransectionId;
                                    }
                                }

                            }
                            else
                            {

                                // For supper Approval
                                if (pipeline.IntSuperUserId == app.ActionBy)
                                {
                                    var countSequence = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId).Select(x => x.IntSequenceId).CountAsync();

                                    var entity = new TblApprovalTransectionRow()
                                    {

                                        IntApprovalTransectionId = approvalHead.IntApprovalTransectionId,
                                        IntUserId = app.ActionBy,
                                        StrUserName = app.ActionByName,
                                        NumThreshold = null,
                                        IntSequenceId = countSequence + 1,
                                        IsApprove = false,
                                        IsReject = true,
                                    };
                                    _contextR.TblApprovalTransectionRows.Add(entity);
                                    await _contextR.SaveChangesAsync();

                                    approval.UserRowId = entity.IntRowId;
                                    approval.UserHeadId = entity.IntApprovalTransectionId;
                                }
                            }
                        }
                        else
                        {
                            userApproval.IsReject = true;
                            //userApproval.IsApprove = true;
                            _contextR.TblApprovalTransectionRows.Update(userApproval);
                            await _contextR.SaveChangesAsync();

                            approval.UserRowId = userApproval.IntRowId;
                            approval.UserHeadId = userApproval.IntApprovalTransectionId;
                        }

                        var userApprovalList = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId && x.IsApprove == false)
                        .Select(x => new NotifyUserDtos { UserId = x.IntUserId.Value, Type = PageType.Reject }).ToListAsync();



                        approvalHead.IsApproved = true;
                        _contextR.TblApprovalTransectionHeaders.Update(approvalHead);
                        await _contextR.SaveChangesAsync();
                    }
                    else if (approvalHead.ApprovalOrderTypeId == (int)ApprovalOrderType.In_Sequence)
                    {
                        // here ActionBy is user whom Rejecting
                        var userApproval = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId && x.IntUserId == app.ActionBy && x.IsApprove == false).FirstOrDefaultAsync();

                        if (userApproval == null)
                        {
                            if (pipeline.StrUserType == "User Group")
                            {
                                var userList = await (from a in _contextR.TblUserGroupHeaders
                                                      join b in _contextR.TblUserGroupRows on a.IntUserGroupId equals b.IntUserGroupId
                                                      where b.IsActive == true
                                                      select b.IntUserId).ToListAsync();

                                if (userList.Contains(app.ActionBy))
                                {
                                    if (pipeline.IntSuperUserId == app.ActionBy)
                                    {
                                        var countSequence = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId).Select(x => x.IntSequenceId).CountAsync();

                                        var entity = new TblApprovalTransectionRow()
                                        {

                                            IntApprovalTransectionId = approvalHead.IntApprovalTransectionId,
                                            IntUserId = app.ActionBy,
                                            StrUserName = app.ActionByName,
                                            NumThreshold = null,
                                            IntSequenceId = countSequence + 1,
                                            IsApprove = false,
                                            IsReject = true,
                                        };
                                        _contextR.TblApprovalTransectionRows.Add(entity);
                                        await _contextR.SaveChangesAsync();

                                        approval.UserRowId = entity.IntRowId;
                                        approval.UserHeadId = entity.IntApprovalTransectionId;
                                    }
                                }

                            }
                            else
                            {

                                // For supper Approval
                                if (pipeline.IntSuperUserId == app.ActionBy)
                                {
                                    var countSequence = await _contextR.TblApprovalTransectionRows.Where(x => x.IntApprovalTransectionId == approvalHead.IntApprovalTransectionId).Select(x => x.IntSequenceId).CountAsync();

                                    var entity = new TblApprovalTransectionRow()
                                    {

                                        IntApprovalTransectionId = approvalHead.IntApprovalTransectionId,
                                        IntUserId = app.ActionBy,
                                        StrUserName = app.ActionByName,
                                        NumThreshold = null,
                                        IntSequenceId = countSequence + 1,
                                        IsApprove = false,
                                        IsReject = true,
                                    };
                                    _contextR.TblApprovalTransectionRows.Add(entity);
                                    await _contextR.SaveChangesAsync();

                                    approval.UserRowId = entity.IntRowId;
                                    approval.UserHeadId = entity.IntApprovalTransectionId;
                                }
                            }
                        }
                        else
                        {
                            userApproval.IsReject = true;
                            //userApproval.IsApprove = true;
                            _contextR.TblApprovalTransectionRows.Update(userApproval);
                            await _contextR.SaveChangesAsync();

                            approval.UserRowId = userApproval.IntRowId;
                            approval.UserHeadId = userApproval.IntApprovalTransectionId;
                        }

                        var userLists = await _contextR.TblApprovalConfigRows.Where(x => x.ApprovalConfigId == stage && x.IsActive == true && x.IntSequenceId > userApproval.IntSequenceId).ToListAsync();

                        if (userLists.Count > 0)
                        {
                            var user = (from a in userLists
                                        where a.ApprovalConfigId == stage && a.IsActive == true && a.IntSequenceId > userApproval.IntSequenceId
                                        orderby a.IntSequenceId ascending
                                        select new TblApprovalTransectionRow()
                                        {
                                            IntApprovalTransectionId = userApproval.IntApprovalTransectionId,
                                            IntUserId = (long)a.IntUserId,
                                            StrUserName = a.StrUserName,
                                            IsApprove = false,
                                            IntSequenceId = a.IntSequenceId
                                        }).FirstOrDefault();

                            await _contextR.TblApprovalTransectionRows.AddAsync(user);
                            await _contextR.SaveChangesAsync();

                            approval.UserList.Add(new NotifyUserDtos { UserId = user.IntUserId.Value, Type = PageType.Approve });

                            return approval;
                        }

                        approvalHead.IsApproved = true;
                        _contextR.TblApprovalTransectionHeaders.Update(approvalHead);
                        await _contextR.SaveChangesAsync();
                    }
                    else
                        return approval;

                    // Set Next Stage
                    var firstStage = await _contextR.TblApprovalConfigHeaders.Where(x => x.IntPipelineId == pipeline.IntPipelineId && x.ApprovalConfigId > stage && x.IsActive == true).OrderBy(x => x.ApprovalConfigId).FirstOrDefaultAsync();

                    // if all approval stage complete
                    if (firstStage == null)
                    {
                        approval.UserList.Add(new NotifyUserDtos { UserId = -1 });
                        return approval;
                    }

                    stage = firstStage.ApprovalConfigId;
                    OrderType = ApprovalOrder.GetApprovelOrderId(firstStage);
                    isSupervisor = firstStage.IsSupervisor;
                    isLinemanager = firstStage.IsLinemanger;
                }

                // Insert New Stage For Approval
                var aph = new TblApprovalTransectionHeader
                {
                    IntPipeLineId = pipeline.IntPipelineId,
                    IsApproved = false,
                    IntAccountId = pipeline.IntAccountId.Value,
                    IntUnitId = pipeline.IntUnitId.Value,
                    IntRequestEmployeeId = app.IntRequestEmployeeId,
                    DteLastActionDateTime = DateTime.Now,
                    DteServerDateTime = DateTime.Now,
                    IsActive = true,
                    ApprovalOrderTypeId = OrderType,
                    ApprovalOrderTypeName = ApprovalOrder.GetApprovelOrderName(OrderType),
                    ModuleId = pipeline.IntModuleId,
                    ModuleName = pipeline.StrModuleName,
                    FeatureId = pipeline.IntActivityFeatureId,
                    FeatureName = string.IsNullOrWhiteSpace(app.FeatureName) ? pipeline.StrActivityFeatureName : app.FeatureName,
                    IntReferenceId = app.ReferenceId,
                    StrReferenceCode = app.ReferenceCode,
                    IntStageId = stage,
                    IsSupervisor = isSupervisor,
                    IsLinemanger = isLinemanager
                };

                var arow = new List<TblApprovalTransectionRow>();

                // isSupervisor isLinemanager
                if (isSupervisor || isLinemanager)
                {
                    arow = await (from a in _contextR.TblApprovalConfigRows
                                  where a.ApprovalConfigId == stage && a.IsActive == true
                                  select new TblApprovalTransectionRow()
                                  {
                                      IntApprovalTransectionId = aph.IntApprovalTransectionId,
                                      IntUserId = a.IntUserId,
                                      StrUserName = a.StrUserName,
                                      //   StrUserName = isSupervisor ? "Supervisor" : "Line-Manager",
                                      IsApprove = false
                                  }).ToListAsync();
                }
                // For In_Sequence, Post 1st User To Approval
                else if (OrderType == (int)ApprovalOrderType.In_Sequence)
                {
                    var user = await (from a in _contextR.TblApprovalConfigRows
                                      where a.ApprovalConfigId == stage && a.IsActive == true
                                      orderby a.RowId ascending
                                      select new TblApprovalTransectionRow()
                                      {
                                          IntApprovalTransectionId = aph.IntApprovalTransectionId,
                                          IntUserId = (long)a.IntUserId,
                                          StrUserName = a.StrUserName,
                                          IsApprove = false
                                      }).FirstOrDefaultAsync();

                    arow.Add(user);
                }
                else
                {
                    arow = await (from a in _contextR.TblApprovalConfigRows
                                  where a.ApprovalConfigId == stage && a.IsActive == true
                                  select new TblApprovalTransectionRow()
                                  {
                                      IntApprovalTransectionId = aph.IntApprovalTransectionId,
                                      IntUserId = (long)a.IntUserId,
                                      StrUserName = a.StrUserName,
                                      IsApprove = false
                                  }).ToListAsync();
                }

                if (isSupervisor)
                {
                    var supervisor = await (
                        from emp in _contextR.TblEmployeeBasicInfos
                        join user in _contextR.TblUsers on emp.IntSupervisorId equals user.IntUserReferenceId
                        where emp.IntEmployeeId == app.IntRequestEmployeeId
                        select user.IntUserId
                    ).ToListAsync();

                    approval.UserList.AddRange(supervisor.Select(x => new NotifyUserDtos { UserId = x, Type = PageType.Reject }).ToList());
                    return approval;
                }
                else if (isLinemanager)
                {
                    var linemanager = await (
                        from emp in _contextR.TblEmployeeBasicInfos
                        join user in _contextR.TblUsers on emp.IntLineManagerId equals user.IntUserReferenceId
                        where emp.IntEmployeeId == app.IntRequestEmployeeId
                        select user.IntUserId
                    ).ToListAsync();

                    approval.UserList.AddRange(linemanager.Select(x => new NotifyUserDtos { UserId = x, Type = PageType.Reject }).ToList());
                    return approval;
                }
                else
                {
                    approval.UserList.AddRange(arow.Select(x => new NotifyUserDtos { UserId = x.IntUserId.Value, Type = PageType.Reject }).ToList());
                    return approval;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}