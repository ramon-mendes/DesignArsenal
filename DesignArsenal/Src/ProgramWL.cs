#if WINDOWS || GTKMONO
using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using SciterSharp;
using SciterSharp.Interop;
using DesignArsenal.Native;
using DesignArsenal.DataFD;
using System.Net;
using System.IO;
using DesignArsenal.Apps;

namespace DesignArsenal
{
	class Program
	{
		static Program()
		{
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
		}

		[STAThread]
		static int Main(string[] args)
		{
#if WINDOWS
			// Sciter needs this for drag'n'drop support; STAThread is required for OleInitialize succeess
			int oleres = PInvokeWindows.OleInitialize(IntPtr.Zero);
			Debug.Assert(oleres == 0);
#endif
#if GTKMONO
			PInvokeGTK.gtk_init(IntPtr.Zero, IntPtr.Zero);
			Mono.Setup();
#endif

#if DEBUG
			//XD.CopyLayer("Arial", "");

			/*var a = (MemoryStream)Clipboard.GetData("com.adobe.xd");
			var b = a.ToArray();
			var s = System.Text.Encoding.Unicode.GetString(b);*/
#endif

			string vs2013 = @"SOFTWARE\Classes\Installer\Dependencies\{050d4fc8-5d48-4b8f-8972-47c82c46020f}";
			string file = "vcredist_x64.exe";
			var res = Registry.LocalMachine.OpenSubKey(vs2013);
			if(res == null)
			{
				var ok = System.Windows.Forms.MessageBox.Show($"For running {Consts.AppName}, you must install 'Visual C++ Redistributable Packages for Visual Studio 2013'.\n\nClick OK to go to the download page.\nThen download and install: " + file, Consts.AppName, MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
				if(ok == DialogResult.OK)
					Process.Start("https://www.microsoft.com/en-us/download/details.aspx?id=40784");
				return 0;
			}

			#region Args handling
			bool arg_in_test = false;
			bool arg_hide = false;

			if(args.Length != 0 && args[0] == "-test")
			{
				arg_in_test = true;
			}
			else if(args.Length != 0 && args[0] == "-hide")
			{
				arg_hide = true;
			}
			else if(args.Length != 0)
			{
				Joiner.Setup(false);

#if WINDOWS
				if(args[0] == "-missing-dlg")
				{
					//AppMD.Run(args[1], args.Length == 3);
					return 0;
				}

				//if(UacHelper.IsProcessElevated)
				{
					if(args[0].StartsWith("-perm-install:"))
					{
						string family = args[0].Substring(14).Trim('"');
						return Installer.PermanentlyInstall(true, family) ? 0 : -1;
					}
					if(args[0].StartsWith("-perm-uninstall:"))
					{
						string family = args[0].Substring(16).Trim('"');
						Installer.PermanentlyInstall(false, family);
						return 0;
					}
					if(args[0].StartsWith("-perm-uninstall-all"))
					{
						Installer.PermanentlyUninstallAll();
						return 0;
					}
				}
#endif
				return -1;
			}
			#endregion


			/*if(!arg_in_test && SingleInstance.IsRunningAndAcquire())
			{
				Debug.WriteLine("ALREADY RUNNING!");
				return;
			}*/

			/*AppDomain.CurrentDomain.UnhandledException += (s, e) =>
			{
				SciterSharp.MessageBox.Show(IntPtr.Zero, e.ExceptionObject.ToString(), "FUCK");
			};*/

			App.Run(arg_in_test, arg_hide);

			SingleInstance.Release();

			GC.Collect();
			GC.WaitForPendingFinalizers();

			return 0;
		}
	}
}
#endif