using TreeClimberCore.Services.JSON;

namespace TreeClimberCore.Services.Path
{
	/// <summary>
	/// Path Service provides a set of static methods to help any classes, methods, pages...etc. that 
	/// handle paths. These static methods help to simplify the logic in the afformetioned usage so
	/// the code can be kept DRY.
	/// </summary>
	public static class NSPathService
	{
		public const string PATH_ARRAY_INDEX_EMBLEM = "#Jarr:";
		public const string PATH_DELIMITER_STRING = ".";
		public const char PATH_DELIMITER_CHAR = '.';
		public const char COLON_DELIMITER = ':';

		/// <summary>
		/// Add a key to a path
		/// </summary>
		/// <param name="path">Path to add key to.</param>
		/// <param name="key">Key to add.</param>
		/// <returns>Path with key added.</returns>
		public static string AddKeyToPath(string path, string key)
		{
			//clean the key before we use it
			key = JSONFileDataService.CleanString(key);
			if (path.Equals(""))
			{
				path += key;
			}
			else
			{
				path += PATH_DELIMITER_STRING + key;
			}
			return path;
		}

		/// <summary>
		/// Remove last key from path
		/// </summary>
		/// <param name="path">Path to remove last element from.</param>
		/// <returns>Path with last element removed.</returns>
		/// <exception cref="ArgumentException">Path passed is empty</exception>
		public static string RemoveLastKeyFromPath(string path)
		{
			// convert the string to the list
			if (!path.Contains(PATH_DELIMITER_STRING))
			{
				if (path == "")
				{
					throw new ArgumentException("Cannot remove last element from empty path!");
				}
				else
				{
					// removing an index from a path with no delimiters (length 1) results in empty path
					return "";
				}
			}
			// implies that the path has delimiter in it
			List<string> pathList = path.Split(PATH_DELIMITER_CHAR).ToList();

			// build the new string
			string newPath = "";
			for (int i = 0; i < pathList.Count - 1; i++)
			{
				if (i == pathList.Count - 2)
				{
					newPath += pathList.ElementAt(i);
				}
				else
				{
					newPath += pathList.ElementAt(i) + PATH_DELIMITER_STRING;
				}
			}

			// update the path with the new string
			return newPath;
		}

		/// <summary>
		/// Get the last array index defined in a path
		/// </summary>
		/// <param name="path">Path to get the last array index from</param>
		/// <returns>Last array index as a string</returns>
		/// <exception cref="ArgumentException">Path passed is empty</exception>
		public static string GetLastArrayIndex(string path)
		{
			if (path == "")
			{
				throw new ArgumentException("Cannot retrieve last index from empty path!");
			}

			// convert path to a path list
			List<string> pathList = PathToKeyList(path);

			// get the element of the last index
			string lastPathElement = pathList[^1];

			// check that the element is an array
			if (!lastPathElement.Contains(PATH_ARRAY_INDEX_EMBLEM))
			{
				// the element is not an array
				return "-1";
			}

			// split the element string by ':'
			List<string> elementList = lastPathElement.Split(COLON_DELIMITER).ToList();

			// parse the numbers at the end
			string numbersAtEnd = elementList[^1];

			return numbersAtEnd;
		}

		/// <summary>
		/// Convert a string-path to list of keys
		/// </summary>
		/// <param name="path">Path to splice.</param>
		/// <returns>List of keys.</returns>
		public static List<string> PathToKeyList(string path)
		{
			if (path == "")
			{
				return new List<string>();
			}
			else
			{
				return path.Split(PATH_DELIMITER_CHAR).ToList();
			}
		}

		/// <summary>
		/// Convert a string-path to list of keys
		/// </summary>
		/// <param name="keyList">List of path keys to assemble.</param>
		/// <returns>Assembled path.</returns>
		public static string KeyListToPath(List<string> keyList)
		{
			if (keyList.Count == 0)
			{
				return "";
			}
			else
			{
				return string.Join(PATH_DELIMITER_CHAR, keyList);
			}
		}

		/// <summary>
		/// Check if provided path is top level or not.
		/// 
		/// Is top level if path does NOT contain any delimiters '.' 
		/// </summary>
		/// <param name="path">Path</param>
		/// <returns>True if path is top level</returns>
		public static bool IsPathTopLevel(string path) => !path.Contains(PATH_DELIMITER_CHAR);
	}
}
