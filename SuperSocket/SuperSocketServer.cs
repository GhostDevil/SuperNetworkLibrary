
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperSocket;
using SuperSocket.Channel;
using SuperSocket.ProtoBase;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SuperNetwork.SuperSocket
{
    /// <summary>
    /// SuperSocket Server Helper
    /// </summary>
    public class SuperSocketServer
    {
        #region 过滤器
        public class BytesPackage
        {
            public byte Key { get; set; }

            public byte[] Datas { get; set; }
        }
        public class BytesPipelineFilter : IPipelineFilter<BytesPackage>
        {
            public IPackageDecoder<BytesPackage> Decoder { get; set; }

            public object Context { get; set; }

            public IPipelineFilter<BytesPackage> NextFilter => this;

            public BytesPackage Filter(ref SequenceReader<byte> reader)
            {
                byte[] bytes = new byte[reader.Length];
                BytesPackage txtPackage = new BytesPackage(); //{ Datas = reader.Sequence.ToArray() };
                int index = 0;
                while (reader.TryRead(out var da))
                {
                    bytes[index] = da;
                    index++;
                }
                txtPackage.Datas = bytes;
                return txtPackage;
            }

            public void Reset() { }
        }
        #endregion

        #region event
        public event Func<IAppSession, Task<bool>> SessionConnected;
        public event Func<IAppSession, CloseEventArgs, Task<bool>> SessionClosed;
        public event Func<IAppSession, BytesPackage, Task<bool>> DataHandler;
        public event Func<IAppSession, PackageHandlingException<BytesPackage>, Task<bool>> ErrorHandler;
        #endregion

        IHost host;
        /// <summary>
        /// 会话集合
        /// </summary>
        public readonly ConcurrentDictionary<string, IAppSession> Sessions = new ConcurrentDictionary<string, IAppSession>();
        public readonly ServerOptions Options;
        public SuperSocketServer(ServerOptions options)
        {
            Options = options;
        }
        public bool CreateServer()
        {
            try
            {
                host = SuperSocketHostBuilder.Create<BytesPackage, BytesPipelineFilter>()
                .ConfigureSuperSocket(options =>
                {

                    options.Name = Options.Name;
                    options.Listeners = Options.Listeners;
                    options.ReceiveBufferSize = Options.ReceiveBufferSize;
                    options.IdleSessionTimeOut = Options.IdleSessionTimeOut;
                    options.In = Options.In;
                    options.Out = Options.Out;
                    options.Logger = Options.Logger;
                    options.MaxPackageLength = Options.MaxPackageLength;
                    options.ReadAsDemand = Options.ReadAsDemand;
                    options.SendBufferSize = Options.SendBufferSize;
                    options.SendTimeout = Options.SendTimeout;
                    options.ReceiveTimeout = Options.ReceiveTimeout;
                    options.Values = Options.Values;
                    options.DefaultTextEncoding = Options.DefaultTextEncoding;
                    options.ClearIdleSessionInterval = Options.ClearIdleSessionInterval;

                })
                .UseClearIdleSession()//自动复用已经闲置或者失去连接的资源
                .UseSessionHandler(OnConnectedAsync, OnClosedAsync)
                .UsePackageHandler(OnPackageAsync)
                .ConfigureErrorHandler(OnConfigureError)
                .UseInProcSessionContainer()
                .ConfigureLogging((hostCtx, loggingBuilder) =>
                {
                    loggingBuilder.AddConsole();
                })
                .Build();

                return true;
            }
            catch { return false; }
        }
        private ValueTask<bool> OnConfigureError(IAppSession session, PackageHandlingException<BytesPackage> package)
        {
            Debug.WriteLine($"{DateTime.Now} {session.RemoteEndPoint} {package.Message} {package.InnerException}");
            return ValueTask.FromResult(true);
        }
        /// <summary>
        /// 数据接收事件
        /// </summary>
        /// <param name="session"></param>
        /// <param name="package"></param>
        /// <returns></returns>
        private async ValueTask OnPackageAsync(IAppSession session, BytesPackage package)
        {

            //await Task.Factory.StartNew(() =>
            //{
            //发送收到的数据
            Debug.WriteLine($"{DateTime.Now} {session.RemoteEndPoint} {Convert.ToHexString(package.Datas)}");

            DataHandler?.Invoke(session, package);
            await ValueTask.FromResult(true);
            //});
        }
        /// <summary>
        /// 会话的连接事件
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private async ValueTask OnConnectedAsync(IAppSession session)
        {

            Debug.WriteLine($"SessionConnected: {session.LastActiveTime} {session.RemoteEndPoint}");
            //await Task.Factory.StartNew(async () =>
            //{
            //    while (!Sessions.ContainsKey(session.SessionID))
            //    {
            //        //添加不成功则重复添加
            //        if (!Sessions.TryAdd(session.SessionID, session))
            //            await Task.Delay(1);
            //    }
            //});
            Sessions.AddOrUpdate(session.SessionID, session, (e, o) => { return session; });
            SessionConnected?.Invoke(session);
            await ValueTask.FromResult(true);

        }
        /// <summary>
        /// 会话的断开事件
        /// </summary>
        /// <param name="session"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private async ValueTask OnClosedAsync(IAppSession session, CloseEventArgs args)
        {
            Debug.WriteLine($"SessionClosed: {session.LastActiveTime} {session.RemoteEndPoint}");
            await Task.Factory.StartNew(async () =>
            {
                while (Sessions.ContainsKey(session.SessionID))
                {
                    //移除不成功则重复移除
                    if (!Sessions.TryRemove(session.SessionID, out _))
                        await Task.Delay(10);
                }
            });
            SessionClosed?.Invoke(session, args);

        }

        /// <summary>
        /// 启动服务
        /// </summary>
        public async void StartAsync() => await host?.StartAsync();

        /// <summary>
        /// 停止服务
        /// </summary>
        /// <returns></returns>
        public async Task<bool> StopAsync()
        {
            bool isSuccess = false;
            foreach (var v in Sessions)
            {
                await v.Value.CloseAsync(CloseReason.ServerShutdown);
            }
            Sessions.Clear();
            if (host != null)
                await host?.StopAsync();
            return isSuccess;
        }

        public async ValueTask SendAsync(string endPoint, byte[] data)
        {
            try
            {
                IAppSession appSession = Sessions.Values.FirstOrDefault(o => o.RemoteEndPoint.ToString() == endPoint && o.State is SessionState.Connected);
                if (appSession != null)
                    await appSession.SendAsync(data);
                else
                    await Task.FromResult(false);
            }
            catch (Exception ex) { Debug.WriteLine(ex); }
        }
        public void Send(string endPoint, byte[] data)
        {
            try
            {
                ValueTask? t = Sessions.Values.FirstOrDefault(o => o.RemoteEndPoint.ToString() == endPoint && o.State is SessionState.Connected)?.SendAsync(data);
                if (t.HasValue)
                {
                    while (!t.Value.IsCompleted)
                    {
                        Task.Delay(5).Wait();
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine(ex); }
        }
    }
}