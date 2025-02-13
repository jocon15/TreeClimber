using Newtonsoft.Json.Linq;

namespace TreeClimberCore.Services.JSON
{
	public class JSONDataService
	{
		private static readonly List<string> _jsonSymbols = new List<string> { "[", "]", "{", "}", "\"" };

		protected JObject? _jsonObject;

		protected int _changeCount = 0;

		/// <summary>
		/// Mutator for the JSON data
		/// </summary>
		/// <param name="data"></param>
		public void SetData(JObject data)
		{
			_changeCount = 0;
			_jsonObject = data;
		}

		public JObject? GetJObject() => _jsonObject;

		public int GetChangeCount() => _changeCount;
	}
}
