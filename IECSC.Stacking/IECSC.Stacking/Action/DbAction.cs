using Dapper;
using DapperExtensions;
using MSTL.DbClient;
using MSTL.LogAgent;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace IECSC.Stacking
{
    public class DbAction
    {
        /// <summary>
        /// 中间数据库操作类
        /// </summary>
        private IDatabase Db = null;

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
        private static DbAction _instance = null;
        public static DbAction Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (typeof(DbAction))
                    {
                        if (_instance == null)
                        {
                            _instance = new DbAction();
                        }
                    }
                }
                return _instance;
            }
        }
        public DbAction()
        {
            var errMsg = string.Empty;
            ConnDb(ref errMsg);
        }
        public bool ConnDb(ref string errMsg)
        {
            try
            {
                this.Db = DbHelper.GetDb(McConfig.Instance.DbConnect, DbHelper.DataBaseType.SqlServer, ref errMsg);

                return true;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                log.Error($"[异常]执行DbAction()建立数据库连接失败:{ex.ToString()}");
                return false;
            }
        }

        /// <summary>
        /// 修改指令步骤
        /// </summary>
        public bool UpdateCmdStep(Loc loc, string cmdStep)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append(" UPDATE WBS_TASK_CMD SET ");
                sb.Append(" CMD_STEP = @CMD_STEP,");
                sb.Append(" EXCUTE_DATE = getdate()");
                sb.Append(" WHERE OBJID = @OBJID");
                var param = new DynamicParameters();
                param.Add("OBJID", loc.taskCmd.Objid);
                param.Add("CMD_STEP", cmdStep);
                Db.Connection.Execute(sb.ToString(), param);
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"[异常]执行UpdateCmdStep({loc.taskCmd.Objid},{cmdStep})修改指令步骤失败:{ex.ToString()}");
                return false;
            }
        }

        internal int GetObjidForRequestCmd(Loc loc)
        {
            try
            {
                return Db.Connection.ExecuteScalar<int>("SELECT NEXT VALUE FOR DBO.SEQ_TPROC_0200_CMD_REQUEST"); ;
            }
            catch (Exception ex)
            {
                log.Error($"[异常]执行GetObjidForRequestCmd()获取请求生成任务参数表主键ID失败:{ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 获取数据库时间
        /// </summary>
        public bool GetDbTime()
        {
            try
            {
                var dt = Db.Connection.QueryTable("select GETDATE()");
                if (dt == null || dt.Rows.Count == 0)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                var Ip = McConfig.Instance.DbIp;
                if (Tools.Instance.PingNetAddress(Ip))
                {
                    var errMsg = string.Empty;
                    ConnDb(ref errMsg);
                }
                else
                {
                    log.Error($"[异常]执行GetDbTime()获取服务器时间失败:{ex.ToString()}");
                }
                return false;
            }
        }

        /// <summary>
        /// 请求生成指令
        /// </summary>
        public int RequestCmd(Loc loc, int taskNo, ref string errMsg)
        {
            try
            {
                //获取是否已经插入数据
                var dt = Db.Connection.QueryTable($"SELECT * FROM TPROC_0200_CMD_REQUEST T WHERE T.OBJID = {loc.RequestCmdObjid}");
                if (dt == null || dt.Rows.Count == 0)
                {
                    var sb = new StringBuilder();
                    sb.Append(" INSERT INTO TPROC_0200_CMD_REQUEST");
                    sb.Append(" (OBJID, TASK_NO, CURR_LOC_NO)");
                    sb.Append(" VALUES");
                    sb.Append(" (@OBJID, @TASKNO, @LOCNO)");
                    var param = new DynamicParameters();
                    param.Add("OBJID", loc.RequestCmdObjid);
                    param.Add("TASKNO", taskNo);
                    param.Add("LOCNO", loc.LocNo);
                    Db.Connection.Execute(sb.ToString(), param);
                }
                else
                {
                    string strSql = $"update TPROC_0200_CMD_REQUEST set TASKNO = {taskNo},LOCNO={loc.LocNo} where OBJID={loc.RequestCmdObjid}";
                    Db.Connection.Execute(strSql);

                }
                //执行存储过程
                var dp = new DynamicParameters();
                dp.Add("I_PARAM_OBJID", loc.RequestCmdObjid);
                dp.Add("O_CMD_OBJID", 0, DbType.Int32, ParameterDirection.Output);
                dp.Add("O_ERR_CODE", 0, DbType.Int32, ParameterDirection.Output);
                dp.Add("O_ERR_DESC", 0, DbType.String, ParameterDirection.Output, size: 80);
                Db.Connection.Execute("PROC_0200_CMD_REQUEST", param: dp, commandType: CommandType.StoredProcedure);
                errMsg = dp.Get<string>("O_ERR_DESC");
                return dp.Get<int>("O_CMD_OBJID");
            }
            catch (Exception ex)
            {
                log.Error($"[异常]执行RequestCmd({loc.RequestCmdObjid},{loc.LocNo},{taskNo},ref string errMsg)请求生成指令失败:{ex.ToString()}");
                errMsg = ex.Message;
                return -1;
            }
        }

        /// <summary>
        /// 获取指令信息
        /// </summary>
        public List<DownLoadInfo> GetTaskCmd(string locNo)
        {
            try
            {
                var taskList = new List<DownLoadInfo>();

                var sb = new StringBuilder();
                sb.Append("SELECT T.OBJID,T.TASK_NO,T.CMD_TYPE,T.CMD_STEP,T.SLOC_NO,T.SLOC_PLC_NO,T1.LOC_TYPE_NAME SLOC_TYPE,");
                sb.Append(" T.ELOC_NO,T.ELOC_PLC_NO,T2.LOC_TYPE_NAME ELOC_TYPE,T.PALLET_NO FROM WBS_TASK_CMD T");
                sb.Append(" LEFT JOIN PSB_LOC_TYPE T1 ON T1.LOC_TYPE_NO = T.SLOC_TYPE");
                sb.Append(" LEFT JOIN PSB_LOC_TYPE T2 ON T2.LOC_TYPE_NO = T.ELOC_TYPE");
                sb.Append(" WHERE T.TRANSFER_TYPE = '20'");
                sb.Append(" AND T.SLOC_NO = @SLOCNO");
                sb.Append(" ORDER BY T.OBJID DESC");
                var param = new DynamicParameters();
                param.Add("SLOCNO", locNo);
                var dt = Db.Connection.QueryTable(sb.ToString(), param);
                if (dt == null && dt.Rows.Count == 0)
                {
                    dt = Db.Connection.QueryTable(sb.ToString(), param);
                }
                foreach (DataRow row in dt.Rows)
                {
                    var taskCmd = new DownLoadInfo();
                    taskCmd.Objid = Convert.ToInt32(row["OBJID"].ToString());
                    taskCmd.SerialNo = Convert.ToInt32(row["TASK_NO"]);
                    taskCmd.SlocType = row["SLOC_TYPE"].ToString();
                    taskCmd.SlocNo = row["SLOC_NO"].ToString();
                    taskCmd.SlocPlcNo = row["SLOC_PLC_NO"].ToString();
                    taskCmd.ElocType = row["ELOC_TYPE"].ToString();
                    taskCmd.ElocNo = row["ELOC_NO"].ToString();
                    taskCmd.ElocPlcNo = row["ELOC_PLC_NO"].ToString();
                    taskCmd.PalletNo = row["PALLET_NO"].ToString();
                    taskCmd.CmdType = row["CMD_TYPE"].ToString();
                    taskCmd.CmdStep = row["CMD_STEP"].ToString();
                    taskCmd.WhNo = row["WH_NO"].ToString();
                    taskList.Add(taskCmd);
                }
                return taskList;

            }
            catch (Exception ex)
            {
                log.Error($"[异常]执行GetTaskCmd({locNo})获取指令信息失败:{ex.ToString()}");
                return null;
            }
        }

        /// <summary>
        /// 获取指令信息
        /// </summary>
        public DataTable GetTaskCmd(Loc loc, string cmdStep)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append("SELECT * FROM WBS_TASK_CMD T");
                sb.Append(" WHERE T.SLOC_NO = @SLOCNO");
                sb.Append(" AND T.CMD_STEP = @CMDSTEP");
                var param = new DynamicParameters();
                param.Add("SLOCNO", loc.LocNo);
                param.Add("CMDSTEP", cmdStep);
                return Db.Connection.QueryTable(sb.ToString(), param);
            }
            catch (Exception ex)
            {
                log.Error($"[异常]执行GetTaskCmd({loc}, {cmdStep})获取指令信息失败:{ex.ToString()}");
                return null;
            }
        }
      
        /// <summary>
        /// 获取站台尚未完成的所有指令信息
        /// </summary>
        public DownLoadInfo LoadTaskCmd(Loc locNo, string cmdStep, ref string errMsg)
        {
            try
            {
                var dt = GetTaskCmd(locNo, cmdStep);
                if (dt == null || dt.Rows.Count <= 0)
                {
                    return null;
                }
                var taskCmd = new DownLoadInfo();
                taskCmd.Objid = Convert.ToInt32(dt.Rows[0]["OBJID"].ToString());
                taskCmd.SerialNo = Convert.ToInt32(dt.Rows[0]["TASK_NO"]);
                taskCmd.SlocNo = dt.Rows[0]["SLOC_NO"].ToString();
                taskCmd.SlocPlcNo = dt.Rows[0]["SLOC_PLC_NO"].ToString();
                taskCmd.ElocNo = dt.Rows[0]["ELOC_NO"].ToString();
                taskCmd.ElocPlcNo = dt.Rows[0]["ELOC_PLC_NO"].ToString();
                taskCmd.PalletNo = dt.Rows[0]["PALLET_NO"].ToString();
                taskCmd.CmdType = dt.Rows[0]["CMD_TYPE"].ToString();
                taskCmd.CmdStep = dt.Rows[0]["CMD_STEP"].ToString();
                taskCmd.WhNo = dt.Rows[0]["WH_NO"].ToString();

                return taskCmd;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return null;
            }
        }

        public string GetWhNoFromDBS(string productGuid)
        {
            try
            {
                string strSql = "select WH_NO from PSB_PRODUCT t where t.PRODUCT_GUID = '" + productGuid + "'";
                var dt = Db.Connection.QueryTable(strSql);
                if (dt != null && dt.Rows.Count > 0)
                {
                    return dt.Rows[0]["WH_NO"].ToString();

                }
                else { return "未找到产品编号对应的归属仓库"; }
            }
            catch (Exception ex)
            {
                log.Error("GetWhNoFromDBS报错:" + ex.Message);
                return null;
            }
        }


        /// <summary>
        /// 初始化站台信息
        /// </summary>
        public bool LoadOpcItems(ref string errMsg)
        {
            try
            {
                #region 获取叠盘机工位信息
                var dt = GetLocData();
                if (dt == null || dt.Rows.Count <= 0)
                {
                    errMsg = "未找到站台信息";
                    return false;
                }
                foreach (DataRow row in dt.Rows)
                {
                    var loc = new Loc();
                    loc.LocNo = row["LOC_NO"].ToString();
                    loc.LocPlcNo = row["LOC_PLC_NO"].ToString();
                    loc.LocTypeNo = row["LOC_TYPE"].ToString();
                    loc.LocTypeDesc = row["LOC_TYPE_NAME"].ToString();
                    loc.TaskType = row["BIZ_TYPE"].ToString();
                    BizHandle.Instance.locDic.Add(loc.LocNo, loc);

                    BizHandle.Instance.downInfoDic.Add(loc.LocNo, new DownLoadInfo { SlocPlcNo = loc.LocPlcNo });
                }
                #endregion

                #region 初始化读取项信息
                var dtRead = GetReadItemsData();
                if (dtRead == null || dtRead.Rows.Count <= 0)
                {
                    errMsg = "未找到读取配置项信息";
                    return false;
                }
                foreach (DataRow row in dtRead.Rows)
                {
                    var opcItem = new LocOpcItem();
                    opcItem.LocNo = row["LOC_NO"].ToString();
                    opcItem.LocPlcNo = row["LOC_PLC_NO"].ToString();
                    opcItem.TagLongName = row["TAGLONGNAME"].ToString();
                    opcItem.BusIdentity = row["BUSIDENTITY"].ToString();
                    BizHandle.Instance.readItems.Add(opcItem.TagLongName, opcItem);
                }
                #endregion

                #region 初始化写入项信息
                var dtWrite = GetWriteItemsData();
                if (dtWrite == null || dtWrite.Rows.Count <= 0)
                {
                    errMsg = "未找到写入配置项信息";
                    return false;
                }
                foreach (DataRow row in dtWrite.Rows)
                {
                    var opcItem = new LocOpcItem();
                    opcItem.LocNo = row["LOC_NO"].ToString();
                    opcItem.LocPlcNo = row["LOC_PLC_NO"].ToString();
                    opcItem.TagLongName = row["TAGLONGNAME"].ToString();
                    opcItem.BusIdentity = row["BUSIDENTITY"].ToString();
                    BizHandle.Instance.writeItems.Add(opcItem.TagLongName, opcItem);
                }
                #endregion
                return true;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }
        }

        internal bool RequestFinishTaskCmd(int requestId, int taskNo, string elocNo, int v, ref string errMsg)
        {
            try
            {

                var dt = Db.Connection.QueryTable($"SELECT * FROM TPROC_0300_CMD_FINISH T WHERE T.OBJID = {requestId}");
                if (dt == null || dt.Rows.Count == 0)
                {
                    //获取指令号
                    var cmdId = Db.Connection.ExecuteScalar<int>($"SELECT NVL(MIN(T.OBJID),0) FROM WBS_TASK_CMD T WHERE T.TASK_NO = {taskNo}");
                    if (cmdId <= 0)
                    {
                        return true;
                    }
                    //插入参数表请求
                    var sb = new StringBuilder();
                    sb.Append(" INSERT INTO TPROC_0300_CMD_FINISH");
                    sb.Append(" (OBJID,CMD_OBJID,CURR_LOC_NO,FINISH_STATUS)");
                    sb.Append(" VALUES");
                    sb.Append(" (@OBJID,@CMD_OBJID,@CURR_LOC_NO,@FINISH_STATUS)");
                    var param = new DynamicParameters();
                    param.Add("OBJID", requestId);
                    param.Add("CMD_OBJID", cmdId);
                    param.Add("CURR_LOC_NO", elocNo);
                    param.Add("FINISH_STATUS", v);
                    Db.Connection.Execute(sb.ToString(), param);
                }
                else
                {
                    var cmdId = Db.Connection.ExecuteScalar<int>($"SELECT NVL(MIN(T.OBJID),0) FROM WBS_TASK_CMD T WHERE T.TASK_NO = {taskNo}");
                    if (cmdId <= 0)
                    {
                        return true;
                    }
                    string strSql = $"update TPROC_0300_CMD_FINISH set CMD_OBJID={cmdId},CURR_LOC_NO={elocNo},FINISH_STATUS={v} where OBJID ={requestId}";
                    Db.Connection.Execute(strSql);

                }
                //执行存储过程
                DynamicParameters dp = new DynamicParameters();
                dp.Add("I_PARAM_OBJID", requestId);
                dp.Add("O_ERR_CODE", 0, DbType.Int32, ParameterDirection.Output);
                dp.Add("O_ERR_DESC", 0, DbType.String, ParameterDirection.Output, size: 80);
                Db.Connection.Execute("PROC_0300_CMD_FINISH", param: dp, commandType: CommandType.StoredProcedure);
                errMsg = dp.Get<string>("O_ERR_DESC") ?? string.Empty;
                if (string.IsNullOrEmpty(errMsg))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                log.Error($"[异常]执行RequestFinishTaskCmd({requestId},{taskNo},{elocNo},ref string errMsg)请求结束指令失败:{ex.ToString()}");
                return false;
            }

        }

        internal bool DeleteTaskCmd(string whNo, int taskNo, ref string errMsg)
        {
            try
            {
                var procName = "PROC_WCS_CRNCMD_DEL";
                var param = new DynamicParameters();
                param.Add("I_TASK_NO", taskNo);
                Db.Connection.Execute(procName, param, commandType: CommandType.StoredProcedure);
                return true;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }

        }

        internal int GetObjidForCmdFinish()
        {
            try
            {
                return Db.Connection.ExecuteScalar<int>("SELECT NEXT VALUE FOR SEQ_TPROC_0300_CMD_FINISH");
            }
            catch (Exception ex)
            {
                log.Error($"[异常]执行GetObjidForCmdFinish()获取指令结束参数表主键ID异常:{ex.Message}");
                return 0;
            }
        }

        internal bool UpdateCmdStep(int cmdId, string whNo, string v)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append(" UPDATE WBS_TASK_CMD SET ");
                sb.Append(" CMD_STEP = @CMD_STEP,");
                sb.Append(" EXCUTE_DATE = getdate()");
                sb.Append(" WHERE OBJID = @OBJID");
                var param = new DynamicParameters();
                param.Add("OBJID", cmdId);
                param.Add("CMD_STEP", v);
                Db.Connection.Execute(sb.ToString(), param);
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"[异常]执行UpdateCmdStep({cmdId},{v})修改指令步骤失败:{ex.ToString()}");
                return false;
            }

        }

        /// <summary>
        /// 获取站台信息
        /// </summary>
        public DataTable GetLocData()
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append(" SELECT T.LOC_NO ,T.LOC_PLC_NO ,T.LOC_TYPE ,T2.LOC_TYPE_NAME,T.BIZ_TYPE");
                sb.Append(" FROM PSB_OPC_LOC_GROUP T");
                sb.Append(" LEFT JOIN PSB_LOC T1 ON T1.LOC_NO = T.LOC_NO");
                sb.Append(" LEFT JOIN PSB_LOC_TYPE T2 ON T2.LOC_TYPE_NO = T1.LOC_TYPE_NO");
                sb.Append(" WHERE T.KIND='Stacking'");

                return Db.Connection.QueryTable(sb.ToString());
            }
            catch (Exception ex)
            {
                log.Error($"[异常]执行GetLocData()获取站台信息失败:{ex.ToString()}");
                return null;
            }
        }

        /// <summary>
        /// 获取读取项
        /// </summary>
        public DataTable GetReadItemsData()
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append(" SELECT T.TAGGROUP + T1.BUSIDENTITY TAGLONGNAME");
                sb.Append(" ,T.LOC_NO");
                sb.Append(" ,T.LOC_PLC_NO");
                sb.Append(" ,T1.BUSIDENTITY");
                sb.Append(" FROM PSB_OPC_LOC_GROUP T");
                sb.Append(" LEFT JOIN PSB_OPC_LOC_ITEMS T1 ON T1.KIND = T.KIND");
                sb.Append(" WHERE T.ISENABLE = 1");
                sb.Append(" AND T1.ISENABLE = 1");
                sb.Append(" AND T1.BUSIDENTITY LIKE 'Read.%'");
                sb.Append(" AND T1.KIND='Stacking'");
                return Db.Connection.QueryTable(sb.ToString());
            }
            catch (Exception ex)
            {
                log.Error($"[异常]执行GetReadItemsData()获取站台读取项信息失败:{ex.ToString()}");
                return null;
            }
        }

        /// <summary>
        /// 获取写入项
        /// </summary>
        public DataTable GetWriteItemsData()
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append(" SELECT T.TAGGROUP + T1.BUSIDENTITY TAGLONGNAME");
                sb.Append(" ,T.LOC_NO");
                sb.Append(" ,T.LOC_PLC_NO");
                sb.Append(" ,T1.BUSIDENTITY");
                sb.Append(" FROM PSB_OPC_LOC_GROUP T");
                sb.Append(" LEFT JOIN PSB_OPC_LOC_ITEMS T1 ON T1.KIND = T.KIND");
                sb.Append(" WHERE T.ISENABLE = 1");
                sb.Append(" AND T1.ISENABLE = 1");
                sb.Append(" AND T1.BUSIDENTITY LIKE 'Write.%'");
                sb.Append(" AND T1.KIND='Stacking'");
                return Db.Connection.QueryTable(sb.ToString());
            }
            catch (Exception ex)
            {
                log.Error($"[异常]执行GetWriteItemsData()获取站台写入项信息失败:{ex.ToString()}");
                return null;
            }
        }

        /// <summary>
        /// 获取请求Objid
        /// </summary>
        public int SelectObjid()
        {
            try
            {
                return Db.Connection.ExecuteScalar<int>("SELECT NEXT VALUE FOR DBO.SEQ_TPROC_0100_TASK_REQUEST");
            }
            catch (Exception ex)
            {
                log.Error($"[异常]SelectObjid()获取参数表主键ID异常:{ex.Message}");
                return 0;
            }
        }

      
        /// <summary>
        /// 请求生成任务
        /// </summary>
        public int RequestTask(Loc loc, ref string errMsg)
        {
            try
            {
                var dt = Db.Connection.QueryTable($"SELECT * FROM TPROC_0100_TASK_REQUEST T WHERE T.OBJID = {loc.RequestTaskObjid}");
                if (dt == null || dt.Rows.Count == 0)
                {
                    var sb = new StringBuilder();
                    sb.Append(" INSERT INTO TPROC_0100_TASK_REQUEST");
                    sb.Append(" (OBJID, ORDER_TYPE_NO, SLOC_NO, PALLET_NO, PALLET_QTY)");
                    sb.Append(" VALUES ");
                    sb.Append(" (@OBJID, @ORDERTYPENO ,@SLOCNO, @PALLETNO, @PALLET_QTY)");
                    var param = new DynamicParameters();
                    param.Add("OBJID", loc.RequestTaskObjid);
                    param.Add("ORDERTYPENO", "100063");
                    param.Add("SLOCNO", loc.LocNo);
                    param.Add("PALLETNO", loc.plcStatus.PalletNo);
                    param.Add("PALLET_QTY", decimal.Parse(loc.plcStatus.PalletQty));

                    Db.Connection.Execute(sb.ToString(), param);
                }
                else
                {
                    if (dt.Rows[0]["PROC_STATUS"].Equals("2") && dt.Rows[0]["ERR_CODE"].Equals("0"))
                    {
                        return 0;
                    }
                    else
                    {
                        string strSql = $"update TPROC_0100_TASK_REQUEST set ORDERTYPENO='100063',SLOCNO={loc.LocNo},PALLETNO={loc.plcStatus.PalletNo},PALLET_QTY={decimal.Parse(loc.plcStatus.PalletQty)} where OBJID={loc.RequestTaskObjid} ";
                        Db.Connection.Execute(strSql);

                    }
                }
                //执行存储过程
                var dp = new DynamicParameters();
                dp.Add("I_PARAM_OBJID", loc.RequestTaskObjid);
                dp.Add("O_TASK_NO", 0, DbType.Int32, ParameterDirection.Output);
                dp.Add("O_ERR_CODE", 0, DbType.Int32, ParameterDirection.Output);
                dp.Add("O_ERR_DESC", 0, DbType.String, ParameterDirection.Output, size: 80);
                Db.Connection.Execute("PROC_0100_TASK_REQUEST", param: dp, commandType: CommandType.StoredProcedure);
                errMsg = dp.Get<string>("O_ERR_DESC");
                return dp.Get<int>("O_TASK_NO");
            }
            catch (Exception ex)
            {
                log.Error($"[异常]执行RequestTask({loc.RequestTaskObjid},{loc.LocNo},{loc.plcStatus.PalletNo},ref string errMsg)请求生成任务失败:{ex.ToString()}");
                errMsg = ex.Message;
                return -1;
            }
        }



        /// <summary>
        /// 更新站台状态
        /// </summary>
        public bool RecordPlcInfo(Loc loc)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append("UPDATE [dbo].[PEM_STACKING_LOC_STATUS]");
                sb.Append(" SET");
                sb.Append("[Update_Date] = getdate()");
                sb.Append(" ,[PalletQty] = @PalletQty");
                sb.Append(" ,[PALLET_NO] = @PalletNo");
                sb.Append(" ,[TASK_NO] = @taskNo");
                sb.Append(" ,[Status_Auto] = @StatusAuto");
                sb.Append(" ,[Status_Fault] = @StatusFault");
                sb.Append(" ,[Status_Load] = @StatusLoad");
                sb.Append(" ,[Status_RequestTask] = @StatusRequestTask");
                sb.Append(" ,[Status_FreeAndPut] = @StatusFreeAndPut");
                sb.Append(" ,[Stauts_BusyAndTake] = @StautsBusyAndTake");
                sb.Append(" WHERE LOC_NO = @LocNo");
                var param = new DynamicParameters();
                param.Add("PalletQty", loc.plcStatus.PalletQty);
                param.Add("PalletNo", loc.plcStatus.PalletNo);
                param.Add("taskNo", loc.plcStatus.TaskNo);

                param.Add("StatusAuto", loc.plcStatus.StatusAuto);
                param.Add("StatusFault", loc.plcStatus.StatusFault);
                param.Add("StatusLoad", loc.plcStatus.StatusLoad);
                param.Add("StatusRequestTask", loc.plcStatus.StatusRequestTask);
                param.Add("StatusFreeAndPut", loc.plcStatus.StatusFreeAndPut);
                param.Add("StautsBusyAndTake", loc.plcStatus.StatusBusyAndTake);
                param.Add("LocNo", loc.LocNo);
                Db.Connection.Execute(sb.ToString(), param);
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"[异常]执行RecordPlcInfo()更新站台状态失败:{ex.ToString()}");
                return false;
            }
        }
    }
}
