using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using IFactory;

namespace TcpProxy
{
    public class AsyncTcp : IAsyncTcp
    {
        #region 参数
        private IPAddress localAdress;
        private const int port = 52888;
        private TcpListener tcpListener;
        private TcpClient tcpClient;
        private NetworkStream networkStream;
        private BinaryReader br;
        private BinaryWriter bw;
        private int sendCount = 1;
        private int receiveCount = 10;
        private string EndpointIP = "127.0.0.1";
        private int Port = 51888;
        private ConnectStates isConnect = ConnectStates.Disconnected;
        private object lockobj = new object();
        private bool IsSer = false;

        private delegate void SendMessageDelegate(string sendMessage);
        private SendMessageDelegate sendMessageDelegate;

        private delegate void ReceiveMessageDelegate(out string receiveMessage);
        ReceiveMessageDelegate receiveMessageDelegate;

        AsyncCallback acceptcallback;
        AsyncCallback requestcallback;
        #endregion
        

        #region 服务端等待连接
        /// <summary>
        /// 等待连接
        /// </summary>
        /// <returns>是否连接成功</returns>
        public bool AcceptClientConnect()
        {
            DateTime nowtime = DateTime.Now;
            while (nowtime.AddSeconds(1) > DateTime.Now) { }
            acceptcallback = new AsyncCallback(AcceptClientCallBack);
            IAsyncResult result = tcpListener.BeginAcceptSocket(acceptcallback, tcpListener);
            while (result.IsCompleted == false)
            {
                Thread.Sleep(30);
            }
            while (isConnect == ConnectStates.Disconnected)
            {
                Thread.Sleep(1000);
            }
            if (isConnect == ConnectStates.Connected)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }

        private void AcceptClientCallBack(IAsyncResult iar)
        {
            try
            {
                tcpListener = (TcpListener)iar.AsyncState;
                tcpClient = tcpListener.EndAcceptTcpClient(iar);
                if (tcpClient != null)
                {
                    networkStream = tcpClient.GetStream();
                    br = new BinaryReader(networkStream);
                    bw = new BinaryWriter(networkStream);
                    lock (lockobj)
                    {
                        isConnect = ConnectStates.Connected;
                    }
                }
                else
                {
                    isConnect = ConnectStates.WaitConnect;
                }
            }
            catch
            {
                lock (lockobj)
                {
                    isConnect = ConnectStates.Failed;
                }
            }
        }
        #endregion

        #region 构造
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="_ip">ip地址</param>
        /// <param name="_port">端口号</param>
        /// <param name="_isSer">是否是服务端</param>
        public  AsyncTcp(string _ip, int _port,bool _isSer)
        {
            EndpointIP = _ip;
            Port = _port;
            IPAddress[] listenIp = Dns.GetHostAddresses(EndpointIP);
            localAdress = listenIp[0];
            IsSer = _isSer;
            if (_isSer)
            {
                tcpListener = new TcpListener(localAdress, Port);
                tcpListener.Start();
                //AcceptClientConnect();
            }
        }
        #endregion

        #region 连接到服务端
        public bool ConnecttoServer()
        {
            try
            {
                if (IsSer || (tcpClient != null && isConnect == ConnectStates.Connected))
                    return true ;
                requestcallback = new AsyncCallback(RequestCallBack);
                tcpClient = new TcpClient(AddressFamily.InterNetwork);
                IAsyncResult result = tcpClient.BeginConnect(IPAddress.Parse(EndpointIP),
                    Port, requestcallback, tcpClient);
                while (result.IsCompleted == false)
                {
                    Thread.Sleep(30);
                }
                if (isConnect == ConnectStates.Closed)
                {
                    return false;
                }
                while (isConnect != ConnectStates.Connected)
                {
                    Thread.Sleep(2000);
                    if (isConnect == ConnectStates.Failed)
                    {
                        tcpClient = null;
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void RequestCallBack(IAsyncResult iar)
        {
            try
            {
                tcpClient = (TcpClient)iar.AsyncState;
                tcpClient.EndConnect(iar);

                DateTime nowtime = DateTime.Now;
                while (nowtime.AddSeconds(1) > DateTime.Now) { }
                if (tcpClient != null)
                {
                    networkStream = tcpClient.GetStream();
                    br = new BinaryReader(networkStream);
                    bw = new BinaryWriter(networkStream);
                    lock (lockobj)
                    {
                        isConnect = ConnectStates.Connected;
                    }
                }
            }
            catch
            {
                lock (lockobj)
                {
                    isConnect = ConnectStates.Failed;
                }
            }
        }
        #endregion
        #region 发送数据
        public bool AsyncSendMsg(string sendMessage)
        {
            try
            {
                if (isConnect != ConnectStates.Connected)
                    return false;
                sendMessageDelegate = new SendMessageDelegate(AsyncSendMsgWrite);
                IAsyncResult result = sendMessageDelegate.BeginInvoke(sendMessage, null, null);
                while (result.IsCompleted == false)
                {
                    Thread.Sleep(30);
                }
                sendMessageDelegate.EndInvoke(result);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void AsyncSendMsgWrite(string sendMessage)
        {
            try
            {
                bw.Write(sendMessage);
                DateTime now = DateTime.Now;
                while (now.AddSeconds(2) > DateTime.Now) { }
                bw.Flush();
            }
            catch
            {
                DisposeResource();
            }
        }

        #endregion

        #region 接收数据
        public bool AsyncRcvMsg(out string receiveMessage)
        {
            try
            {
                if (isConnect != ConnectStates.Connected)
                {
                    receiveMessage = null;
                    return false;
                }

                receiveMessageDelegate = new ReceiveMessageDelegate(AsyncRcvMsgRead);
                IAsyncResult result = receiveMessageDelegate.BeginInvoke(out receiveMessage, null, null);
                while (result.IsCompleted == false)
                {
                    Thread.Sleep(500);
                }
                receiveMessageDelegate.EndInvoke(out receiveMessage, result);
                return true;
            }
            catch
            {
                receiveMessage = string.Empty;
                return false;
            }
        }

        private void AsyncRcvMsgRead(out string receiveMessage)
        {
            receiveMessage = null;
            if (isConnect != ConnectStates.Connected)
                return;
            try
            {
                receiveMessage = br.ReadString();
            }
            catch
            {
                DisposeResource();
            }
        }

        #endregion

        #region 释放资源
        private void DisposeResource()
        {
            if (br != null)
            {
                br.Close();
            }
            if (bw != null)
            {
                bw.Close();
            }
            if (tcpClient != null)
            {
                tcpClient.Close();
            }
            isConnect = ConnectStates.Closed;
        }

        #endregion
        private void SetConnectStates()
        {
 
        }
    }
    /// <summary>
    /// 连接状态
    /// </summary>
    enum ConnectStates
    {
        Connected,//已连接
        Disconnected,//未连接
        WaitConnect,//等待连接
        Failed,//连接失败
        Closed//关闭
    }
}
