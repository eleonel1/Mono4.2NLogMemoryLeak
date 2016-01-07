using System;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using Amib.Threading;
using ServiceStack.Common.Web;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace MemoryLeakTest
{
    public abstract class AppHostHttpListenerSmartThreadPool : AppHostHttpListenerBase {
        private readonly AutoResetEvent _listenForNextRequest = new AutoResetEvent(false);
        private readonly SmartThreadPool _threadPoolManager;

        private const int ThreadsPerProcessor = 2;

        private static int CalculatePoolSize() {
            return Environment.ProcessorCount* ThreadsPerProcessor;
        }
        
        private const int IdleTimeout = 300;

        protected AppHostHttpListenerSmartThreadPool(string serviceName, params Assembly[] assembliesWithServices)
            : base(serviceName, assembliesWithServices)
        { _threadPoolManager = new SmartThreadPool(IdleTimeout); }

        protected AppHostHttpListenerSmartThreadPool(string serviceName, int poolSize, params Assembly[] assembliesWithServices)
            : base(serviceName, assembliesWithServices)
        { _threadPoolManager = new SmartThreadPool(IdleTimeout, poolSize); }

        protected AppHostHttpListenerSmartThreadPool(string serviceName, string handlerPath, params Assembly[] assembliesWithServices)
            : this(serviceName, handlerPath, CalculatePoolSize(), assembliesWithServices) { }

        private AppHostHttpListenerSmartThreadPool(string serviceName, string handlerPath, int poolSize, params Assembly[] assembliesWithServices)
            : base(serviceName, handlerPath, assembliesWithServices)
        { _threadPoolManager = new SmartThreadPool(IdleTimeout, poolSize); }

        private bool _disposed;

        protected override void Dispose(bool disposing) {
            if (_disposed) return;

            lock (this) {
                if (_disposed) return;

                if (disposing)
                    _threadPoolManager.Dispose();

                // new shared cleanup logic
                _disposed = true;

                base.Dispose(disposing);
            }
        }

        private bool IsListening => IsStarted && Listener != null && Listener.IsListening;

        // Loop here to begin processing of new requests.
        private void Listen(object state) {
            while (IsListening) {
                if (Listener == null) return;

                try {
                    Listener.BeginGetContext(ListenerCallback, Listener);
                    _listenForNextRequest.WaitOne();
                } catch (Exception ex) {
                    Console.Error.WriteLine($"{DateTime.UtcNow} [SMART THREAD POOL] Listen: | {ex.Message} | {ex.StackTrace} | {ex.InnerException}");
                    return;
                }
                if (Listener == null) return;
            }
        }

        // Handle the processing of a request in here.
        private void ListenerCallback(IAsyncResult asyncResult) {
            var listener = asyncResult.AsyncState as HttpListener;
            HttpListenerContext context;

            if (listener == null) return;
            var isListening = listener.IsListening;

            try {
                if (!isListening) {
                    Console.WriteLine($"{DateTime.UtcNow} [SMART THREAD POOL] Ignoring ListenerCallback() as HttpListener is no longer listening");
                    return;
                }
                // The EndGetContext() method, as with all Begin/End asynchronous methods in the .NET Framework,
                // blocks until there is a request to be processed or some type of data is available.
                context = listener.EndGetContext(asyncResult);
            } catch (Exception ex) {
                // You will get an exception when httpListener.Stop() is called
                // because there will be a thread stopped waiting on the .EndGetContext()
                // method, and again, that is just the way most Begin/End asynchronous
                // methods of the .NET Framework work.
                Console.Error.WriteLine($"{DateTime.UtcNow} [SMART THREAD POOL] ListenerCallback: {isListening} | {ex.Message} | {ex.StackTrace} | {ex.InnerException}");
                return;
            } finally {
                // Once we know we have a request (or exception), we signal the other thread
                // so that it calls the BeginGetContext() (or possibly exits if we're not
                // listening any more) method to start handling the next incoming request
                // while we continue to process this request on a different thread.
                _listenForNextRequest.Set();
            }

            if (Config.DebugMode)
                Console.WriteLine($"{DateTime.UtcNow} [SMART THREAD POOL] {context.Request.UserHostAddress} Request : {context.Request.RawUrl}");

            RaiseReceiveWebRequest(context);

            _threadPoolManager.QueueWorkItem(() => { try { ProcessRequest(context); } catch(Exception e) { ReturnError(e, context); }});
        }

        private void ReturnError(Exception ex, HttpListenerContext context) {
            var error = $"Error this.ProcessRequest(context): [{ex.GetType().Name}]: {ex.Message}";
            Console.Error.WriteLine($"{DateTime.UtcNow} [SMART THREAD POOL] {error}");

            try {
                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("\"ResponseStatus\":{");
                sb.AppendFormat(" \"ErrorCode\":{0},\n", ex.GetType().Name.EncodeJson());
                sb.AppendFormat(" \"Message\":{0},\n", ex.Message.EncodeJson());
                if (Config.DebugMode) sb.AppendFormat(" \"StackTrace\":{0}\n", ex.StackTrace.EncodeJson());
                sb.AppendLine("}");
                sb.AppendLine("}");

                context.Response.StatusCode = 500;
                context.Response.ContentType = ContentType.Json;

                var sbBytes = sb.ToString().ToUtf8Bytes();
                context.Response.OutputStream.Write(sbBytes, 0, sbBytes.Length);
                context.Response.Close();
            } catch (Exception errorEx) {
                error = $"Error this.ProcessRequest(context)(Exception while writing error to the response): [{errorEx.GetType().Name}]: {errorEx.Message}";
                Console.Error.WriteLine($"{DateTime.UtcNow} [SMART THREAD POOL] {error}");
            }
        }
    }
}