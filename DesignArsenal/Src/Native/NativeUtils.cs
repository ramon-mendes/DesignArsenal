﻿#if WINDOWS
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace DesignArsenal.Native
{
	static class NativeUtils
	{
		public static void StartSelf(string args)
		{
			try
			{
				Process.Start(Process.GetCurrentProcess().MainModule.FileName, args);
			}
			catch(Exception ex)
			{
				throw;
			}
		}

		public static bool StartElevated(string args, bool wait = false)
		{
			try
			{
				var p = Process.Start(new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName, args) { Verb = "runas" });
				if(wait)
					p.WaitForExit();
			}
			catch(Exception ex)
			{
				return false;
			}
			return true;
		}

		public static bool StartElevatedProc(string args, out Process proc)
		{
			try
			{
				var p = Process.Start(new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName, args) { Verb = "runas" });
				proc = p;
			}
			catch(Exception ex)
			{
				proc = null;
				return false;
			}
			return true;
		}

		public static void Trace(string str)
		{
			 OutputDebugString(str);
		}

		[DllImport("kernel32.dll")]
		static extern void OutputDebugString(string lpOutputString);
	}

	static class UacHelper
	{
		private const string uacRegistryKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
		private const string uacRegistryValue = "EnableLUA";

		private static uint STANDARD_RIGHTS_READ = 0x00020000;
		private static uint TOKEN_QUERY = 0x0008;
		private static uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);

		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern bool GetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength);


		public enum TOKEN_INFORMATION_CLASS
		{
			TokenUser = 1,
			TokenGroups,
			TokenPrivileges,
			TokenOwner,
			TokenPrimaryGroup,
			TokenDefaultDacl,
			TokenSource,
			TokenType,
			TokenImpersonationLevel,
			TokenStatistics,
			TokenRestrictedSids,
			TokenSessionId,
			TokenGroupsAndPrivileges,
			TokenSessionReference,
			TokenSandBoxInert,
			TokenAuditPolicy,
			TokenOrigin,
			TokenElevationType,
			TokenLinkedToken,
			TokenElevation,
			TokenHasRestrictions,
			TokenAccessInformation,
			TokenVirtualizationAllowed,
			TokenVirtualizationEnabled,
			TokenIntegrityLevel,
			TokenUIAccess,
			TokenMandatoryPolicy,
			TokenLogonSid,
			MaxTokenInfoClass
		}

		public enum TOKEN_ELEVATION_TYPE
		{
			TokenElevationTypeDefault = 1,
			TokenElevationTypeFull,
			TokenElevationTypeLimited
		}

		public static bool IsUacEnabled
		{
			get
			{
				RegistryKey uacKey = Registry.LocalMachine.OpenSubKey(uacRegistryKey, false);
				bool result = uacKey.GetValue(uacRegistryValue).Equals(1);
				return result;
			}
		}

		public static bool IsProcessElevated
		{
			get
			{
				//Debug.Assert(false);

				if(IsUacEnabled)
				{
					IntPtr tokenHandle;
					if(!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_READ, out tokenHandle))
					{
						throw new ApplicationException("Could not get process token. Win32 Error Code: " + Marshal.GetLastWin32Error());
					}

					TOKEN_ELEVATION_TYPE elevationResult = TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault;

					int elevationResultSize = Marshal.SizeOf((int)elevationResult);
					uint returnedSize = 0;
					IntPtr elevationTypePtr = Marshal.AllocHGlobal(elevationResultSize);

					bool success = GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevationType, elevationTypePtr, (uint)elevationResultSize, out returnedSize);
					if(success)
					{
						elevationResult = (TOKEN_ELEVATION_TYPE)Marshal.ReadInt32(elevationTypePtr);
						bool isProcessAdmin = elevationResult == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull;
						return isProcessAdmin;
					}
					else
					{
						throw new ApplicationException("Unable to determine the current elevation.");
					}
				}
				else
				{
					WindowsIdentity identity = WindowsIdentity.GetCurrent();
					WindowsPrincipal principal = new WindowsPrincipal(identity);
					bool result = principal.IsInRole(WindowsBuiltInRole.Administrator);
					return result;
				}
			}
		}
	}
}
#endif
