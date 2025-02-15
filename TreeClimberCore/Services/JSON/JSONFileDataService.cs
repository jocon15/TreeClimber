using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using TreeClimberCore.Services.Path;
using TreeClimberCore.Util;

namespace TreeClimberCore.Services.JSON
{
	public class JSONFileDataService
	{
		public const string TEMPLATE = "template";

		private static readonly List<string> _jsonSymbols = new List<string> { "[", "]", "{", "}", "\"" };

		protected volatile IBrowserFile? _file;

		protected volatile JToken? _fileContents;

		protected volatile int _changeCount = 0;

		/// <summary>
		/// Mutator for the JSON data
		/// </summary>
		/// <param name="data"></param>
		public async Task SetDataAsync(IBrowserFile file)
		{
			_changeCount = 0;
			_file = file;
			_fileContents = await ConvertFileContentsToJObjectAsync(file);
		}

		public int GetChangeCount() => _changeCount;

		public JToken? GetFileContents()
		{
			return _fileContents;
		}			

		public string GetFileName() => _file.Name;

		public IBrowserFile GetFile()
		{
			// suggested file size limit for memory stream is < 250MB
			// FIXME: https://stackoverflow.com/questions/52748183/serialize-into-json-and-return-as-a-stream
			// use this to convert JObject to memory stream
			// FIXME: https://learn.microsoft.com/en-us/aspnet/core/blazor/file-downloads?view=aspnetcore-9.0
			// use this to download a file from a memory stream
			throw new NotImplementedException();
		}

		public MemoryStream GetFileAsMemoryStream()
		{
			var stream = new MemoryStream();
			var streamWriter = new StreamWriter(stream);
			var jsonWriter = new JsonTextWriter(streamWriter);

			var serializer = new Newtonsoft.Json.JsonSerializer();
			serializer.Formatting = Formatting.Indented;
			serializer.Serialize(jsonWriter, _fileContents);
			jsonWriter.Flush();
			streamWriter.Flush();
			stream.Seek(0, SeekOrigin.Begin);
			return stream;
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
		/// Access data within the JSON data structure
		/// </summary>
		/// <param name="path">JSON path of the desired data. NOT NSFT path</param>
		/// <returns> Tuple
		/// 1. Status code
		/// 2. Message
		/// 3. Contents
		/// </returns>
		public Tuple<int, string, JToken?> AccessData(string path)
		{
			// run pre-processing (using the access version)
			Tuple<int, JToken?, string> result = PreProcessAccess(path);
			if (result.Item1 != ResponseUtil.OK)
			{
				return Tuple.Create(result.Item1, "Unable to copy data!", result.Item2);
			}
			path = result.Item3;

			if (ValidPath(path) == false)
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, "Invalid path!", (JToken?)new JObject());
			}

			string keyPath = BuildNewtonsoftKeyPath(path);
			return Tuple.Create(ResponseUtil.OK, "", _fileContents.SelectToken(keyPath));
		}

