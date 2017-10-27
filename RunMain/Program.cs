using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpProxy;
using System.Threading;

namespace RunMain
{
    class Program
    {
        static bool isSer = false;//是否是服务端
        static object lockobj = new object();//锁对象
        static bool isReceive = true;//是否接收到数据，可以对接收的数据处理，并给出相应回应
        static void Main(string[] args)
        {
            
            Console.WriteLine("Please input S/C");
            string str = Console.ReadLine();
           
            if (str.ToUpper() == "S")
            {
                isSer = true;
            }

            ////发送与接收分别为两个线程，两个tcpclient，两者可同时执行，且不冲突
            //若只用一个tcpclient，则不能同时执行发与收
            //TODO  地址端口统一问题
            //TODO  将状态传出，以状态标识显示
            Thread ts = new Thread(() => Receive(isSer));
            ts.IsBackground = true;
            Thread tw = new Thread(() => Send(isSer));
            tw.IsBackground = true;
            ts.Start();
            tw.Start();
            while (true)
            {
                Thread.Sleep(1000);
            }

        }

        /// <summary>
        /// 接收
        /// </summary>
        /// <param name="isSer">是否是服务端</param>
        public static void Receive(bool isSer)
        {
            TcpProxy.AsyncTcp tcpCilent = new AsyncTcp("127.0.0.1", isSer ? 51888 :  52888,isSer);

            Connect(tcpCilent ,"127.0.0.1"+(isSer ? "51888" : "52888"),5000,100);
            
            while (true)
            {
                bool isok = false;
                string receiveMessage;
                isok = tcpCilent.AsyncRcvMsg(out receiveMessage);
                //Console.WriteLine(isok.ToString());
                if (receiveMessage == null)
                {
                    lock (lockobj)
                    {
                        isReceive = false;
                    }
                }
                else
                {
                    Console.WriteLine("收：" + receiveMessage);
                    lock (lockobj)
                    {
                        isReceive = true;
                    }
                }
            }
        }

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="isSer">是否是服务端</param>
        public static void Send(bool isSer)
        {
            TcpProxy.AsyncTcp tcpCilent = new AsyncTcp("127.0.0.1", isSer ? 52888 : 51888, isSer);

            Connect(tcpCilent, "127.0.0.1" + (isSer ? "52888" : "51888"),5000,100);
            bool isok = true;
            while (true)
            {
                if (isReceive)
                {
                    string str = Console.ReadLine();
                    isok = tcpCilent.AsyncSendMsg(str);
                }
                else
                {
                    Thread.Sleep(3000);
                }

            }
        }

        /// <summary>
        /// 服务端等待连接
        /// 客户端连接
        /// </summary>
        /// <param name="tcpCilent"></param>
        /// <param name="connectStr">连接成功提示字符串</param>
        private static void Connect(TcpProxy.AsyncTcp tcpCilent,string connectStr,int interval,int maxTimes)
        {
            int counts = 0;
            if (isSer)
            {
                while (!tcpCilent.AcceptClientConnect())
                {
                    Thread.Sleep(interval);
                    if (++counts == maxTimes)
                    {
                        Console.WriteLine(connectStr + " 连接超时");
                        return;
                    }
                }
                Console.WriteLine(connectStr + " 收到一个连接");
            }
            else
            {
                while (!tcpCilent.ConnecttoServer())
                {
                    Thread.Sleep(interval);
                    if (++counts == maxTimes)
                    {
                        Console.WriteLine(connectStr + " 连接超时");
                        return;
                    }
                }
                Console.WriteLine(connectStr + " 连接到服务器");
            }
        }
    }
}
