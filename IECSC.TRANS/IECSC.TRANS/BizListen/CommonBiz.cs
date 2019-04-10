﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IECSC.TRANS
{
    public class CommonBiz
    {
        /// <summary>
        /// 请求生成任务
        /// </summary>
        public int RequstTask(Loc loc)
        {

            var errMsg = string.Empty;
            if (loc.RequestTaskObjid <= 0)
            {
                loc.RequestTaskObjid = DbAction.Instance.GetObjidForRequestTask();
            }
            if (loc.RequestTaskObjid <= 0)
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]生成请求任务参数表OBJID失败", loc.LocNo));
                return 0;
            }

            //传入参数，请求任务
            var taskNo = DbAction.Instance.RequestTask(loc.RequestTaskObjid, loc.LocNo, "100064", loc.plcStatus.PalletNo, ref errMsg);
            if (taskNo > 0)
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]成功请求生成任务[{taskNo}]", loc.LocNo));
            }
            else
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]请求生成任务失败,原因{errMsg}", loc.LocNo));
                return 0;
            }
            return taskNo;

        }

        public int RequstTaskAndCmd(Loc loc)
        {

            var errMsg = string.Empty;
            if (loc.RequestTaskAndCmdObjid <= 0)
            {
                loc.RequestTaskAndCmdObjid = DbAction.Instance.GetObjidForRequestTaskAndCmd();
            }
            if (loc.RequestTaskAndCmdObjid <= 0)
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]生成请求任务参数表OBJID失败", loc.LocNo));
                return 0;
            }

            //传入参数，请求任务
            var taskNo = DbAction.Instance.RequestTaskAndCmd(loc.RequestTaskAndCmdObjid, loc.LocNo, "100892", ref errMsg);
            if (taskNo > 0)
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]成功请求生成任务与指令[{taskNo}]", loc.LocNo));
            }
            else
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]请求生成任务与指令失败,原因{errMsg}", loc.LocNo));
                return 0;
            }
            return taskNo;

        }


        /// <summary>
        /// 请求生成指令
        /// </summary>
        public bool RequstCmd(Loc loc, int taskNo)
        {
            var errMsg = string.Empty;
            //获取指令生成请求OBJID
            if (loc.RequestCmdObjid <= 0)
            {
                loc.RequestCmdObjid = DbAction.Instance.GetObjidForRequestCmd();
            }
            if (loc.RequestCmdObjid <= 0)
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]生成请求参数表OBJID失败", loc.LocNo));
                return false;
            }
            //传入参数，请求指令
            var CmdId = DbAction.Instance.RequestCmd(loc.RequestCmdObjid, loc.LocNo, taskNo, ref errMsg);
            if (CmdId > 0)
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]成功请求生成指令[{CmdId}]", loc.LocNo, InfoType.taskCmd));
            }
            else
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]请求生成指令失败,原因{errMsg}", loc.LocNo));
                return false;
            }
            return true;
        }
        /// <summary>
        /// 请求生成指令
        /// </summary>
        //public bool RequstCmdForStackloc(Loc loc, int taskNo)
        //{
        //    var errMsg = string.Empty;
        //    //获取指令生成请求OBJID
        //    if (loc.RequestCmdObjid <= 0)
        //    {
        //        loc.RequestCmdObjid = DbAction.Instance.GetObjidForRequestCmd();
        //    }
        //    if (loc.RequestCmdObjid <= 0)
        //    {
        //        ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]生成请求参数表OBJID失败", loc.LocNo));
        //        return false;
        //    }
        //    //传入参数，请求指令
        //    var CmdId = DbAction.Instance.RequestCmdForStackloc(loc.RequestCmdObjid, loc.LocNo, taskNo, ref errMsg);
        //    if (CmdId > 0)
        //    {
        //        ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]成功请求生成指令[{CmdId}]", loc.LocNo, InfoType.taskCmd));
        //    }
        //    else
        //    {
        //        ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]请求生成指令失败,原因{errMsg}", loc.LocNo));
        //        return false;
        //    }
        //    return true;
        //}
        /// <summary>
        /// 获取指令信息
        /// </summary>
        public bool SelectTaskCmd(Loc loc)
        {
            var errMsg = string.Empty;
            loc.taskCmd = DbAction.Instance.LoadTaskCmd(loc, "00", ref errMsg);
            if (loc.taskCmd != null)
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]站台接收到请求任务信号,获取到指令[{loc.taskCmd.ObjId}]", loc.LocNo, InfoType.taskCmd));
                return true;
            }
            else
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]站台接收到请求任务信号,获取指令失败,原因：{errMsg}", loc.LocNo));
                return false;
            }
        }



        /// <summary>
        /// 修改指令信息步骤
        /// </summary>
        public bool UpdateTaskCmd(Loc loc)
        {
            var errMsg = string.Empty;
            //更新指令步骤
            var result = DbAction.Instance.UpdateCmdStep(loc.taskCmd.ObjId, "02");
            if (result)
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]更新指令步骤为执行[02]成功", loc.LocNo, InfoType.taskCmd));
            }
            else
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]更新指令步骤为执行[02]失败,原因{errMsg}", loc.LocNo));
                return false;
            }
            return true;
        }

        /// <summary>
        /// 结束指令
        /// </summary>
        public bool FinishCmd(Loc loc, long TaskNo)
        {
            var errMsg = string.Empty;
            //获取指令结束请求OBJID
            if (loc.RequestFinishObjid <= 0)
            {
                loc.RequestFinishObjid = DbAction.Instance.GetObjidForCmdFinish();
            }
            if (loc.RequestFinishObjid <= 0)
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]生成指令结束参数表OBJID失败", loc.LocNo));
                return false;
            }
            //传入参数，结束指令
            var result = DbAction.Instance.RequestFinishTaskCmd(loc.RequestFinishObjid, TaskNo, loc.LocNo, 1, ref errMsg);
            if (result)
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]成功结束任务[{TaskNo}]", loc.LocNo, InfoType.taskCmd));
            }
            else
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]结束任务[{TaskNo}]失败,原因{errMsg}", loc.LocNo));
                return false;
            }
            return true;
        }

        /// <summary>
        /// 下发指令信息
        /// </summary>
        public bool WriteTaskCmd(Loc loc)
        {
            var errMsg = string.Empty;
            //写入指令信息
            var result = OpcAction.Instance.WriteTaskCmd(loc, ref errMsg);
            if (result)
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]成功写入指令[{loc.taskCmd.ObjId}]", loc.LocNo));
                return true;
            }
            else
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]写入指令失败,原因：{errMsg}", loc.LocNo));
                return false;
            }
        }

        /// <summary>
        /// 下发已处理信号
        /// </summary>
        public bool WriteTaskDeal(Loc loc)
        {
            var errMsg = string.Empty;
            //写入任务已处理信号
            var result = OpcAction.Instance.WriteTaskDeal(loc, ref errMsg);
            if (result)
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]成功写入任务已处理标记[1]", loc.LocNo));
                return true;
            }
            else
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]写入任务已处理标记失败,原因：{errMsg}", loc.LocNo));
                return false;
            }
        }
    }
}
