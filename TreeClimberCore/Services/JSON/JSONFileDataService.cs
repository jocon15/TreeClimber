using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json.Linq;

namespace TreeClimberCore.Services.JSON
{
	public class JSONFileDataService
	{
		private static readonly List<string> _jsonSymbols = new List<string> { "[", "]", "{", "}", "\"" };

		// FIXME: should this service store a file or file contents

		protected IBrowserFile _file;

		protected JToken? _fileContents;

		protected int _changeCount = 0;

		/// <summary>
		/// Mutator for the JSON data
		/// </summary>
		/// <param name="data"></param>
		public async Task SetData(IBrowserFile file)
		{
			_changeCount = 0;
			_file = file;
			_fileContents = await ConvertFileContentsToJObjectAsync(file);
		}

		public int GetChangeCount() => _changeCount;

		public JToken? GetFileContents() => _fileContents;

		public IBrowserFile GetFile()
		{
			// suggested file size limit for memory stream is < 250MB
			// FIXME: https://stackoverflow.com/questions/52748183/serialize-into-json-and-return-as-a-stream
			// use this to convert JObject to memory stream
			// FIXME: https://learn.microsoft.com/en-us/aspnet/core/blazor/file-downloads?view=aspnetcore-9.0
			// use this to download a file from a memory stream
			throw new NotImplementedException();
		}

		

		protected static async Task<JToken> ConvertFileContentsToJObjectAsync(IBrowserFile file)
		{
			string fileContents;
			using (var stream = file.OpenReadStream())
			using (var reader = new StreamReader(stream))
			{
				fileContents = await reader.ReadToEndAsync();
			}

			return JToken.Parse(fileContents);
		}

		/// <summary>
		/// Removes unwanted characters from a string. Note, this function does not act as 
		/// a validator. This function is merely designed to be used to clean strings that
		/// may contain characters such as tabs and new lines which could be present after
		/// copy/pasting.
		/// 
		/// Note: It seems that the html text input automatically converts \ to \\. When 
		/// the debugger hits this function where "exampl\re was typed, the dirtyString 
		/// will be "exampl\\re". The added backslash correctly avoids the escape character
		/// backslash and thus keeps the string as a literal. This behavior makes this 
		/// function useless in this case, but I want to keep it as there are other use
		/// cases where the string may not originate from an html textbox and thus may contain
		/// excaped characters.
		/// </summary>
		/// <param name="dirtyString">The string to cleanThe path to be used</param>
		/// <returns>The cleaned string</returns>
		public static string CleanString(string dirtyString) => dirtyString.Replace("\n", "").Replace("\r", "").Replace("\t", "");
	}
}
