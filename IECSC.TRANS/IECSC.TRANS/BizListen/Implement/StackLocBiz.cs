using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IECSC.TRANS.BizListen.Implement
{
    class StackLocBiz : IBiz
    {
        private CommonBiz commonBiz = null;

        public StackLocBiz()
        {
            commonBiz = new CommonBiz();
        }
        public void HandleLoc(string locNo)
        {
            try
            {
                var loc = BizHandle.Instance.locDic[locNo];
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
                //根据站台空闲可卸货标志位生成输送指令
                if (loc.plcStatus.StatusFree == 1)
                {
                    new RequestTaskCmdAndDealRequest().HandleLoc(locNo);
                    return;
                }
                if (loc.plcStatus.StatusToLoad==1)
                {
                    new FinishTask().HandleLoc(locNo);
                    return;
                }
                if (loc.plcStatus.StatusRequest==1)
                {
                    new RequestAndDownTask().HandleLoc(locNo);
                    return;
                }

               
            }
            catch
            { throw; }
        }
    }
}
