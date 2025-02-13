

using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json.Linq;

namespace TreeClimberCore.Services.Files
{
	public class FileImporterService
	{
		public static async Task<JObject> ConvertFileContentsToJObjectAsync(IBrowserFile file)
		{
			string fileContents = await ConvertFileContentsToStringAsync(file);

			return JObject.Parse(fileContents);
		}

		protected static async Task<string> ConvertFileContentsToStringAsync(IBrowserFile file)
		{
			string fileContents;
			using (var stream = file.OpenReadStream())
			using (var reader = new StreamReader(stream))
			{
				fileContents = await reader.ReadToEndAsync();
			}
			return fileContents;
		}
	}
}
