using Microsoft.AspNetCore.Components.Forms;

namespace TreeClimberCore.Stubs
{
	public class BrowserFileStub : IBrowserFile
	{
		public string Name { get; }

		public BrowserFileStub(string name)
		{
			Name = name;
		}

		public DateTimeOffset LastModified => throw new NotImplementedException();

		public long Size => throw new NotImplementedException();

		public string ContentType => throw new NotImplementedException();

		public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}
	}
}
