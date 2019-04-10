using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IECSC.Stacking
{
    public class LocPlcStatus
    {
        /// <summary>
        /// 任务号
        /// </summary>
        public int TaskNo { get; set; }

 


        /// <summary>
        /// 工装数量
        /// </summary>
        public string PalletQty { get; set; }

        /// <summary>
        /// 源地址
        /// </summary>
        public string Sloc => this.SlocArea + this.SlocNo;
        /// <summary>
        /// 源地址区域符号
        /// </summary>
        public string SlocArea { get; set; }
        /// <summary>
        /// 源地址设备Id
        /// </summary>
        public string SlocNo { get; set; }
        /// <summary>
        /// 源地址
        /// </summary>
        public string Eloc => this.ElocArea + this.ElocNo;
        /// <summary>
        /// 目的地址区域符号
        /// </summary>
        public string ElocArea { get; set; }
        /// <summary>
        /// 目的地址设备ID
        /// </summary>
        public string ElocNo { get; set; }
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
        /// 工装编号
        /// </summary>
        public string PalletNo { get; set; }

    }
}
