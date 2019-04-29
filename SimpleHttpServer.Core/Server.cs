using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleHttpServer.Core
{
    public static class Server
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static int _maxSimultaneousConnections = 20;

        public static int MaxSimultaneousConnections
        {
            get => _maxSimultaneousConnections;
            set
            {
                if (_maxSimultaneousConnections == value) return;

                _maxSimultaneousConnections = value;
                ConnectionsSemaphore = new Semaphore(MaxSimultaneousConnections, MaxSimultaneousConnections);
            }
        }

        private static Semaphore ConnectionsSemaphore =
            new Semaphore(MaxSimultaneousConnections, MaxSimultaneousConnections);

        /// <summary>
        /// Starts the server.
        /// </summary>
        public static async void Start()
        {
            IList<IPAddress> localhostIps = await GetLocalHostIpsAsync();
            HttpListener httpListener = InitializeHttpListenerForIps(localhostIps, true);
            StartInternal(httpListener);
        }

        /// <summary>
        /// Start background waiting for connections.
        /// </summary>
        private static void StartInternal(HttpListener httpListener)
        {
            httpListener.Start();
            Task.Run(() => RunServer(httpListener));
        }

        /// <summary>
        /// Wait for connections up to MaxSimultaneousConnections value.
        /// </summary>
        private static void RunServer(HttpListener httpListener)
        {
            while (true)
            {
                ConnectionsSemaphore.WaitOne();
                StartConnectionListener(httpListener);
            }
        }

        /// <summary>
        /// Awaits for and handles a connection.
        /// </summary>
        private static async void StartConnectionListener(HttpListener httpListener)
        {
            // Wait for a connection.
            HttpListenerContext context = await httpListener.GetContextAsync();

            // Release the semaphore so another listener can be started up.
            ConnectionsSemaphore.Release();

            // Handle the request.
            LogRequest(context.Request);

            const string response = "Hello World";
            byte[] encoded = Encoding.UTF8.GetBytes(response);
            context.Response.ContentLength64 = encoded.Length;
            context.Response.OutputStream.Write(encoded, 0, encoded.Length);
            context.Response.OutputStream.Close();
        }

        #region Helpers

        /// <summary>
        /// Gets a list of IP addresses for the running machine.
        /// </summary>
        private static async Task<IList<IPAddress>> GetLocalHostIpsAsync()
        {
            IPHostEntry host = await Dns.GetHostEntryAsync(Dns.GetHostName());
            return host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToList();
        }

        /// <summary>
        /// Returns an HttpListener that is listening on the given IP addresses.
        /// </summary>
        private static HttpListener InitializeHttpListenerForIps(IList<IPAddress> ipAddresses, bool includeLocalhost)
        {
            HttpListener httpListener = new HttpListener();

            if (includeLocalhost)
            {
                httpListener.Prefixes.Add("http://localhost/");
            }

            foreach (IPAddress ipAddress in ipAddresses)
            {
                Console.WriteLine($"Listening on http://{ipAddress}/");
                httpListener.Prefixes.Add($"http://{ipAddress}/");
            }

            return httpListener;
        }

        #endregion

        #region Logging

        private static void LogRequest(HttpListenerRequest httpListenerRequest)
        {
            // Note: Not using concatentation / interpolation as NLog
            // defers the formatting, reducing overhead.
            Logger.Info("Request: {0} {1} /{2}",
                httpListenerRequest.RemoteEndPoint,
                httpListenerRequest.HttpMethod,
                httpListenerRequest.Url.AbsoluteUri);
        }

        #endregion
    }
}
