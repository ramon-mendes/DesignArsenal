﻿#if WINDOWS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace DesignArsenal.Native
{
	[ComImport, Guid("00000121-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface IDropSource
	{
		[PreserveSig]
		uint QueryContinueDrag([MarshalAs(UnmanagedType.Bool)] bool fEscapePressed, uint grfKeyState);

		[PreserveSig]
		uint GiveFeedback(uint dwEffect);
	}

	class DragSource : IDropSource
	{
		[DllImport("ole32.dll")]
		static extern int DoDragDrop(IDataObject pDataObject, IDropSource pDropSource, int dwOKEffect, int[] pdwEffect);

		public uint GiveFeedback(uint dwEffect)
		{
			return 0;
		}

		public uint QueryContinueDrag([MarshalAs(UnmanagedType.Bool)] bool fEscapePressed, uint grfKeyState)
		{
			return 0;
		}
	}
}
#endif
