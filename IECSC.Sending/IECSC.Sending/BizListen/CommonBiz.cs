using IECSC.Sending.Action;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IECSC.Sending
{
    public class CommonBiz
    {
       
       

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
