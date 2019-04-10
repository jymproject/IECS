using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IECSC.TRANS.BizListen.Implement
{
    public class RequestTaskCmdAndDealRequest : IBiz
    {
        private CommonBiz commonBiz = null;
        public RequestTaskCmdAndDealRequest()
        {
            commonBiz = new CommonBiz();
        }

        public void HandleLoc(string locNo)
        {
            try
            {
                var loc = BizHandle.Instance.locDic[locNo];
                if (loc.plcStatus.StatusFree != 1)
                {
                    return;
                }
                //获取指令
                if (loc.bizStatus == BizStatus.None)
                {
                    var result = commonBiz.SelectTaskCmd(loc);
                    if (result)
                    {
                        loc.bizStatus = BizStatus.WriteDeal;
                    }
                    else
                    {
                        loc.bizStatus = BizStatus.ReqTaskAndCmd;
                    }
                }
                //获取任务
                var taskNo = 0;
                if (loc.bizStatus == BizStatus.ReqTaskAndCmd)
                {
                    taskNo = commonBiz.RequstTaskAndCmd(loc);
                    if (taskNo > 0)
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
                        loc.bizStatus = BizStatus.WriteDeal;
                    }
                    else
                    {
                        return;
                    }
                }

                //写入已处理信号
                if (loc.bizStatus == BizStatus.WriteDeal)
                {
                    var result = commonBiz.WriteTaskDeal(loc);
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
                    loc.RequestTaskAndCmdObjid = 0;
                    loc.plcStatus.StatusFree = 0;
                    loc.bizStatus = BizStatus.None;
                }
            }
            catch { throw; }
        }
    }
}
