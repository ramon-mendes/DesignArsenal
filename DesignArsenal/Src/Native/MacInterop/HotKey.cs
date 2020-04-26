using System;
using AppKit;
using Foundation;

namespace MonoDevelop.MacInterop
{
	public class HotKey
	{
		private static int _countHk = 3;
		private int _nHk = 0;

		public HotKey(NSKey key, NSEventModifierMask mask, Action cbk)
		{
			const uint kEventHotKeyPressed	= 5;
			_nHk = _countHk++;

			CarbonEventTypeSpec spec = new CarbonEventTypeSpec(CarbonEventClass.Keyboard, kEventHotKeyPressed);
			Carbon.InstallEventHandler(Carbon.GetApplicationEventTarget(), (callRef, eventRef, userData) =>
			{
				if(userData.ToInt32() == _nHk)
					cbk();
				return CarbonEventHandlerStatus.Handled;
			}, new CarbonEventTypeSpec[] { spec }, new IntPtr(_nHk));

			const uint kVK_ANSI_D = 0x02;
			uint cmdKey = 1 << 8;
			uint shiftKey = 1 << 9;
			uint maskCarbon = 0;
			if(mask.HasFlag(NSEventModifierMask.ShiftKeyMask))
				maskCarbon |= shiftKey;
			if(mask.HasFlag(NSEventModifierMask.CommandKeyMask))
				maskCarbon |= cmdKey;

			Carbon.EventHotKeyID hk = new Carbon.EventHotKeyID()
			{
				id = 1,
				signature = new OSType("htk1")
			};

			IntPtr outRef;
			Carbon.RegisterEventHotKey((uint) key, maskCarbon, hk, Carbon.GetApplicationEventTarget(), 0, out outRef);
		}
	}
}