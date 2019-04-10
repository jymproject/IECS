using Dapper;
using DapperExtensions;
using MSTL.DbClient;
using MSTL.LogAgent;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace IECSC.Grouping
{
    public partial class DbAction
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
            ConnDb(McConfig.Instance.DbConnect,ref errMsg);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool ConnDb(string dbConnect, ref string errMsg)
        {
            try
            {
                this.Db = DbHelper.GetDb(dbConnect, DbHelper.DataBaseType.SqlServer, ref errMsg);
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
        /// 获取数据库时间
        /// </summary>
        public bool GetDbTime()
        {
            try
            {
                var dt = Db.Connection.QueryTable("SELECT GETDATE()");
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
                    ConnDb(McConfig.Instance.DbConnect, ref errMsg);
                }
                else
                {
                    log.Error($"[异常]执行GetDbTime()获取服务器时间失败:{ex.ToString()}");
                }
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
                sb.Append(" WHERE T.KIND='Grouping'");

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
                sb.Append(" AND T1.KIND='Grouping'");
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
                sb.Append(" AND T1.KIND='Grouping'");
                return Db.Connection.QueryTable(sb.ToString());
            }
            catch (Exception ex)
            {
                log.Error($"[异常]执行GetWriteItemsData()获取站台写入项信息失败:{ex.ToString()}");
                return null;
            }
        }

        /// <summary>
        /// 获取归属仓库编号
        /// </summary>
        public string GetWhNo(string productGuid, ref string errMsg)
        {
            try
            {
                if(string.IsNullOrEmpty(productGuid))
                {
                    errMsg = "产品GUID信息异常";
                    return null;
                }
                var sb = new StringBuilder();
                sb.Append(" SELECT WH_NO FROM PSB_PRODUCT T ");
                sb.Append(" WHERE T.PRODUCT_GUID = @PRODUCT_GUID");
                var param = new DynamicParameters();
                param.Add("PRODUCT_GUID", productGuid);
                var dt = Db.Connection.QueryTable(sb.ToString(), param);
                if (dt == null || dt.Rows.Count <= 0)
                {
                    errMsg = $"产品GUID[{productGuid}]未绑定归属仓库";
                    return string.Empty;
                }
                return dt.Rows[0]["WH_NO"].ToString();
            }
            catch (Exception ex)
            {
                log.Error($"GetWhNoByProductGuid({productGuid})获取归属仓库异常:" + ex.ToString());
                errMsg = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// 初始化仓库
        /// </summary>
        public IDatabase GetDb(string whNo, ref string errMsg)
        {
            try
            {
                IDatabase dbConn = null;
                if(whNo.Equals(McConfig.Instance.WhNo2))
                {
                    dbConn =  DbHelper.GetDb(McConfig.Instance.DbConnect_2LK, DbHelper.DataBaseType.SqlServer, ref errMsg);
                }
                if(whNo.Equals(McConfig.Instance.WhNo4))
                {
                    dbConn = DbHelper.GetDb(McConfig.Instance.DbConnect_4LK, DbHelper.DataBaseType.SqlServer, ref errMsg);
                }
                return dbConn;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
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
                return loc.Db.Connection.QueryTable(sb.ToString(), param);
            }
            catch (Exception ex)
            {
                log.Error($"[异常]执行GetTaskCmd({loc}, {cmdStep})获取指令信息失败:{ex.ToString()}");
                return null;
            }
        }

        /// <summary>
        /// 获取请求任务Objid
        /// </summary>
        public int GetRequestTaskObjid(Loc loc)
        {
            try
            {
                return loc.Db.Connection.ExecuteScalar<int>("SELECT NEXT VALUE FOR DBO.SEQ_TPROC_0100_TASK_REQUEST");
            }
            catch (Exception ex)
            {
                log.Error($"[异常]执行GetRequestTaskObjid()获取请求生成任务OBJID异常:{ex.Message}");
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
                var dt = loc.Db.Connection.QueryTable($"SELECT * FROM TPROC_0100_TASK_REQUEST T WHERE T.OBJID = {loc.RequestTaskObjid}");
                var sb = new StringBuilder();
                if (dt == null || dt.Rows.Count <= 0)
                {
                    sb.Append(" INSERT INTO TPROC_0100_TASK_REQUEST");
                    sb.Append(" (OBJID, ORDER_TYPE_NO, SLOC_NO, PALLET_NO, REAL_WEIGHT,PRODUCT_GUID)");
                    sb.Append(" VALUES ");
                    sb.Append(" (@OBJID, @ORDERTYPENO ,@SLOCNO, @PALLETNO, @REAL_WEIGHT,@PRODUCT_GUID)");
                }
                else
                {
                    sb.Append(" UPDATE TPROC_0100_TASK_REQUEST");
                    sb.Append(" SET ORDER_TYPE_NO = @ORDERTYPENO)");
                    sb.Append(" ,SLOC_NO = @SLOCNO");
                    sb.Append(" ,PALLET_NO = @PALLETNO");
                    sb.Append(" ,REAL_WEIGHT = @REAL_WEIGHT");
                    sb.Append(" ,PRODUCT_GUID = @PRODUCT_GUID");
                    sb.Append(" WHERE OBJID = @OBJID");
                }
                var param = new DynamicParameters();
                param.Add("OBJID", loc.RequestTaskObjid);
                param.Add("ORDERTYPENO", "100064");
                param.Add("SLOCNO", loc.LocNo);
                param.Add("PALLETNO", loc.plcStatus.PalletNo);
                param.Add("PRODUCTWEIGHT", decimal.Parse(loc.plcStatus.RealWeight));
                param.Add("PRODUCT_GUID", loc.plcStatus.ProductGuid);
                loc.Db.Connection.Execute(sb.ToString(), param);
                //执行存储过程
                var dp = new DynamicParameters();
                dp.Add("I_PARAM_OBJID", loc.RequestTaskObjid);
                dp.Add("O_TASK_NO", 0, DbType.Int32, ParameterDirection.Output);
                dp.Add("O_ERR_CODE", 0, DbType.Int32, ParameterDirection.Output);
                dp.Add("O_ERR_DESC", 0, DbType.String, ParameterDirection.Output, size: 80);
                loc.Db.Connection.Execute("PROC_0100_TASK_REQUEST", param: dp, commandType: CommandType.StoredProcedure);
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
        /// 获取请求指令OBJID
        /// </summary>
        public int GetRequestCmdObjid(Loc loc)
        {
            try
            {
                return loc.Db.Connection.ExecuteScalar<int>("SELECT NEXT VALUE FOR DBO.SEQ_TPROC_0200_CMD_REQUEST"); ;
            }
            catch (Exception ex)
            {
                log.Error($"[异常]执行GetRequestCmdObjid()获取请求生成指令OBJID失败:{ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 请求生成指令
        /// </summary>
        public int RequestCmd(Loc loc, int taskNo, ref string errMsg)
        {
            try
            {
                var dt = loc.Db.Connection.QueryTable($"SELECT * FROM TPROC_0200_CMD_REQUEST T WHERE T.OBJID = {loc.RequestCmdObjid}");
                var sb = new StringBuilder();
                if (dt == null || dt.Rows.Count <= 0)
                {
                    sb.Append(" INSERT INTO TPROC_0200_CMD_REQUEST");
                    sb.Append(" (OBJID, TASK_NO, CURR_LOC_NO)");
                    sb.Append(" VALUES");
                    sb.Append(" (@OBJID, @TASKNO, @LOCNO)");
                }
                else
                {
                    sb.Append(" UPDATE TPROC_0200_CMD_REQUEST");
                    sb.Append(" SET TASK_NO = @TASKNO");
                    sb.Append(" ,CURR_LOC_NO = @LOCNO");
                    sb.Append(" WHERE OBJID = @OBJID");
                }
                var param = new DynamicParameters();
                param.Add("OBJID", loc.RequestCmdObjid);
                param.Add("TASKNO", taskNo);
                param.Add("LOCNO", loc.LocNo);
                loc.Db.Connection.Execute(sb.ToString(), param);
                //执行存储过程
                var dp = new DynamicParameters();
                dp.Add("I_PARAM_OBJID", loc.RequestCmdObjid);
                dp.Add("O_CMD_OBJID", 0, DbType.Int32, ParameterDirection.Output);
                dp.Add("O_ERR_CODE", 0, DbType.Int32, ParameterDirection.Output);
                dp.Add("O_ERR_DESC", 0, DbType.String, ParameterDirection.Output, size: 80);
                loc.Db.Connection.Execute("PROC_0200_CMD_REQUEST", param: dp, commandType: CommandType.StoredProcedure);
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
        /// 修改指令步骤
        /// </summary>
        public bool UpdateCmdStep(Loc loc, string cmdStep)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append(" UPDATE WBS_TASK_CMD");
                sb.Append(" SET CMD_STEP = @CMD_STEP,");
                sb.Append(" EXCUTE_DATE = getdate()");
                sb.Append(" WHERE OBJID = @OBJID");
                var param = new DynamicParameters();
                param.Add("OBJID", loc.taskCmd.Objid);
                param.Add("CMD_STEP", cmdStep);
                loc.Db.Connection.Execute(sb.ToString(), param);
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"[异常]执行UpdateCmdStep({loc.taskCmd.Objid},{cmdStep})修改指令步骤失败:{ex.ToString()}");
                return false;
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
                sb.Append(" T.ELOC_NO,T.ELOC_PLC_NO,T2.LOC_TYPE_NAME ELOC_TYPE,T.PALLET_NO FROM VIEW_WBS_TASK_CMD T");
                sb.Append(" LEFT JOIN PSB_LOC_TYPE T1 ON T1.LOC_TYPE_NO = T.SLOC_TYPE");
                sb.Append(" LEFT JOIN PSB_LOC_TYPE T2 ON T2.LOC_TYPE_NO = T.ELOC_TYPE");
                sb.Append(" WHERE T.TRANSFER_TYPE = '20'");
                sb.Append(" AND T.SLOC_NO = @SLOCNO");
                sb.Append(" ORDER BY T.OBJID DESC");
                var param = new DynamicParameters();
                param.Add("SLOCNO", locNo);
                var dt = Db.Connection.QueryTable(sb.ToString(), param);
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
        /// 获取指令结束OBJID
        /// </summary>
        public int GetCmdFinishObjid(string whNo)
        {
            try
            {
                var errMsg = string.Empty;
                var connDb = GetDb(whNo, ref errMsg);
                return connDb.Connection.ExecuteScalar<int>("SELECT NEXT VALUE FOR SEQ_TPROC_0300_CMD_FINISH");
            }
            catch (Exception ex)
            {
                log.Error($"[异常]执行GetCmdFinishObjid()获取指令结束OBJID异常:{ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 请求结束指令
        /// </summary>
        public bool RequestFinishCmd(string whNo, int requestId, int taskNo, string elocNo, int finishStatus, ref string errMsg)
        {
            try
            {
                var connDb = GetDb(whNo, ref errMsg);
                //获取指令号
                var cmdId = connDb.Connection.ExecuteScalar<int>($"SELECT NVL(MIN(T.OBJID),0) FROM WBS_TASK_CMD T WHERE T.TASK_NO = {taskNo}");
                if (cmdId <= 0)
                {
                    return true;
                }
                var dt = connDb.Connection.QueryTable($"SELECT * FROM TPROC_0300_CMD_FINISH T WHERE T.OBJID = {requestId}");
                var sb = new StringBuilder();
                if (dt == null || dt.Rows.Count == 0)
                {
                    sb.Append(" INSERT INTO TPROC_0300_CMD_FINISH");
                    sb.Append(" (OBJID,CMD_OBJID,CURR_LOC_NO,FINISH_STATUS)");
                    sb.Append(" VALUES");
                    sb.Append(" (@OBJID,@CMD_OBJID,@CURR_LOC_NO,@FINISH_STATUS)");
                }
                else
                {
                    sb.Append(" UPDATE TPROC_0300_CMD_FINISH");
                    sb.Append(" SET CMD_OBJID = @CMD_OBJID");
                    sb.Append(" ,CURR_LOC_NO = @CURR_LOC_NO");
                    sb.Append(" ,FINISH_STATUS = @FINISH_STATUS");
                    sb.Append(" WHERE OBJID = @OBJID");
                }
                var param = new DynamicParameters();
                param.Add("OBJID", requestId);
                param.Add("CMD_OBJID", cmdId);
                param.Add("CURR_LOC_NO", elocNo);
                param.Add("FINISH_STATUS", finishStatus);
                connDb.Connection.Execute(sb.ToString(), param);

                //执行存储过程
                DynamicParameters dp = new DynamicParameters();
                dp.Add("I_PARAM_OBJID", requestId);
                dp.Add("O_ERR_CODE", 0, DbType.Int32, ParameterDirection.Output);
                dp.Add("O_ERR_DESC", 0, DbType.String, ParameterDirection.Output, size: 80);
                connDb.Connection.Execute("PROC_0300_CMD_FINISH", param: dp, commandType: CommandType.StoredProcedure);
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

        /// <summary>
        /// 删除指令
        /// </summary>
        public bool DeleteTaskCmd(string whNo, int taskNo, ref string errMsg)
        {
            try
            {
                var connDb = GetDb(whNo, ref errMsg);
                var procName = "PROC_WCS_CRNCMD_DEL";
                var param = new DynamicParameters();
                param.Add("I_TASK_NO", taskNo);
                connDb.Connection.Execute(procName, param, commandType: CommandType.StoredProcedure);
                return true;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }

        }

        /// <summary>
        /// 修改指令步骤
        /// </summary>
        internal bool UpdateCmdStep(int cmdId, string whNo, string cmdStep)
        {
            try
            {
                var errMsg = string.Empty;
                var connDb = GetDb(whNo, ref errMsg);
                var sb = new StringBuilder();
                sb.Append(" UPDATE WBS_TASK_CMD SET ");
                sb.Append(" CMD_STEP = @CMD_STEP,");
                sb.Append(" EXCUTE_DATE = getdate()");
                sb.Append(" WHERE OBJID = @OBJID");
                var param = new DynamicParameters();
                param.Add("OBJID", cmdId);
                param.Add("CMD_STEP", cmdStep);
                connDb.Connection.Execute(sb.ToString(), param);
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"[异常]执行UpdateCmdStep({cmdId},{cmdStep})修改指令步骤失败:{ex.ToString()}");
                return false;
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
                sb.Append("UPDATE [dbo].[PEM_GROUPING_LOC_STATUS]");
                sb.Append(" SET");
                sb.Append("[Update_Date] = getdate()");
                sb.Append(" ,[RealWeight] = @RealWeight");
                sb.Append(" ,[ProductGuid] = @ProductGuid");
                sb.Append(" ,[PalletNo] = @PalletNo");
                sb.Append(" ,[Status_Auto] = @StatusAuto");
                sb.Append(" ,[Status_Fault] = @StatusFault");
                sb.Append(" ,[Status_Load] = @StatusLoad");
                sb.Append(" ,[Status_RequestTask] = @StatusRequestTask");
                sb.Append(" ,[Status_FreeAndPut] = @StatusFreeAndPut");
                sb.Append(" ,[Stauts_BusyAndTake] = @StautsBusyAndTake");
                sb.Append(" ,[PalletType] = PalletType");
                sb.Append(" WHERE LOC_NO = @LocNo");
                var param = new DynamicParameters();
                param.Add("RealWeight", loc.plcStatus.RealWeight);
                param.Add("ProductGuid", loc.plcStatus.ProductGuid);
                param.Add("PalletNo", loc.plcStatus.PalletNo);
                param.Add("StatusAuto", loc.plcStatus.StatusAuto);
                param.Add("StatusFault", loc.plcStatus.StatusFault);
                param.Add("StatusLoad", loc.plcStatus.StatusLoad);
                param.Add("StatusRequestTask", loc.plcStatus.StatusRequestTask);
                param.Add("StatusFreeAndPut", loc.plcStatus.StatusFreeAndPut);
                param.Add("StautsBusyAndTake", loc.plcStatus.StatusBusyAndTake);
                param.Add("PalletType", loc.plcStatus.PalletType);
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
