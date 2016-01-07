using ServiceStack.ServiceInterface;

namespace MemoryLeakTest
{
	public class MemoryLeakTestService:Service
	{
		public IBusinessRule BusinessRule { get; set; }
		public object Any(MemoryLeakTestRequest request)
		{
			/*var testArray = new char[request.Test];
			for (var i = 0; i < request.Test; i++)
			{
				testArray[i] = 'a';
			}
			Console.WriteLine(testArray);
			return testArray;*/
			return BusinessRule.DoNothing(request.Test);
		}
	}
}