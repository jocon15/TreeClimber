using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace TreeClimber.Util
{
	public class CacheUtil
	{
		[Inject]
		public static IFileVersionProvider FileVersionProvider { get; set; }

		/* This is function acts as a cache-buster.
        *
        * The scoped (custom) css files that we define in wwwroot/css are cached by the browser as a part of normal operation.
        * Since we began usage of OnParametersSetAsync() vs. OnInitialized(), we stopped using forceload:true because it was no
        * longer needed. This was likely the reason that the browser was not reloading the css files.
        *
        * So, no changes to the scoped css files were reflected in the application because the browser used the old cached css file
        * instead of loading the new one. With this function, the scoped css files are appended with a file version string to force the broswer
        * to load and use the newest version of the file at runtime.
        */
		public static string AppendVersion(IFileVersionProvider vp, string path) => vp.AddFileVersionToPath("/", path);
	}
}
