using IECSC.Raise.Action;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IECSC.Raise
{
    public class CommonBiz
    {
       
        /// <summary>
        /// 下发指令信息
        /// </summary>
        public bool WriteTaskCmd(Loc loc)
        {
            var errMsg = string.Empty;
            //写入信息
            var result = OpcAction.Instance.WriteTaskCmd(loc, ref errMsg);
            if (result)
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData($"[{loc.LocPlcNo}]成功写入信息,请求号[{loc.GlobalObjid}]", loc.LocNo));
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
