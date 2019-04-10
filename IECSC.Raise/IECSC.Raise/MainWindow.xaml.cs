using IECSC.Raise.Action;
using IECSC.Raise.CustomControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace IECSC.Raise
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 定义委托进行跨线程操作控件
        /// </summary>
        private delegate void FlushForm(string msg, string locNo, InfoType infoType);
        /// <summary>
        /// 站台列列表
        /// </summary>
        private Dictionary<string, LocControl> locControlDic = new Dictionary<string, LocControl>();
        /// <summary>
        /// 限定站台
        /// </summary>
        private string LimitLocNo = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            ShowFormData.Instance.OnAppDtoData += ShowInfo;

            //登陆时间
            this.lbTime.Content = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            //初始化
            var errMsg = string.Empty;
            if (!InitDb(ref errMsg))
            {
                return;
            }
            if (!InitOpc(ref errMsg))
            {
                return;
            }
            //站台初始化
            InitLocControl();
            //业务处理
            var thBiz = new Thread(Run);
            thBiz.IsBackground = true;
            thBiz.Start();
            //PLC连接状态监控
            var thConn = new Thread(ConnStatus);
            thConn.IsBackground = true;
            thConn.Start();
        }

        /// <summary>
        /// 初始化数据库配置信息
        /// </summary>
        private bool InitDb(ref string errMsg)
        {
            try
            {
                if (DbAction.Instance.GetDbTime())
                {
                    ShowDbConnStatus("Y");
                }
                else
                {
                    ShowDbConnStatus("N");
                    return false;
                }
                if (DbAction.Instance.LoadOpcItems(ref errMsg))
                {
                    ShowExecLog("初始化站台数据库配置成功", string.Empty);
                }
                else
                {
                    ShowExecLog($"初始化站台数据库配置失败,原因{errMsg}", string.Empty);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                ShowExecLog($"[异常]初始化数据库,[原因]{ex.Message}", string.Empty);
                return false;
            }
        }

        /// <summary>
        /// 初始化OPC信息
        /// </summary>
        private bool InitOpc(ref string errMsg)
        {
            try
            {
                if (Tools.Instance.PingNetAddress(McConfig.Instance.LocIp))
                {
                    ShowPlcConnStatus("Y");
                }
                else
                {
                    ShowPlcConnStatus("N");
                    return false;
                }
                if (OpcAction.Instance.ConnectOpc(ref errMsg))
                {
                    ShowExecLog("初始化OPC连接成功", string.Empty);
                }
                else
                {
                    ShowExecLog($"初始化OPC连接失败,原因{errMsg}", string.Empty);
                    return false;
                }
                if (OpcAction.Instance.AddOpcGroup(ref errMsg))
                {
                    ShowExecLog("初始化OPC组成功", string.Empty);
                }
                else
                {
                    ShowExecLog($"初始化OPC组失败,原因{errMsg}", string.Empty);
                    return false;
                }
                if (OpcAction.Instance.AddOpcItem(ref errMsg))
                {
                    ShowExecLog("初始化OPC项成功", string.Empty);
                }
                else
                {
                    ShowExecLog($"初始化OPC项失败,原因{errMsg}", string.Empty);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                ShowExecLog($"[异常]初始化OPC,[原因]{ex.Message}", string.Empty);
                return false;
            }
        }

        #region 界面刷新展示
        /// <summary>
        /// 初始化站台信息
        /// </summary>
        private void InitLocControl()
        {
            foreach (var loc in BizHandle.Instance.locDic.Values.OrderBy(p => p.LocPlcNo))
            {
                var locControl = new LocControl();
                locControl.LocNo = loc.LocNo;
                locControl.LocPlcNo = loc.LocPlcNo;
                locControl.Width = 180;
                locControl.Height = 180;
                locControl.Margin = new Thickness(3);
                locControl.Click += LocControl_Click;
                this.locControlDic.Add(loc.LocNo, locControl);
                this.GridLocList.Children.Add(locControl);
            }
            ShowTaskCmd();
        }

        /// <summary>
        /// 显示界面信息
        /// </summary>
        public void ShowInfo(object sender, AppDataEventArgs e)
        {
            var appData = e.AppData;
            var msg = appData.StringInfo;
            var locNo = appData.LocNo;
            var infoType = appData.InfoType;
            FormShow(msg, locNo, infoType);
        }

        /// <summary>
        /// 单机站台状态控件事件
        /// </summary>
        private void LocControl_Click(string locNo)
        {
            if (locNo.Equals(LimitLocNo))
            {
                return;
            }
            var loc = BizHandle.Instance.locDic[locNo];
            this.gbExecLog.Header = $"{loc.LocPlcNo}运行日志";
            this.gbTaskCmd.Header = $"{loc.LocPlcNo}指令信息";
            this.txtLocRecord.Text = loc.ExecLog;
            LimitLocNo = locNo;
            ShowTaskCmd();
        }

        private void FormShow(string msg, string locNo, InfoType infoType)
        {
            this.Dispatcher.Invoke(() =>
            {
                switch (infoType)
                {
                    case InfoType.dbConn:
                        ShowDbConnStatus(msg);
                        break;
                    case InfoType.plcConn:
                        ShowPlcConnStatus(msg);
                        break;
                    case InfoType.logInfo:
                        ShowExecLog(msg, locNo);
                        break;
                    case InfoType.locStatus:
                        ShowLocStatus(locNo);
                        break;
                    case InfoType.taskCmd:
                        ShowTaskCmd();
                        ShowExecLog(msg, locNo);
                        break;
                }
            });
        }

        private void ShowPlcConnStatus(string msg)
        {
            if (msg.Equals("Y"))
            {
                this.recPlcConnStatus.Fill = CustomSolidBrush.Green;
            }
            else
            {
                this.recPlcConnStatus.Fill = CustomSolidBrush.Red;
            }
        }

        private void ShowDbConnStatus(string msg)
        {
            if (msg.Equals("Y"))
            {
                this.recDbConnStatus.Fill = CustomSolidBrush.Green;
            }
            else
            {
                this.recDbConnStatus.Fill = CustomSolidBrush.Red;
            }
        }

        private void ShowExecLog(string msg, string locNo)
        {
            if (txtLocRecord.Text.Length > 10000)
            {
                this.txtLocRecord.Clear();
            }
            //如果日志不归属于任何站台，直接输出即可
            if (string.IsNullOrEmpty(locNo))
            {
                this.txtLocRecord.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + msg + Environment.NewLine);
                return;
            }
            if (msg.Equals(BizHandle.Instance.locDic[locNo].LastExecLog))
            {
                return;
            }
            BizHandle.Instance.locDic[locNo].ExecLog = msg;
            //如果未指定哪个站台,直接输出日志
            if (string.IsNullOrEmpty(LimitLocNo))
            {
                this.txtLocRecord.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + msg + Environment.NewLine);
                return;
            }
            //如果指定站台，则只显示指定站台日志
            if (locNo.Equals(LimitLocNo))
            {
                this.txtLocRecord.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + msg + Environment.NewLine);
            }
        }

        /// <summary>
        /// 站台状态刷新
        /// </summary>
        private void ShowLocStatus(string locNo)
        {
            var loc = BizHandle.Instance.locDic[locNo];
            locControlDic[locNo].LocType = loc.LocTypeDesc;
            locControlDic[locNo].ProductWeight = loc.plcStatus.RealWeight.ToString();
            locControlDic[locNo].StandardWeight = loc.plcStatus.StandardWeight;
            locControlDic[locNo].ProductGuid = loc.plcStatus.ProductGuid;
            locControlDic[locNo].ErrWeight = loc.plcStatus.AllowErrRange;
            locControlDic[locNo].SetAuto(loc.plcStatus.StatusAuto);
            locControlDic[locNo].SetFault(loc.plcStatus.StatusFault);
            locControlDic[locNo].SetLoading(loc.plcStatus.StatusLoad);
            locControlDic[locNo].SetRequest(loc.plcStatus.StatusRequestTask);
            locControlDic[locNo].SetFree(loc.plcStatus.StatusFreeAndPut);
            locControlDic[locNo].SetToLoad(loc.plcStatus.StatusBusyAndTake);
        }

        /// <summary>
        /// 指令信息刷新
        /// </summary>
        private void ShowTaskCmd()
        {
            var errMsg = string.Empty;
            var infoList = new List<DownLoadInfo>();
            if (string.IsNullOrEmpty(LimitLocNo))
            {
                foreach (var info in BizHandle.Instance.downInfoDic.Values)
                {
                    infoList.Add(info);
                }
            }
            else
            {
                infoList.Add(BizHandle.Instance.downInfoDic[LimitLocNo]);
            }
            this.dgv.ItemsSource = infoList;
        }

        /// <summary>
        /// PLC连接状态监控
        /// </summary>
        private void ConnStatus()
        {
            while (true)
            {
                if (Tools.Instance.PingNetAddress(McConfig.Instance.LocIp))
                {
                    ShowFormData.Instance.ShowFormInfo(new ShowInfoData("Y", string.Empty, InfoType.plcConn));
                }
                else
                {
                    //记录断连时间
                    ShowFormData.Instance.ShowFormInfo(new ShowInfoData("N", string.Empty, InfoType.plcConn));
                }
                Thread.Sleep(5000);
            }
        }
        #endregion

        /// <summary>
        /// 执行业务
        /// </summary>
        private void Run()
        {
            while (true)
            {
                //数据库连接验证
                if (DbAction.Instance.GetDbTime())
                {
                    ShowFormData.Instance.ShowFormInfo(new ShowInfoData("Y", string.Empty, InfoType.dbConn));
                }
                else
                {
                    ShowFormData.Instance.ShowFormInfo(new ShowInfoData("N", string.Empty, InfoType.dbConn));
                    continue;
                }
                //根据业务步骤执行相关处理
                BizHandle.Instance.BizListen();

                Thread.Sleep(1000);
            }
        }
    }
}
