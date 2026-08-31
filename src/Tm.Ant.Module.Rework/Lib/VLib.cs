using Modbus.Device; 
using System.Diagnostics; 
using System.Net.Sockets; 

namespace VanguardLibrary;

public class VLib
{
    public VLib()
    {
        Thread t1 = new Thread(AutoConnectThread);
        t1.IsBackground = true;
        t1.Start();
        StartWatch();
    }

    public bool IsConnected => mIsOpen;
    public Action<bool> ConnectChanged { get; set; }
    #region Methoed
    /// <summary>
    /// 连接设备，超时时间内连接成功则返回True，否则返回False
    /// </summary>
    /// <param name="ip">Ip地址</param>
    /// <param name="port">端口号</param>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    /// <returns></returns>
    public Task<bool> ConnectAsync(string ip,int port,int timeoutMs=1000)
    {
        deviceIp = ip;
        devicePort = port;
        semaphoreSlim.Release();
        return Task.Run(() =>
        {
            connecting = true;
            Stopwatch sw = Stopwatch.StartNew();
            while (timeoutMs<sw.ElapsedMilliseconds)
            {
                if (client != null)
                    return true;
                Thread.Sleep(15);
            }
            return false;
        });
    }

    /// <summary>
    /// 断开连接，并关闭自动重连
    /// </summary>
    /// <returns></returns>
    public Task DisconnectAsync()
    {
        return Task.Run(()=>{
            lock (this)
            {
                connecting = false;
                mIsOpen = false;
                if (client != null)
                {
                    client.Dispose();
                    mb.Dispose();
                    client = null;
                    mb = null;
                }
            }
        });  
    }

    void ConnectedCallback(IAsyncResult ar)
    {
        try
        {
            lock (this)
            {
                var _c = (TcpClient)ar.AsyncState;
                if(_c.Client?.Connected == true)
                {
                    _c.EndConnect(ar);
                    Console.WriteLine($"{_c.Client.AddressFamily}已连接!");
                    mb = ModbusIpMaster.CreateIp(_c);
                    client = _c;
                    mIsOpen = true;
                    ConnectChanged?.Invoke(mIsOpen);
                }
            }
        }
        catch (Exception)
        {

        }
    }

    void AutoConnectThread()
    {
        while (true)
        {
            if (!connecting)
                semaphoreSlim.Wait();
            else
                Thread.Sleep(500);
            if (client != null)
                continue;
            try
            {
                var _c = new TcpClient();
                var ir = _c.BeginConnect(deviceIp, devicePort,ConnectedCallback,_c);
                if (!ir.AsyncWaitHandle.WaitOne(1000))
                {
                    _c.Close();
                    _c.Dispose();
                }
            }
            catch
            {

            }
            finally
            {

            }
        }
    }

    /// <summary>
    /// 启动电批拧紧
    /// </summary>
    public void StartWork()
    {
        if (!mIsOpen) return;
        StopWork();
        lock (this)
        {
            mb.WriteMultipleRegisters(1, 0xc4, new ushort[] { 0x01 });
            mb.WriteMultipleRegisters(1, 0xc3, new ushort[] { 0x10 });
        }
        mSoftwareStart = true;
    }

    public void StopWork()
    {
        if (!mIsOpen)
            return;
        lock (this)
        {
            mb.WriteMultipleRegisters(1, 0xc3, new ushort[] { 0x00 });
            mb.WriteMultipleRegisters(1, 0xc4, new ushort[] { 0x00 });
        }

        mSoftwareStart = false;
    }



