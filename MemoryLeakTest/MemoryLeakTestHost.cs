using System.Reflection;
using Funq;
using NLog;
using ServiceStack.CacheAccess;
using ServiceStack.Common;
using ServiceStack.Common.Support;
using ServiceStack.Redis;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace MemoryLeakTest
{
	public class MemoryLeakTestHost: AppHostHttpListenerSmartThreadPool
	{
		public MemoryLeakTestHost(string serviceName, params Assembly[] assembliesWithServices) : base(serviceName, assembliesWithServices)
		{
		}

		public MemoryLeakTestHost(string serviceName, int poolSize, params Assembly[] assembliesWithServices) : base(serviceName, poolSize, assembliesWithServices)
		{
		}

		public MemoryLeakTestHost(string serviceName, string handlerPath, params Assembly[] assembliesWithServices) : base(serviceName, handlerPath, assembliesWithServices)
		{
		}

		public override void Configure(Container container)
		{
			SetConfig(new EndpointHostConfig()
			{
				EnableFeatures = Feature.All.Remove(Feature.Metadata),
				DefaultRedirectPath = "/default",
				GlobalResponseHeaders = { { "X-Powered-by", "Test" }, { "Server", "Test" } },
				AllowJsonpRequests = true,
			});
			StreamExtensions.GZipProvider = new NetGZipProvider();
			StreamExtensions.DeflateProvider = new NetDeflateProvider();
			container.Register<ICacheClient>(x => new PooledRedisClientManager("10.10.3.166:6379").GetCacheClient());
			container.Register<Logger>(x => LogManager.GetLogger("MemoryLeakTestLog"));
			container.RegisterAutoWiredAs<BusinessRule, IBusinessRule>().ReusedWithin(ReuseScope.None);
		}
	}
}