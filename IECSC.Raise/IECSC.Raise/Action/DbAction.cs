using Dapper;
using DapperExtensions;
using MSTL.DbClient;
using MSTL.LogAgent;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IECSC.Raise
{
    public class DbAction
    { 
        /// <summary>
      /// 数据库操作类
      /// </summary>
        private IDatabase Db = null;
        private static DbAction _instance = null;

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
        /// 初始化站台信息
        /// </summary>
        public bool LoadOpcItems(ref string errMsg)
        {
            try
            {
                #region 获取提升机信息
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

                    BizHandle.Instance.downInfoDic.Add(loc.LocNo, new DownLoadInfo());
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

        /// <summary>
        /// 获取站台信息
        /// </summary>
        public DataTable GetLocData()
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append("SELECT T.LOC_NO ,T.LOC_PLC_NO ,T.LOC_TYPE ,T2.LOC_TYPE_NAME,T.BIZ_TYPE");
                sb.Append(" FROM PSB_OPC_LOC_GROUP T");
                sb.Append(" LEFT JOIN PSB_LOC T1 ON T1.LOC_NO = T.LOC_NO");
                sb.Append(" LEFT JOIN PSB_LOC_TYPE T2 ON T2.LOC_TYPE_NO = T1.LOC_TYPE_NO");
                sb.Append(" WHERE T.KIND='Raise'");

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
                sb.Append("SELECT T.TAGGROUP + T1.TAGNAME TAGLONGNAME");
                sb.Append(" ,T.LOC_NO");
                sb.Append(" ,T.LOC_PLC_NO");
                sb.Append(" ,T1.BUSIDENTITY");
                sb.Append(" FROM PSB_OPC_LOC_GROUP T");
                sb.Append(" LEFT JOIN PSB_OPC_LOC_ITEMS T1 ON T1.KIND = T.KIND");
                sb.Append(" WHERE T.ISENABLE = 1");
                sb.Append(" AND T1.ISENABLE = 1");
                sb.Append(" AND T1.BUSIDENTITY LIKE 'Read.%'");
                sb.Append(" AND T1.KIND='Raise'");
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
                sb.Append("SELECT T.TAGGROUP + T1.TAGNAME TAGLONGNAME");
                sb.Append(" ,T.LOC_NO");
                sb.Append(" ,T.LOC_PLC_NO");
                sb.Append(" ,T1.BUSIDENTITY");
                sb.Append(" FROM PSB_OPC_LOC_GROUP T");
                sb.Append(" LEFT JOIN PSB_OPC_LOC_ITEMS T1 ON T1.KIND = T.KIND");
                sb.Append(" WHERE T.ISENABLE = 1");
                sb.Append(" AND T1.ISENABLE = 1");
                sb.Append(" AND T1.BUSIDENTITY LIKE 'Write.%'");
                sb.Append(" AND T1.KIND='Raise'");
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
                return Db.Connection.ExecuteScalar<int>("SELECT NEXT VALUE FOR DBO.SEQ_TPROC_BIND_PRODUCT");
            }
            catch (Exception ex)
            {
                log.Error($"[异常]SelectObjid()获取参数表主键ID异常:{ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 插入参数表数据
        /// </summary>
        public bool InsertInfo(Loc loc)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append("INSERT INTO TPROC_0010_BIND_PRODUCT");
                sb.Append(" ([OBJID],[REAL_WEIGHT],[SLOC_NO])");
                sb.Append(" VALUES");
                sb.Append(" @OBJID, @REALWEIGHT, @SLOC_NO");
                var param = new DynamicParameters();
                param.Add("OBJID", loc.GlobalObjid);
                param.Add("REALWEIGHT", loc.plcStatus.RealWeight);
                param.Add("SLOC_NO", loc.LocNo);
                return Db.Connection.Execute(sb.ToString(), param) > 0;
            }
            catch (Exception ex)
            {
                log.Error($"[异常]InsertInfo()插入信息至参数表TPROC_0010_BIND_PRODUCT失败:{ex.ToString()}");
                return false;
            }
        }

        public DownLoadInfo ExcuteProcedure(Loc loc, ref string errMsg)
        {
            try
            {
                var para = new DynamicParameters();
                para.Add("I_PARAM_OBJID", loc.GlobalObjid);
                para.Add("O_ERR_CODE", 0, DbType.Int32, ParameterDirection.Output);
                para.Add("O_ERR_DESC", 0, DbType.String, ParameterDirection.Output, size: 80);
                para.Add("O_PRODUCT_GUID", 0, DbType.String, ParameterDirection.Output, size: 80);
                para.Add("O_STANDARD_WEIGHT", 0, DbType.Decimal, ParameterDirection.Output);
                para.Add("O_ERROR_WEIGHT", 0, DbType.Decimal, ParameterDirection.Output);
                Db.Connection.Execute("PROC_0010_BIND_PRODUCT", param: para, commandType: CommandType.StoredProcedure);
                errMsg = para.Get<string>("O_ERR_DESC") ?? string.Empty;
                if (string.IsNullOrEmpty(errMsg))
                {
                    var info = new DownLoadInfo();
                    info.LocPlcNo = loc.LocPlcNo;
                    info.ProductGuid = para.Get<string>("O_PRODUCT_GUID") ?? string.Empty;
                    info.StandardWeight = para.Get<string>("O_STANDARD_WEIGHT") ?? string.Empty;
                    info.AllowErrRange = para.Get<string>("O_ERROR_WEIGHT") ?? string.Empty;

                    if(string.IsNullOrEmpty(info.ProductGuid) || string.IsNullOrEmpty(info.StandardWeight) || string.IsNullOrEmpty(info.AllowErrRange))
                    {
                        errMsg = "反馈信息异常！";
                    }
                    else
                    {
                        errMsg = string.Empty;
                        return info;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return null;
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
                sb.Append("UPDATE [dbo].[PEM_RAISE_LOC_STATUS]");
                sb.Append(" SET");
                sb.Append(" ,[Update_Date] = getdate()");
                sb.Append(" ,[RealWeight] = @RealWeight");
                sb.Append(" ,[ProductGuid] = @ProductGuid");
                sb.Append(" ,[StandardWeight] = @StandardWeight");
                sb.Append(" ,[AllowErrRange] = @AllowErrRange");
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
                param.Add("StandardWeight", loc.plcStatus.StandardWeight);
                param.Add("AllowErrRange", loc.plcStatus.AllowErrRange);
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
