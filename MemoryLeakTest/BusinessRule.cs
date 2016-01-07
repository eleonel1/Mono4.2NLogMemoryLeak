using System;
using NLog;
using ServiceStack.CacheAccess;

namespace MemoryLeakTest
{
	public class BusinessRule:IBusinessRule
	{
		public ICacheClient CacheClient { get; set; }
		public Logger Logger { get; set; }
		public int DoNothing(int parameter)
		{
			Console.WriteLine(parameter);
			CacheClient.Set("MemoryLeakTest_"+parameter, parameter);
			Logger.Debug("MemoryLeakTest: {0}",parameter);
            return parameter;
		}
	}
}