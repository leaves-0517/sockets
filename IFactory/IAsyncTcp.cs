using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFactory
{
    public interface IAsyncTcp
    {
        /// <summary>
        /// 等待连接
        /// </summary>
        /// <returns></returns>
        bool AcceptClientConnect();

        bool ConnecttoServer();
        /// <summary>
        /// tcp异步接收数据
        /// </summary>
        /// <param name="sendMessage"></param>
        bool AsyncSendMsg(string sendMessage);
        /// <summary>
        /// tcp异步发送数据
        /// </summary>
        /// <param name="receiveMessage"></param>
        bool AsyncRcvMsg(out string receiveMessage);

    }
}
