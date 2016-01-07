using ServiceStack.ServiceHost;

namespace MemoryLeakTest
{
	[Route("/MemoryLeakTest")]
	public class MemoryLeakTestRequest
	{
		public int Test { get; set; }
	}
}