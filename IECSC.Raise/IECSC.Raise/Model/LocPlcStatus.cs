using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IECSC.Raise
{
    public class LocPlcStatus
    {
        /// <summary>
        /// 任务号
        /// </summary>
        public long TaskNo { get; set; }
     
        /// <summary>
        /// 提升机区域
        /// </summary>
        public string RaiseArea { get; set; }

        /// <summary>
        /// 提升机编号
        /// </summary>
        public string DeviceNo { get; set; }

        /// <summary>
        /// 实际重量
        /// </summary>
        public string RealWeight { get; set; }

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
        /// 源地址
        /// </summary>
        //public string Sloc => this.SlocArea + this.SlocNo;
        /// <summary>
        /// 源地址区域符号
        /// </summary>
        //public string SlocArea { get; set; }
        /// <summary>
        /// 源地址设备Id
        /// </summary>
        //public string SlocNo { get; set; }
        /// <summary>
        /// 源地址
        /// </summary>
        //public string Eloc => this.ElocArea + this.ElocNo;
        /// <summary>
        /// 目的地址区域符号
        /// </summary>
        //public string ElocArea { get; set; }
        /// <summary>
        /// 目的地址设备ID
        /// </summary>
        //public string ElocNo { get; set; }
        /// <summary>
        /// 自动 标识
        /// </summary>
        public int StatusAuto { get; set; }
        /// <summary>
        /// 故障 标识
        /// </summary>
        public int StatusFault { get; set; }
        /// <summary>
        /// 有载 标识
        /// </summary>
        //public int StatusLoading { get; set; }
        /// <summary>
        /// 请求任务 标识
        /// </summary>
        public int StatusRequestTask { get; set; }
        /// <summary>
        /// 空闲可放货 标识
        /// </summary>
        public int StatusFreeAndPut { get; set; }
        /// <summary>
        /// 有货需取货 标识
        /// </summary>
        public int StatusBusyAndTake { get; set; }
        /// <summary>
        /// 有载 标识
        /// </summary>
        public int StatusLoad { get; set; }
        /// <summary>
        /// 重量
        /// </summary>
        //public int ProductWeight { get; set; }

    }
}