    /// <summary>
    /// 监控繁忙信号
    /// </summary>
    private void StartWatch()
    {
        Thread t = new Thread((ThreadStart)delegate
        {
            bool _b = false;
            ushort v = 0;
            while (true)
            {
                Thread.Sleep(10);
                try
                {
                    lock (this)
                    {
                        if (!mIsOpen)
                            continue;
                        v = mb.ReadHoldingRegisters(1, 0xc2, 1)[0];
                        _b = (v & 0b10) == 0b10;
                        if (_b != IsBusy)
                        {
                            IsBusy = _b;
                            if (_b == false)
                            {
                                if(mSoftwareStart)
                                  StopWork();//如果是由软件启动的工作，则发送停止信号
                                ReadReuslt();//读取扭力结果
                                ReadPlotData();
                                ChatDatasChanged?.Invoke(Points);
                            }
                            else if (_b)
                            {
                                ReadLen = 0; //清空长度
                                CurAngle = 0;//当前角度清空
                                Points.Clear();
                                //读取
                                var angle = mb.ReadHoldingRegisters(1, 0x1a, 1);
                                WatchAngle = 1;
                            }
                            BusyChanged(_b);
                        }

                        if (_b)
                        {
                            ReadPlotData();
                        }
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("连接断开!");
                    ConnectChanged?.Invoke(false);
                    lock (this)
                    {
                        client?.Dispose();
                        mb?.Dispose();
                        client = null;
                        mb = null;
                        mIsOpen = false;
                    }
                }
            }
        });
        t.IsBackground = true;
        t.Priority = ThreadPriority.Highest;
        t.Start();
    }

    void ReadReuslt()
    {
        for (int i = 0; i < 10; i++)
        {
            var canread = mb.ReadInputRegisters(1, 0x30, 1)[0];
            if (canread == 1)
            {
                var v = mb.ReadInputRegisters(1, 0x31, 6);
                ResultChanged?.Invoke(new VResult()
                {
                    ProgNo  = v[0],
                    Turns   = v[1]/10.0,
                    Torque  = v[2]/10.0,
                    Time    = v[3]/1000.0,
                    Result  = v[4],
                    Reason  = v[5]
                });
                break;
            }
            else
            {
                Thread.Sleep(30);
            }
        }
    }
    public void ReadPlotData()
    {
        //读取波形数据
        var len = mb.ReadInputRegisters(1, 0x3f, 1)[0];
        //var val = master.ReadInputRegisters(1, 0x15, 3);
        ushort signalLen = 120;
        if ((len - ReadLen > signalLen) || IsBusy == false)
        {
            //如果数据长度超过128个数据，或者是已经工作完成，则读取波形

            int count = (len - ReadLen) / signalLen;
            int remain = (len - ReadLen) % signalLen;
            for (int i = 0; i < count; i++)
            {
                var datas = mb.ReadInputRegisters(1, (ushort)(0x40 + ReadLen), signalLen);
                ReadLen += signalLen;
                foreach (var item in datas)
                {
                    Points.Add(new ChatPoint()
                    {
                        Turns = CurAngle += WatchAngle,
                        Torque = (float)(item / 10.0)
                    });
                }
            }
            if (IsBusy == false)
            {
                var datas = mb.ReadInputRegisters(1, (ushort)(0x40 + ReadLen), (ushort)remain);
                ReadLen += (ushort)remain;
                foreach (var item in datas)
                {
                    Points.Add(new ChatPoint()
                    {
                        Turns = CurAngle += WatchAngle,
                        Torque = (float)(item / 10.0)
                    });
                }
            }
        }
    }


    #endregion

    #region 字段
    string deviceIp;
    int devicePort;
    TcpClient client;
    Modbus.Device.ModbusMaster mb;
    bool connecting;
    SemaphoreSlim semaphoreSlim = new SemaphoreSlim(0, 1);
    bool mIsOpen = false;
    bool mSoftwareStart = false;
    #endregion

    #region 属性与委托
    public bool IsBusy { get; set; }
    /// <summary>
    /// 已经读取的波形数量
    /// </summary>
    public ushort ReadLen { get; set; }
    /// <summary>
    /// 存储的波形数据内容
    /// </summary>
    public List<ChatPoint> Points { get; set; } = new List<ChatPoint>();
    /// <summary>
    /// 监控角度
    /// </summary>
    public int WatchAngle { get; set; }
    public int CurAngle { get; set; }
    
    public delegate void BusyChangedDelegate(bool IsBusy);
    public BusyChangedDelegate BusyChanged { get; set; }

    public delegate void ResultChangedDelegate(VResult result);
    public ResultChangedDelegate ResultChanged { get; set; }

    public delegate void ChatDatasChangedDelegate(List<ChatPoint> points);
    public ChatDatasChangedDelegate ChatDatasChanged { get; set; }
    
    #endregion
}
public class ChatPoint
{
    public double Turns;
    public double Torque;
}

public class VResult
{
    /// <summary>
    /// 程序号
    /// </summary>
    public ushort ProgNo;
    /// <summary>
    /// 圈数
    /// </summary>
    public double Turns;
    /// <summary>
    /// 扭矩
    /// </summary>
    public double Torque;
    /// <summary>
    /// 时间
    /// </summary>
    public double Time;
    /// <summary>
    /// 结果
    /// </summary>
    public double Result;
    /// <summary>
    /// 原因
    /// </summary>
    public double Reason;

    public override string ToString()
    {
        string content = $"\nProgNo:{ProgNo},Turns:{Turns},Torque:{Torque},Time:{Time},Result:{Result},Reason:{Reason}";
        return content;
    }
}
