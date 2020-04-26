using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using DesignArsenal;

partial class Script
{
	const string APPNAME = "DesignArsenal";
	const string APPNAME_EXE = APPNAME + ".exe";
	const string CONFIG = "Release";

	public static void Main(string[] args)
	{
		if(Environment.OSVersion.Platform == PlatformID.Win32NT)
			CWD = Path.GetFullPath(Environment.CurrentDirectory + "/../../../ReleaseInfo/");
		else
			CWD = "/Users/midiway/Documents/DesignArsenal/";

		Environment.CurrentDirectory = CWD;

		string exe_test;
		if(true)
		{
			GitPush();
			exe_test = BuildAndDeploy();

			// Run with -test
			Console.WriteLine("### RUN + TESTS (WAITS FOR EXIT) ###");
			SpawnProcess(exe_test, "-test");
		} else {
			_upload_output = CWD + "ReleaseInfo/Latest/DesignArsenal.zip";
		}

		// Copy to DB
		Console.WriteLine("### UPLOAD");
		if(Environment.OSVersion.Platform == PlatformID.Win32NT)
			File.Copy(_upload_output, @"D:\Dropbox\Apps\DesignArsenalWIN.zip", true);
		else
			File.Copy(_upload_output, @"/Users/midiway/Dropbox/Apps/DesignArsenalOSX.zip", true);

		// Save version
		Console.WriteLine("### UPDATE INFO");
		using(WebClient wb = new WebClient())
		{
			wb.DownloadString("http://ion-mvc.azurewebsites.net/Info/SetInfo?ep=DESIGNARSENAL&version=" + Consts.VersionInt);
		}
	}

	static string BuildAndDeploy()
	{
		_upload_output = CWD + "/DesignArsenal.zip";

		Console.WriteLine("### BUILD ###");
		if(Environment.OSVersion.Platform == PlatformID.Unix)
		{
			string APPAPP = CWD + "DesignArsenal/bin/Release/DesignArsenal.app";
			string APPZIPPARENT = CWD + "ReleaseInfo/Latest/Output/";
			string APPLATEST = APPZIPPARENT + "DesignArsenal.app/";

			SpawnProcess("sh", CWD + "DesignArsenal/scripts/preBuildOSX.sh");
			SpawnProcess("msbuild", CWD + "DesignArsenal/DesignArsenalOSX.csproj /t:Build /p:Configuration=Release");

			if(Directory.Exists(APPLATEST))
				Directory.Delete(APPLATEST, true);
			Directory.CreateDirectory(APPZIPPARENT);
			Directory.Move(APPAPP, APPLATEST);

			if(File.Exists(_upload_output))
				File.Delete(_upload_output);
			ZipFile.CreateFromDirectory(APPZIPPARENT, _upload_output);

			return APPLATEST + "Contents/MacOS/DesignArsenal";
		}
		else
		{
			string how = "Clean,Build";
			SpawnProcess(@"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\msbuild.exe",
				    CWD + $"..\\{APPNAME}\\{APPNAME}Windows.csproj /t:{how} /p:Configuration={CONFIG} /p:Platform=x64");

			#region Pack
			var WORK_DIR = $"{CWD}TmpInput\\";
			var CONFUSE_OUTDIR = $"{CWD}OutConfused\\";
			var CONFUSE_PROJ = "Confuse.crproj";

			// Delete and create these dirs
			if(Directory.Exists(WORK_DIR))
				Directory.Delete(WORK_DIR, true);
			Directory.CreateDirectory(WORK_DIR);

			if(Directory.Exists(CONFUSE_OUTDIR))
				Directory.Delete(CONFUSE_OUTDIR, true);
			Directory.CreateDirectory(CONFUSE_OUTDIR);

			// Copy \bin\Release to WORK_DIR
			string BIN_DIR = Path.GetFullPath(CWD + "..\\" + APPNAME + "\\bin\\Release");

			var files1 = Directory.EnumerateFiles(BIN_DIR, "*.exe", SearchOption.AllDirectories);
			var files2 = Directory.EnumerateFiles(BIN_DIR, "*.dll", SearchOption.AllDirectories);
			foreach(var file in files1.Union(files2))
			{
				string subpath = file.Substring(BIN_DIR.Length);
				string outpath = WORK_DIR + subpath;
				Directory.CreateDirectory(Path.GetDirectoryName(outpath));
				File.Copy(file, outpath);
			}

			// Copy \Shared to WORK_DIR
			string SHARED_DIR = Path.GetFullPath(CWD + "..\\" + APPNAME + "\\Shared");
			DirectoryCopy(SHARED_DIR, WORK_DIR + "\\Shared", true);

			// Delete SketchPlugin
			Directory.Delete(WORK_DIR + "\\Shared\\SketchPlugin", true);

			// Confuse
			SpawnProcess(CWD + @"ConfuserEx_bin\Confuser.CLI.exe", $"-noPause {CONFUSE_PROJ}");
			File.Copy(CONFUSE_OUTDIR + APPNAME_EXE, WORK_DIR + APPNAME_EXE, true);

			// Generate installer
			SpawnProcess("iscc", "installer.iss");

			// Get version / Rename dir
			var OUT_DIR = CWD + $"Latest\\";
			if(Directory.Exists(OUT_DIR))
			{
				try
				{
					Directory.Delete(OUT_DIR, true);
				}
				catch(Exception)
				{
					Debugger.Break();// close it plz
					Directory.Delete(OUT_DIR, true);
				}
			}
			Directory.Move(WORK_DIR, OUT_DIR);
			#endregion

			// ZIP
			if(File.Exists(_upload_output))
				File.Delete(_upload_output);
			ZipFile.CreateFromDirectory(OUT_DIR, _upload_output);

			return OUT_DIR + APPNAME_EXE;
		}
	}
}