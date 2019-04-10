
using System;
using System.ComponentModel;
using System.Text;

namespace IECSC.Spliting
{
    public class DownLoadInfo
    {

        /// <summary>
        /// Objid
        /// </summary>
        public int Objid { get; set; }

        /// <summary>
        /// 指令类型
        /// </summary>
        public string CmdType { get; set; }
        /// <summary>
        /// 指令步骤
        /// </summary>
        public string CmdStep { get; set; }
        /// <summary>
        /// 归属库区
        /// </summary>
        public string WhNo { get; set; }
        /// <summary>
        /// 起始地址类型
        /// </summary>
        public string SlocType { get; set; }
        /// <summary>
        /// 起始地址
        /// </summary>
        public string SlocNo { get; set; }

        /// <summary>
        /// 目的地址类型
        /// </summary>
        public string ElocType { get; set; }
        /// <summary>
        /// 结束地址
        /// </summary>
        public string ElocNo { get; set; }

        /// <summary>
        /// 源站台号
        /// </summary>
        public string SlocPlcNo { get; set; }

        /// <summary>
        /// 目的站台号
        /// </summary>
        public string ElocPlcNo { get; set; }
        /// <summary>
        /// 产品编号
        /// </summary>
        public string ProductGuid { get; set; }
        /// <summary>
        /// 流水号
        /// </summary>
        public int SerialNo { get; set; }
        /// <summary>
        /// 允许误差范围
        /// </summary>
        public string AllowErrRange { get; set; }
        /// <summary>
        /// 工装编号
        /// </summary>
        public string PalletNo { get; set; }
        /// <summary>
        /// 工装类型
        /// </summary>
        public string PalletType { get; set; }

        /// <summary>
        /// 站台区域
        /// </summary>
        public int SlocArea
        {
            get
            {
                return (int)Encoding.ASCII.GetBytes(SlocPlcNo?.Substring(0, 1))[0];
            }
        }
        /// <summary>
        /// 站台编号
        /// </summary>
        public string SlocCode
        {
            get
            {
                return SlocPlcNo?.Substring(1);
            }
        }

        /// <summary>
        /// 站台区域
        /// </summary>
        public int ElocArea
        {
            get
            {
                return (int)Encoding.ASCII.GetBytes(ElocPlcNo?.Substring(0, 1))[0];
            }
        }
        /// <summary>
        /// 站台编号
        /// </summary>
        public string ElocCode
        {
            get
            {
                return ElocPlcNo?.Substring(1);
            }
        }
    }
}
