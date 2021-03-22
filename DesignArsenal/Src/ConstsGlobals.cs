using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Ion;

namespace DesignArsenal
{
	static partial class Consts
	{
		public const string AppName = "Design Arsenal";
		public const EProduct ProductID = EProduct.DESIGNARSENAL;
		public static readonly string DirLibrary = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/Library/Design Arsenal";
		public static readonly string DirUserData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/MISoftware/DesignArsenal/";
		public static readonly string DirUserCache = DirUserData + "Cache/";
		public static readonly string DirUserCache_Fonts = DirUserCache + "Fonts/";
		public static readonly string DirUserCache_LF = DirUserCache_Fonts + "lfcache.json";
		public static readonly string DirUserCache_StoreIcons = DirUserCache + "StoreIcons/";
		public static readonly string DirUserCache_StorePatterns = DirUserCache + "StorePatterns/";
		public static readonly string DirUserCache_LinkThumbs = DirUserCache + "Thumbs/";
		public static readonly string APP_EXE = Process.GetCurrentProcess().MainModule.FileName;
		public static readonly string APP_DIR = Path.GetDirectoryName(APP_EXE) + Path.DirectorySeparatorChar;
		//public static readonly string APP_DIR_SLASH = APP_DIR.Replace('\\', '/');
        public static readonly string LOG_FILE = DirUserData + "log.txt";

		public static readonly string DirUserFiles = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/" + AppName + "/";
		public static readonly string DirUserFiles_Fonts = DirUserFiles + "Fonts/";
		public static readonly string DirUserFiles_Icons = DirUserFiles + "Icons/";
		public static readonly string DirUserFiles_Pattenrs = DirUserFiles + "Patterns/";

		public const string SERVER_ASSETS = "https://storagemvc.blob.core.windows.net/arsenal/";

#if DEBUG
		public const string SERVER_SYNC = "https://localhost:44307/";
#else
		public const string SERVER_SYNC = "https://localhost:44307/";
#endif

		static Consts()
		{
			CreateDirs();
		}

		public static void CreateDirs()
		{
			Directory.CreateDirectory(DirUserData);
			Directory.CreateDirectory(DirUserCache_Fonts);
			Directory.CreateDirectory(DirUserCache_StoreIcons);
			Directory.CreateDirectory(DirUserCache_StorePatterns);
			Directory.CreateDirectory(DirUserCache_LinkThumbs);
		}

#if WINDOWS
		public static readonly string AppDir_Shared = Path.GetFullPath(APP_DIR + "\\Shared\\");
#elif OSX
		public static readonly string AppDir_Tmp = Path.GetTempPath() + "tmp/";
		public static readonly string AppDir_Resources = Foundation.NSBundle.MainBundle.ResourcePath + '/';
		public static readonly string AppDir_Shared = Path.GetFullPath(APP_DIR + "../Shared/");

		#if DEBUG
		public static readonly string AppDir_SharedSource = Path.GetFullPath(APP_DIR + "../../../../../Shared/");
		#else
		public static readonly string AppDir_SharedSource = AppDir_Shared;
		#endif
#endif
	}

	static class Globals
	{
		// so I can use in AppTests, AppMD
		//public static BaseHost GlobalHost;
		//public static SciterWindow GlobalWnd;

        public static void Log(string msg)
        {
            Console.WriteLine(msg);
            File.WriteAllText(Consts.LOG_FILE, msg);
        }
	}
}