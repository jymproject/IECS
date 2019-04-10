using System;
using System.Collections.Generic;
using System.Data;

namespace IECSC.Grouping
{
    public partial class DbAction
    {
        /// <summary>
        /// 初始化站台信息
        /// </summary>
        public bool LoadOpcItems(ref string errMsg)
        {
            try
            {
                #region 获取组盘工位信息
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
    }
}