		/// <summary>
		/// Edit data in the JSON structure
		/// </summary>
		/// <param name="path">JSON path of the desired data.</param>
		/// <param name="newValue">New value to replace with.</param>
		/// <returns>Tuple of status and message</returns>
		public Tuple<int, string> EditData(string path, string newValue)
		{
			Tuple<int, string, string, string> result = PreProcess(path, newValue);
			if (result.Item1 != ResponseUtil.OK)
			{
				return Tuple.Create(result.Item1, result.Item2);
			}
			path = result.Item3;
			newValue = result.Item4;

			if (ValidPath(path) == false)
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, $"Path: {path} does not exist in the data!");
			}

			JToken existingValue = _fileContents.SelectToken(BuildNewtonsoftKeyPath(path));

			try
			{
				switch (existingValue.Type)
				{
					case JTokenType.String:
						// no conversion necessary because they are already strings
						existingValue.Replace(newValue);
						break;
					case JTokenType.Integer:
						existingValue.Replace(int.Parse(newValue));
						break;
					case JTokenType.Float:
						existingValue.Replace(float.Parse(newValue));
						break;
					case JTokenType.Boolean:
						existingValue.Replace(bool.Parse(newValue));
						break;
					case JTokenType.Date:
						existingValue.Replace(DateTime.Parse(newValue));
						break;
					case JTokenType.Array:
						existingValue.Replace(JArray.Parse(newValue));
						break;
					default:
						return Tuple.Create(ResponseUtil.INTERNAL_ERROR, $"Unrecognized Newtonsoft type {existingValue.Type}");
				}
			}
			catch
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, $"Cannot convert '{newValue}' to {existingValue.Type}!");
			}

			_changeCount++;

			// data has successfully been edited
			return Tuple.Create(ResponseUtil.OK, "");
		}

		/// <summary>
		/// Rename a key in the JSON structure
		/// </summary>
		/// <param name="path">JSON path of the desired data.</param>
		/// <param name="newName">New name to replace the key with.</param>
		/// <returns>Tuple of status and message</returns>
		public Tuple<int, string> RenameKey(string path, string newName)
		{
			Tuple<int, string, string, string> result = PreProcess(path, newName);
			if (result.Item1 != ResponseUtil.OK)
			{
				return Tuple.Create(result.Item1, result.Item2);
			}
			path = result.Item3;
			newName = result.Item4;

			// PrePrecess only checks newName for null, not empty, newName cannot be empty
			if (newName == "")
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, $"New key cannot be empty!");
			}

			if (ValidPath(path) == false)
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, $"Path: {path} does not exist in the data!");
			}

			JToken? existingToken = _fileContents.SelectToken(BuildNewtonsoftKeyPath(path));

			// Note: to avoid triggering a clone of the existing property's value,
			// we need to save a reference to it and then null out property.Value
			// before adding the value to the new JProperty.  
			JProperty? property = (JProperty?)existingToken.Parent;

			var existingValue = property.Value;
			property.Value = null;
			var newProperty = new JProperty(newName, existingValue);
			try
			{
				property.Replace(newProperty);
			}
			catch (ArgumentException)
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, "A key already exists with that name!");
			}

			_changeCount++;

			return Tuple.Create(ResponseUtil.OK, "");
		}

		/// <summary>
		/// Insert data into a JArray of the JSON data structure
		/// </summary>
		/// <param name="path">JSON path of the desired data.</param>
		/// <param name="newData">New data to insert.</param>
		/// <param name="insertIndex">Index to insert data into.</param>
		/// <returns>Tuple of status and message</returns>
		public Tuple<int, string> InsertData(string path, string newData, int insertIndex)
		{
			if (string.IsNullOrEmpty(newData))
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, "Custom JSON cannot be empty!");
			}

			Tuple<int, string, string> result = PreProcess(path);
			if (result.Item1 != ResponseUtil.OK)
			{
				return Tuple.Create(result.Item1, result.Item2);
			}
			path = result.Item3;

			if (ValidPath(path) == false)
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, $"Path: {path} does not exist in the data!");
			}

			newData = SanitizeJCOLString(newData);

			JToken existingValue = _fileContents.SelectToken(BuildNewtonsoftKeyPath(path));

			if (existingValue.Type != JTokenType.Array)
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, $"JToken at Path: {path} is not of type JArray!");
			}

			JToken customJSON;
			try
			{
				// Convert new data to JToken. Reminder: this will convert numbers to JSON as well, but not strings...see catch block
				customJSON = JToken.Parse(newData);
			}
			catch
			{
				// check if the newData contains JSON symbols
				if (_jsonSymbols.Any(s => (newData).Contains(s)) == false)
				{
					// no JSON symbols detected, we can add quotes to it
					newData = "\"" + newData + "\"";

					try
					{
						customJSON = JToken.Parse(newData);
					}
					catch
					{
						return Tuple.Create(ResponseUtil.INTERNAL_ERROR, "Invalid custom json entered!");
					}
				}
				else
				{
					return Tuple.Create(ResponseUtil.INTERNAL_ERROR, "Invalid custom json entered!");
				}
			}

			JArray array = (JArray)existingValue;

			if (array.Count == 0)
			{
				// add the new data to the end of the array
				array.Add(customJSON);
			}
			else
			{
				try
				{
					array.Insert(insertIndex, customJSON);
				}
				catch
				{
					return Tuple.Create(ResponseUtil.INTERNAL_ERROR, "Invalid insert index!");
				}
			}

			_changeCount++;

			return Tuple.Create(ResponseUtil.OK, "");
		}

		/// <summary>
		/// Add data into a JSON data structure WHERE THE CHILD KEY DEFINED AT THE END OF PATH IS NOT YET IN THE JSON STRUCTURE.
		/// If the child key at the end of the path exists in the data, the function will return a ErrorUtil.INTERNAL_ERROR.
		/// NOTE: May require subclass overload (like FormDataService) if you want to add a type JObject instead of a string
		/// </summary>
		/// <param name="path">JSON path of the desired data.</param>
		/// <param name="newValue">New data to insert.</param>
		/// <returns>Tuple of status and message</returns>
		public Tuple<int, string> AddData(string path, string newValue)
		{
			Tuple<int, string, string, string> result = PreProcess(path, newValue);
			if (result.Item1 != ResponseUtil.OK)
			{
				return Tuple.Create(result.Item1, result.Item2);
			}
			path = result.Item3;
			newValue = result.Item4;

			if (ValidPath(path) == true)
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, "An element already exists at that path!");
			}

			if (NSPathService.IsPathTopLevel(path) == true)
			{
				// top level -> can just add it to the JSON
				_fileContents[path] = newValue;
			}
			else
			{
				// non-top level -> need to split and check base path 

				// get the parent path and the last key from the full path
				Tuple<string, string> pathResult = GetParentPathAndLastKey(path);
				string parentPath = pathResult.Item1;
				string lastKey = pathResult.Item2;

				// check the validity of the base path
				if (ValidPath(parentPath) == false)
				{
					return Tuple.Create(ResponseUtil.INTERNAL_ERROR, $"Parent path of path: {path} does not exist in the data!");
				}

				JToken existingValue = _fileContents.SelectToken(BuildNewtonsoftKeyPath(parentPath));

				existingValue[lastKey] = newValue;
			}

			_changeCount++;

			return Tuple.Create(ResponseUtil.OK, "");
		}

		/// <summary>
		/// Add data into a JSON data structure WHERE THE CHILD KEY DEFINED AT THE END OF PATH IS NOT YET IN THE JSON STRUCTURE.
		/// If the child key at the end of the path exists in the data, the function will return a ErrorUtil.INTERNAL_ERROR.
		/// NOTE: May require subclass overload (like FormDataService) if you want to add a type JObject instead of a string
		/// </summary>
		/// <param name="path">JSON path of the desired data.</param>
		/// <param name="newValue">New data to insert.</param>
		/// <returns>Tuple of status and message</returns>
		public Tuple<int, string> AddData(string path, int newValue)
		{
			Tuple<int, string, string> result = PreProcess(path);
			if (result.Item1 != ResponseUtil.OK)
			{
				return Tuple.Create(result.Item1, result.Item2);
			}
			path = result.Item3;

			if (ValidPath(path) == true)
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, "An element already exists at that path!");
			}

			if (NSPathService.IsPathTopLevel(path) == true)
			{
				// top level -> can just add it to the JSON
				_fileContents[path] = newValue;
			}
			else
			{
				// non-top level -> check base path 

				// get the parent path and the last key from the full path
				Tuple<string, string> pathResult = GetParentPathAndLastKey(path);
				string parentPath = pathResult.Item1;
				string lastKey = pathResult.Item2;

				// check the validity of the base path
				if (ValidPath(parentPath) == false)
				{
					return Tuple.Create(ResponseUtil.INTERNAL_ERROR, $"Parent path of path: {path} does not exist in the data!");
				}

				JToken existingValue = _fileContents.SelectToken(BuildNewtonsoftKeyPath(parentPath));

				existingValue[lastKey] = newValue;
			}

			_changeCount++;

			return Tuple.Create(ResponseUtil.OK, "");
		}

		/// <summary>
		/// Add data into a JSON data structure WHERE THE CHILD KEY DEFINED AT THE END OF PATH IS NOT YET IN THE JSON STRUCTURE.
		/// If the child key at the end of the path exists in the data, the function will return a ErrorUtil.INTERNAL_ERROR.
		/// NOTE: May require subclass overload (like FormDataService) if you want to add a type JObject instead of a string
		/// </summary>
		/// <param name="path">JSON path of the desired data.</param>
		/// <param name="newValue">New data to insert.</param>
		/// <returns>Tuple of status and message</returns>
		public Tuple<int, string> AddData(string path, float newValue)
		{
			Tuple<int, string, string> result = PreProcess(path);
			if (result.Item1 != ResponseUtil.OK)
			{
				return Tuple.Create(result.Item1, result.Item2);
			}
			path = result.Item3;

			if (ValidPath(path) == true)
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, "An element already exists at that path!");
			}

			if (NSPathService.IsPathTopLevel(path) == true)
			{
				// top level -> can just add it to the JSON
				_fileContents[path] = newValue;
			}
			else
			{
				// non-top level -> need to split and check for valid base path 

				// get the parent path and the last key from the full path
				Tuple<string, string> pathResult = GetParentPathAndLastKey(path);
				string parentPath = pathResult.Item1;
				string lastKey = pathResult.Item2;

				// check the validity of the base path
				if (ValidPath(parentPath) == false)
				{
					return Tuple.Create(ResponseUtil.INTERNAL_ERROR, $"Parent path of path: {path} does not exist in the data!");
				}

				JToken existingValue = _fileContents.SelectToken(BuildNewtonsoftKeyPath(parentPath));

				existingValue[lastKey] = newValue;
			}

			_changeCount++;

			return Tuple.Create(ResponseUtil.OK, "");
		}

		/// <summary>
		/// Add data into a JSON data structure WHERE THE CHILD KEY DEFINED AT THE END OF PATH IS NOT YET IN THE JSON STRUCTURE.
		/// If the child key at the end of the path exists in the data, the function will return a ErrorUtil.INTERNAL_ERROR.
		/// NOTE: May require subclass overload (like FormDataService) if you want to add a type JObject instead of a string
		/// </summary>
		/// <param name="path">JSON path of the desired data.</param>
		/// <param name="newValue">New data to insert.</param>
		/// <returns>Tuple of status and message</returns>
		public Tuple<int, string> AddData(string path, bool newValue)
		{
			Tuple<int, string, string> result = PreProcess(path);
			if (result.Item1 != ResponseUtil.OK)
			{
				return Tuple.Create(result.Item1, result.Item2);
			}
			path = result.Item3;

			if (ValidPath(path) == true)
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, "An element already exists at that path!");
			}

			if (NSPathService.IsPathTopLevel(path) == true)
			{
				// top level -> can just add it to the JSON
				_fileContents[path] = newValue;
			}
			else
			{
				// non-top level -> need to split and check base path 

				// get the parent path and the last key from the full path
				Tuple<string, string> pathResult = GetParentPathAndLastKey(path);
				string parentPath = pathResult.Item1;
				string lastKey = pathResult.Item2;

				// check the validity of the base path
				if (ValidPath(parentPath) == false)
				{
					return Tuple.Create(ResponseUtil.INTERNAL_ERROR, $"Parent path of path: {path} does not exist in the data!");
				}

				JToken existingValue = _fileContents.SelectToken(BuildNewtonsoftKeyPath(parentPath));

				existingValue[lastKey] = newValue;
			}

			_changeCount++;

			return Tuple.Create(ResponseUtil.OK, "");
		}

		/// <summary>
		/// Add data into a JSON data structure WHERE THE CHILD KEY DEFINED AT THE END OF PATH IS NOT YET IN THE JSON STRUCTURE.
		/// If the child key at the end of the path exists in the data, the function will return a ErrorUtil.INTERNAL_ERROR.
		/// NOTE: May require subclass overload (like FormDataService) if you want to add a type JObject instead of a string
		/// </summary>
		/// <param name="path">JSON path of the desired data.</param>
		/// <param name="newValue">New data to insert.</param>
		/// <returns>Tuple of status and message</returns>
		public Tuple<int, string> AddData(string path, JToken newValue)
		{
			Tuple<int, string, string> result = PreProcess(path);
			if (result.Item1 != ResponseUtil.OK)
			{
				return Tuple.Create(result.Item1, result.Item2);
			}
			path = result.Item3;

			if (ValidPath(path) == true)
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, "An element already exists at that path!");
			}

			if (NSPathService.IsPathTopLevel(path) == true)
			{
				// top level -> can just add it to the JSON
				_fileContents[path] = newValue;
			}
			else
			{
				// non-top level -> need to split and check base path 

				// get the parent path and the last key from the full path
				Tuple<string, string> pathResult = GetParentPathAndLastKey(path);
				string parentPath = pathResult.Item1;
				string lastKey = pathResult.Item2;

				// check the validity of the base path
				if (ValidPath(parentPath) == false)
				{
					return Tuple.Create(ResponseUtil.INTERNAL_ERROR, $"Parent path of path: {path} does not exist in the data!");
				}

				JToken existingValue = _fileContents.SelectToken(BuildNewtonsoftKeyPath(parentPath));

				existingValue[lastKey] = newValue;
			}

			_changeCount++;

			return Tuple.Create(ResponseUtil.OK, "");
		}

		/// <summary>
		/// Delete data from the JSON structure
		/// </summary>
		/// <param name="path">The path of the desired data.</param>
		/// <returns>Tuple of status and message</returns>
		public Tuple<int, string> DeleteData(string path)
		{
			Tuple<int, string, string> result = PreProcess(path);
			if (result.Item1 != ResponseUtil.OK)
			{
				return Tuple.Create(result.Item1, result.Item2);
			}
			path = result.Item3;

			if (ValidPath(path) == false)
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, $"Path: {path} does not exist in the data!");
			}

			// build the newtonsoft keypath
			string keyPath = BuildNewtonsoftKeyPath(path);
			// select the JToken using path
			JToken jToken = _fileContents.SelectToken(keyPath);

			/* {
             *  "key": "value"
             * }
             * 
             * JToken is the value "value".
             * JToken.Parent is the property "key"
             * JToken.Parent.Parent is the {} (JObject)
             * 
             * https://stackoverflow.com/questions/21898727/getting-the-error-cannot-add-or-remove-items-from-newtonsoft-json-linq-jpropert
             */
			if (jToken.Parent.Parent is JObject)
			{
				// delete the property from the JObject (2xParent is JObject Case)
				jToken.Parent.Remove();
			}
			else
			{
				// delete value from the JArray (2xParent is JArray Case)
				jToken.Remove();
			}

			_changeCount++;

			return Tuple.Create(ResponseUtil.OK, "");
		}

		public Tuple<int, string> DeleteArrayElements(string path)
		{
			Tuple<int, string, string> result = PreProcess(path);
			if (result.Item1 != ResponseUtil.OK)
			{
				return Tuple.Create(result.Item1, result.Item2);
			}
			path = result.Item3;

			if (ValidPath(path) == false)
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, $"Path: {path} does not exist in the data!");
			}

			// build the newtonsoft keypath
			string keyPath = BuildNewtonsoftKeyPath(path);
			// select the JToken using path
			JToken jToken = _fileContents.SelectToken(keyPath);

			JArray emptyArray = new JArray();
			if (jToken is JArray)
			{
				// "delete" all elements by replacing the array with an epty one
				// edit function will turn the new value into the correct type
				EditData(path, emptyArray.ToString());
			}
			else
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, $"Path: {path} is not an array!");
			}

			// since this calls EditData(), there is no need to update change count, EditData does it on its own

			return Tuple.Create(ResponseUtil.OK, "");
		}

		public Tuple<int, string> AddArrayElementWithTemplate(string arrayPath, int insertIndex, int templateIndex)
		{
			Tuple<int, string, JToken?> result = GetArrayElement(arrayPath, templateIndex);
			JToken template = result.Item3;

			if (result.Item1 != ResponseUtil.OK)
			{
				return Tuple.Create(result.Item1, "Invalid template index!");
			}

			Tuple<int, string> insertResult = InsertData(arrayPath, template.ToString(), insertIndex);

			if (insertResult.Item1 != ResponseUtil.OK)
			{
				return insertResult;
			}

			return Tuple.Create(ResponseUtil.OK, "");
		}

		public Tuple<int, string, JToken?> GetArrayElement(string arrayPath, int index)
		{
			return AccessData(BuildArrayPathWithIndex(arrayPath, index));
		}

		public Tuple<int, string> DeleteArrayElement(string arrayPath, int index)
		{
			return DeleteData(BuildArrayPathWithIndex(arrayPath, index));
		}

		public Tuple<int, string> DeleteAllArrayElements(string arrayPath)
		{
			return DeleteArrayElements(arrayPath);
		}

		public Tuple<int, string> AddObjectElement(string objectPath, string newKey, string newValue, string newValueType)
		{
			if (string.IsNullOrEmpty(newKey))
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, "You must enter in a valid key!");
			}

			string newPath = NSPathService.AddKeyToPath(objectPath, newKey);

			return AddValue(newPath, newValue, newValueType);
		}

		public Tuple<int, string, JToken?> GetObjectElement(string objectPath, string key)
		{
			JToken? dummy = new JObject();

			if (string.IsNullOrEmpty(key))
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, "You must select a key!", dummy);
			}

			return AccessData(NSPathService.AddKeyToPath(objectPath, key));
		}

		public Tuple<int, string> DeleteObjectElement(string objectPath, string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, "You must select a key!");
			}

			return DeleteData(NSPathService.AddKeyToPath(objectPath, key));
		}

		public Tuple<int, string> EditNullValue(string path, string newValue, string newValueType)
		{
			// delete the null key/value pair at Path (so we can replace it with an actual value)
			DeleteData(path);

			return AddValue(path, newValue, newValueType);
		}

		private Tuple<int, string> AddValue(string path, string newValue, string newValueType)
		{
			try
			{
				switch (newValueType)
				{
					case InputUtil.STRING:
						AddData(path, newValue);
						break;
					case InputUtil.INTEGER:
						AddData(path, int.Parse(newValue));
						break;
					case InputUtil.FLOAT:
						AddData(path, float.Parse(newValue));
						break;
					case InputUtil.BOOL:
						AddData(path, bool.Parse(newValue));
						break;
					case InputUtil.DATE:
						AddData(path, DateTime.Parse(newValue));
						break;
					case InputUtil.CUSTOM:
						AddData(path, JToken.Parse(newValue));
						break;
					default:
						return Tuple.Create(ResponseUtil.INTERNAL_ERROR, "You must select a value type!");
				}
			}
			catch (FormatException)
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, $"Cannot convert '{newValue}' to {newValueType}!");
			}
			catch (JsonReaderException)
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, $"Cannot convert value to custom JSON!");
			}

			return Tuple.Create(ResponseUtil.OK, "");
		}

		/// <summary>
		/// Build a newtonsoft keypath from string-based key path
		/// </summary>
		/// <param name="path">String-based key path</param>
		/// <returns>Newtonsoft key path</returns>
		public static string BuildNewtonsoftKeyPath(string path)
		{
			// https://www.newtonsoft.com/json/help/html/QueryJsonSelectTokenWithLinq.htm
			string keyPath = "";
			string key;
			List<string> keys = NSPathService.PathToKeyList(path);
			for (int i = 0; i < keys.Count; i++)
			{
				key = keys.ElementAt(i);
				if (key.Contains(NSPathService.PATH_ARRAY_INDEX_EMBLEM))
				{
					int arrayIndex = int.Parse(key.Replace(NSPathService.PATH_ARRAY_INDEX_EMBLEM, ""));
					{
						keyPath += $"[{arrayIndex}]";
					}
				}
				else
				{
					keyPath += $"['{key}']";
				}
			}
			return keyPath;
		}

		public static string BuildArrayPathWithIndex(string arrayPath, int index)
		{
			return $"{arrayPath}.{NSPathService.PATH_ARRAY_INDEX_EMBLEM}{index}";
		}

		/// <summary>
		/// Validate a JSON path
		/// </summary>
		/// <param name="path">The path to validate.</param>
		/// <returns>bool of valid path or not</returns>
		protected bool ValidPath(string path)
		{
			if (path == "")
			{
				return false;
			}

			// key check
			// https://stackoverflow.com/questions/33828942/set-json-attribute-by-path
			try
			{
				string keyPath = BuildNewtonsoftKeyPath(path);
				if (_fileContents.SelectToken(keyPath) is null)
				{
					return false;
				}
				else
				{
					return true;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return false;
			}
		}

		/// <summary>
		/// Get the parent path and the last key from any path.
		/// </summary>
		/// <param name="path">The FULL path to get parent path and last key from.</param>
		/// <returns>Tuple of parent path and last key</returns>
		public static Tuple<string, string> GetParentPathAndLastKey(string path)
		{
			// split the path into segments
			List<string> pathSegments = NSPathService.PathToKeyList(path);

			// the top level key will be the last element in the path
			string lastKey = pathSegments[^1];

			// take all segemnts except for the last
			List<string> parentPathSegments = new List<string>();
			for (int i = 0; i < pathSegments.Count - 1; i++)
			{
				parentPathSegments.Add(pathSegments[i]);
			}

			string parentPath = NSPathService.KeyListToPath(parentPathSegments);

			return Tuple.Create(parentPath, lastKey);
		}

		/// <summary>
		/// Performs a set of standard checks for valid operations.
		/// </summary>
		/// <param name="path">The path to be used</param>
		/// <returns>Tuple of parent path and last key
		///     1. status code
		///     2. message
		///     3. cleaned path
		/// </returns>
		protected Tuple<int, string, string> PreProcess(string path)
		{
			if (_fileContents is null)
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, "This object is empty!", "");
			}
			if (path is null)
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, "Path cannot be empty!", "");
			}
			if (path.Contains(' ') == true)
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, "Path cannot contain spaces!", "");
			}

			// remove any unwanted characters from the inputs
			path = CleanString(path);

			return Tuple.Create(ResponseUtil.OK, "", path);
		}

		/// <summary>
		/// Performs a set of standard checks for valid operations. This is the overload for methods
		/// that want to use the additional new value parameter.
		/// </summary>
		/// <param name="path">The path to be used</param>
		/// <returns>Tuple of parent path and last key
		///     1. status code
		///     2. message
		///     3. cleaned path
		///     4. cleaned new value
		/// </returns>
		protected Tuple<int, string, string, string> PreProcess(string path, string newValue)
		{
			// newValue is allowed to be empty, but not null
			if (newValue == null)
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, "New value cannot be null!", "", "");
			}

			// remove any unwanted characters from the inputs
			newValue = CleanString(newValue);

			Tuple<int, string, string> result = PreProcess(path);

			return Tuple.Create(result.Item1, result.Item2, result.Item3, newValue);
		}

		/// <summary>
		/// Performs a set of standard checks for valid operations of access methods.
		/// Access methods require a JToken return type because they are accessing a
		/// accessing and returning a chunk of the JSON. This requires a different 
		/// method signature that the other JSON operations do not require.
		/// </summary>
		/// <param name="path">The path to be used</param>
		/// <returns>Tuple of parent path and last key
		///     1. status code
		///     2. JToken
		///     3. cleaned path
		/// </returns>
		protected Tuple<int, JToken?, string> PreProcessAccess(string path)
		{
			if (_fileContents is null)
			{
				return Tuple.Create(ResponseUtil.INTERNAL_ERROR, (JToken?)new JObject(), "");
			}
			if (path is null)
			{
				// must throw an exception here because there is no way to return the message
				throw new ArgumentException($"Path passed was null!");
			}

			// remove any unwanted characters from the inputs
			path = CleanString(path);

			return Tuple.Create(ResponseUtil.OK, (JToken?)null, path);
		}

		/// <summary>
		/// Sanitiza a jcol string
		/// </summary>
		/// <param name=RecordUtil.JCOL>JSON string.</param>
		/// <returns>Sanitized JSON string</returns>
		public static string SanitizeJCOLString(string jcol)
		{
			char quoteChar = '"';
			char bracketChar = '{';

			// remove whitespace
			jcol = Regex.Replace(jcol, @"(""[^""\\]*(?:\\.[^""\\]*)*"")|\s+", "$1");

			string finaljcol = jcol;
			if (jcol.Length > 0)
			{
				// remove quotes from string
				if (jcol[0].Equals(quoteChar))
				{
					if (jcol[1].Equals(bracketChar))
					{
						if (jcol[jcol.Length - 1].Equals(quoteChar))
						{
							string newjcol = jcol.Remove(0, 1);

							finaljcol = newjcol.Remove(newjcol.Length - 1, 1);
						}
						else
						{
							finaljcol = jcol.Remove(0, 1);
						}
					}
				}
				// remove any backslashes from escaping
				finaljcol = finaljcol.Replace("\\", "");
			}

			return finaljcol;
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
