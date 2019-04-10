using IECSC.Raise.Action;
using System;

namespace IECSC.Raise
{
    public class RequestAndDownInfo : IBiz
    {
        private CommonBiz commonBiz = null;

        public RequestAndDownInfo()
        {
            commonBiz = new CommonBiz();
        }

        public void HandleLoc (string locNo)
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
                if (loc.plcStatus.StatusRequestTask == 0)
                {
                    return;
                }
                if (Double.Parse(loc.plcStatus.RealWeight) <= 0)
                {
                    ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]读取称重信息异常", locNo, InfoType.locStatus));
                    return;
                }
                var errMsg = string.Empty;
                //请求生成产品信息
                if (loc.bizStatus == BizStatus.None)
                {
                    if(loc.GlobalObjid == 0)
                    {
                        loc.GlobalObjid = DbAction.Instance.SelectObjid();
                        var result = DbAction.Instance.InsertInfo(loc);
                        if(!result)
                        {
                            ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]保存参数表信息失败", locNo));
                            loc.GlobalObjid = 0;
                            return;
                        }
                    }
                    
                    loc.taskCmd = DbAction.Instance.ExcuteProcedure(loc, ref errMsg);
                    if(loc.taskCmd == null)
                    {
                        ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]调用存储过程处理失败：{errMsg}", locNo));
                        return;
                    }
                    BizHandle.Instance.downInfoDic[locNo] = loc.taskCmd;
                    loc.bizStatus = BizStatus.WriteTask;
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
                        loc.GlobalObjid = 0;
                        loc.taskCmd = new DownLoadInfo();
                        loc.bizStatus = BizStatus.None;
                    }
                }
               
            }
            catch (Exception ex)
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"站台{locNo}请求处理失败：{ex.Message}", locNo));
            }
        }
    }
}