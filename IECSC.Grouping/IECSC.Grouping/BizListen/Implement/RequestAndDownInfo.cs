using IECSC.Grouping.Action;
using System;

namespace IECSC.Grouping
{
    public class RequestAndDownInfo : IBiz
    {
        private CommonBiz commonBiz = null;

        public RequestAndDownInfo()
        {
            commonBiz = new CommonBiz();
        }

        public void HandleLoc(string locNo)
        {
            try
            {
                var loc = BizHandle.Instance.locDic[locNo];
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]站台刷新", locNo, InfoType.locStatus));
                //更新站台状态
                DbAction.Instance.RecordPlcInfo(loc);
                if (loc.plcStatus.StatusAuto <= 0)
                {
                    ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]非自动状态", locNo));
                    return;
                }
                if (loc.plcStatus.StatusFault > 0)
                {
                    ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]站台故障", locNo));
                    return;
                }
                if (loc.plcStatus.StatusRequestTask != 1)
                {
                    return;
                }
                if (string.IsNullOrEmpty(loc.plcStatus.ProductGuid))
                {
                    ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]读取产品编号信息异常", locNo));
                    return;
                }
                if (string.IsNullOrEmpty(loc.plcStatus.PalletNo))
                {
                    ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]读取工装编号信息异常", locNo));
                    return;
                }
                
                //根据ProductGuid判断调用哪个数据库
                var errMsg = string.Empty;
                var whNo = DbAction.Instance.GetWhNo(loc.plcStatus.ProductGuid, ref errMsg);
                if(string.IsNullOrEmpty(loc.WhNo))
                {
                    ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]获取归属仓库失败：{errMsg}", locNo));
                    return;
                }
                if(!loc.WhNo.Equals(whNo))
                {
                    loc.Db = DbAction.Instance.GetDb(loc.WhNo, ref errMsg);
                    if (loc.Db == null)
                    {
                        ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]连接[{loc.WhNo}]仓库数据库失败：{errMsg}", locNo));
                        return;
                    }
                    loc.WhNo = whNo;
                }

                //检查是否已存在请求指令
                if (loc.bizStatus == BizStatus.None)
                {
                    var result = commonBiz.SelectTaskCmd(loc);
                    if (result)
                    {
                        loc.bizStatus = BizStatus.WriteTask;
                    }
                    else
                    {
                        loc.bizStatus = BizStatus.ReqTask;
                    }
                }
                //请求任务
                var taskNo = 0;
                if (loc.bizStatus == BizStatus.ReqTask)
                {
                    taskNo = commonBiz.RequstTask(loc);
                    if (taskNo > 0)
                    {
                        loc.bizStatus = BizStatus.ReqCmd;
                    }
                    else
                    {
                        return;
                    }
                }
                //请求指令
                if (loc.bizStatus == BizStatus.ReqCmd)
                {
                    var result = commonBiz.RequstCmd(loc, taskNo);
                    if (result)
                    {
                        loc.bizStatus = BizStatus.Select;
                    }
                    else
                    {
                        return;
                    }
                }
                //查找指令
                if (loc.bizStatus == BizStatus.Select)
                {
                    var result = commonBiz.SelectTaskCmd(loc);
                    if (result)
                    {
                        loc.bizStatus = BizStatus.WriteTask;
                    }
                    else
                    {
                        return;
                    }
                }
                //写入信息
                if (loc.bizStatus == BizStatus.WriteTask)
                {
                    var result = commonBiz.WriteTaskCmd(loc);
                    if (result)
                    {
                        loc.bizStatus = BizStatus.WriteDeal;
                    }
                }
                //写入已处理信号
                if (loc.bizStatus == BizStatus.WriteDeal)
                {
                    var result = commonBiz.WriteTaskDeal(loc);
                    if (result)
                    {
                        loc.bizStatus = BizStatus.Update;
                    }
                }
                //更新指令步骤
                if (loc.bizStatus == BizStatus.Update)
                {
                    var result = commonBiz.UpdateTaskCmd(loc);
                    if (result)
                    {
                        loc.bizStatus = BizStatus.Reset;
                    }
                    else
                    {
                        return;
                    }
                }
                //复位
                if (loc.bizStatus == BizStatus.Reset)
                {
                    loc.Db = null;
                    loc.RequestTaskObjid = 0;
                    loc.RequestCmdObjid = 0;
                    loc.plcStatus.StatusRequestTask = 0;
                    loc.bizStatus = BizStatus.None;
                }
            }
            catch (Exception ex)
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"站台{locNo}请求处理失败：{ex.Message}", locNo));
            }
        }
    }
}