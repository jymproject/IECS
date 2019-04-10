using MSTL.LogAgent;
using MSTL.OpcClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IECSC.Raise.Action
{
    class OpcAction
    {
        public OpcClient opcClient;

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

        #region 单例模式
        private static OpcAction _instance = null;
        public static OpcAction Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (typeof(OpcAction))
                    {
                        if (_instance == null)
                        {
                            _instance = new OpcAction();
                        }
                    }
                }
                return _instance;
            }
        }
        private OpcAction()
        {
            opcClient = new OpcClient();
        }
        #endregion

        /// <summary>
        /// 连接OPC
        /// </summary>
        public bool ConnectOpc(ref string errMsg)
        {
            try
            {
                var serverIp = McConfig.Instance.OpcServerIp;
                var serverName = McConfig.Instance.OpcServerName;
                var result = opcClient.ConnectOpcServer(serverIp, serverName, ref errMsg);
                if (result)
                {
                    opcClient.DataChanged += OpcClient_DataChanged;
                }
                return result;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 添加组
        /// </summary>
        public bool AddOpcGroup(ref string errMsg)
        {
            try
            {
                var groupName = McConfig.Instance.OpcGroupName;
                return opcClient.AddOpcGroup(groupName, ref errMsg);

            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 添加项
        /// </summary>
        public bool AddOpcItem(ref string errMsg)
        {
            try
            {
                var groupName = McConfig.Instance.OpcGroupName;
                var readItems = BizHandle.Instance.readItems.Keys.ToArray();
                if (!opcClient.AddOpcItems(groupName, readItems, ref errMsg))
                {
                    return false;
                }
                var writeItems = BizHandle.Instance.writeItems.Keys.ToArray();
                if (!opcClient.AddOpcItems(groupName, writeItems, ref errMsg))
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// OPCCLIENT数据改变事件处理方法
        /// </summary>
        /// <param name="e"></param>
        private void OpcClient_DataChanged(MSTL.OpcClient.Model.DataChangedEventArgs e)
        {
            try
            {
                foreach (var item in e.Data)
                {
                    //判断PLC连接
                    if (item.Quality.Equals(Opc.Da.Quality.Bad))
                    {
                        continue;
                    }
                    if (item.TagLongName == null)
                    {
                        continue;
                    }
                    //检查是否为站台的读取项
                    if (!BizHandle.Instance.readItems.Keys.Contains(item.TagLongName))
                    {
                        continue;
                    }
                    var items = BizHandle.Instance.readItems[item.TagLongName];
                    //获取站台号
                    var locNo = BizHandle.Instance.readItems[item.TagLongName].LocNo;
                    #region 绑定读取值
                    switch (items.BusIdentity)
                    {
                        case "Read.RaiseArea":
                            BizHandle.Instance.locDic[locNo].plcStatus.RaiseArea = (item.TagValue ?? 0).ToString().Trim();
                            break;
                        case "Read.DeviceNo":
                            BizHandle.Instance.locDic[locNo].plcStatus.DeviceNo = (item.TagValue ?? 0).ToString().Trim();
                            break;
                        case "Read.RealWeight":
                            BizHandle.Instance.locDic[locNo].plcStatus.RealWeight = (item.TagValue ?? 0).ToString().Trim();
                            break;
                        case "Read.ProductGuid":
                            BizHandle.Instance.locDic[locNo].plcStatus.ProductGuid = (item.TagValue ?? 0).ToString().Trim();
                            break;
                        case "Read.StandardWeight":
                            BizHandle.Instance.locDic[locNo].plcStatus.StandardWeight = (item.TagValue ?? 0).ToString().Trim();
                            break;
                        case "Read.AllowErrRange":
                            BizHandle.Instance.locDic[locNo].plcStatus.AllowErrRange = (item.TagValue ?? 0).ToString().Trim();
                            break;
                        case "Read.Status_Auto":
                            BizHandle.Instance.locDic[locNo].plcStatus.StatusAuto = Convert.ToInt32(item.TagValue ?? 0);
                            break;
                        case "Read.Status_Fault":
                            BizHandle.Instance.locDic[locNo].plcStatus.StatusFault = Convert.ToInt32(item.TagValue ?? 0);
                            break;
                        case "Read.Status_Load":
                            BizHandle.Instance.locDic[locNo].plcStatus.StatusLoad = Convert.ToInt32(item.TagValue ?? 0);
                            break;
                        case "Read.Status_RequestTask":
                            BizHandle.Instance.locDic[locNo].plcStatus.StatusRequestTask = Convert.ToInt32(item.TagValue ?? 0);
                            break;
                        case "Read.Status_FreeAndPut":
                            BizHandle.Instance.locDic[locNo].plcStatus.StatusFreeAndPut = Convert.ToInt32(item.TagValue ?? 0);
                            break;
                        case "Read.Stauts_BusyAndTake":
                            BizHandle.Instance.locDic[locNo].plcStatus.StatusBusyAndTake = Convert.ToInt32(item.TagValue ?? 0);
                            break;
                        case "Read.PalletType":
                            BizHandle.Instance.locDic[locNo].plcStatus.PalletType = (item.TagValue ?? 0).ToString().Trim();
                            break;
                        default:
                            break;
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                log.Error($"[异常]执行OpcClient_DataChanged(MSTL.OpcClient.Model.DataChangedEventArgs e)读取OPC信息失败:{ex.ToString()}");
            }
        }

        /// <summary>
        /// 写入指令
        /// </summary>
        public bool WriteTaskCmd(Loc loc, ref string errMsg)
        {
            try
            {
                var keyValues = new List<KeyValuePair<string, object>>();
                foreach (var item in BizHandle.Instance.writeItems)
                {
                    if (item.Value.LocNo != loc.LocNo)
                    {
                        continue;
                    }
                    switch (item.Value.BusIdentity)
                    {
                        case "Write.RaiseArea":
                            keyValues.Add(new KeyValuePair<string, object>(item.Key, loc.taskCmd.LocArea));
                            break;
                        case "Write.DeviceNo":
                            keyValues.Add(new KeyValuePair<string, object>(item.Key, loc.taskCmd.LocCode));
                            break;
                        case "Write.ProductGuid":
                            keyValues.Add(new KeyValuePair<string, object>(item.Key, loc.taskCmd.ProductGuid));
                            break;
                        case "Write.StandardWeight":
                            keyValues.Add(new KeyValuePair<string, object>(item.Key, loc.taskCmd.StandardWeight));
                            break;
                        case "Write.AllowErrRange":
                            keyValues.Add(new KeyValuePair<string, object>(item.Key, loc.taskCmd.AllowErrRange));
                            break;
                        case "Write.PalletType":
                            keyValues.Add(new KeyValuePair<string, object>(item.Key, loc.taskCmd.PalletType));
                            break;
                    }
                }
                if (keyValues.Count > 0)
                {
                    if (opcClient.WriteValues(McConfig.Instance.OpcGroupName, keyValues.ToArray(), ref errMsg))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    errMsg = "未找到OPC任务信息写入项";
                    return false;
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 写入顺序控制字
        /// </summary>
        public bool WriteTaskDeal(Loc loc, ref string errMsg)
        {
            try
            {
                foreach (var item in BizHandle.Instance.writeItems)
                {
                    if (item.Value.LocNo != loc.LocNo)
                    {
                        continue;
                    }
                    if (item.Value.BusIdentity.Equals("Write.TaskHandled"))
                    {
                        var kValue = new KeyValuePair<string, object>(item.Key, 1);
                        if (opcClient.WriteValue(McConfig.Instance.OpcGroupName, kValue, ref errMsg))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                errMsg = "未找到OPC“任务已处理标记”写入项";
                return false;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }
        }
    }
}
