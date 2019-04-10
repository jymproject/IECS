
using System;
using System.ComponentModel;
using System.Text;

namespace IECSC.Raise
{ 
    public class DownLoadInfo 
    {
        /// <summary>
        /// 站台号
        /// </summary>
        public string LocPlcNo { get; set; }
        /// <summary>
        /// 产品编号
        /// </summary>
        public string ProductGuid { get; set; }
        /// <summary>
        /// 标准重量
        /// </summary>
        public string StandardWeight { get; set; }
        /// <summary>
        /// 允许误差范围
        /// </summary>
        public string AllowErrRange { get; set; }
        /// <summary>
        /// 工装类型
        /// </summary>
        public string PalletType { get; set; }

        /// <summary>
        /// 站台区域
        /// </summary>
        public int LocArea
        {
            get
            {
                return (int)Encoding.ASCII.GetBytes(LocPlcNo.Substring(0, 1))[0];
            }
        }
        /// <summary>
        /// 站台编号
        /// </summary>
        public string LocCode
        {
            get
            {
                return LocPlcNo.Substring(1);
            }
        }
    }
}
