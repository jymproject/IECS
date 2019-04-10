using MSTL.LogAgent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IECSC.Stacking
{
    public class BizHandle
    {
        /// <summary>
        /// OPC读取项
        /// </summary>
        public Dictionary<string, LocOpcItem> readItems = null;
        /// <summary>
        /// OPC写入项
        /// </summary>
        public Dictionary<string, LocOpcItem> writeItems = null;
        /// <summary>
        /// 站台信息
        /// </summary>
        public Dictionary<string, Loc> locDic = null;
        /// <summary>
        /// 记录上位机最后一次写入下位机信息
        /// </summary>
        public Dictionary<string, DownLoadInfo> downInfoDic = null;
        /// <summary>
        /// 日志
        /// </summary>
        private ILog log
        {
            get
            {
                return Log.Store[this.GetType().FullName];
            }
        }
        private static BizHandle _instance = null;
        public static BizHandle Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (typeof(BizHandle))
                    {
                        if (_instance == null)
                        {
                            _instance = new BizHandle();
                        }
                    }
                }
                return _instance;
            }
        }
        private BizHandle()
        {
            readItems = new Dictionary<string, LocOpcItem>();
            writeItems = new Dictionary<string, LocOpcItem>();
            locDic = new Dictionary<string, Loc>();
            downInfoDic = new Dictionary<string, DownLoadInfo>();
        }

        /// <summary>
        /// 业务处理入口
        /// </summary>
        public void BizListen()
        {
            IBiz biz = null;
            foreach (var loc in locDic.Values)
            {
                ShowFormData.Instance.ShowFormInfo(new ShowInfoData("更新状态", loc.LocNo, InfoType.locStatus));
                switch (loc.TaskType)
                {
                    case "RequsetAndDownTask":
                        biz = new RequestAndDownInfo();
                        break;
                  
                }
                biz?.HandleLoc(loc.LocNo);
            }
        }
    }
}
