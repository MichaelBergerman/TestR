﻿#region References

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

#endregion

namespace TestR.Native
{
	internal static class NativeMethods
	{
		#region Fields

		private static readonly IntPtr _notTopMost = new IntPtr(-2);
		private static readonly Guid _securityManagerClsid = new Guid("7b8a2d94-0ac9-11d1-896c-00c04fb6bfc4");
		private static readonly IntPtr _topMost = new IntPtr(-1);

		#endregion

		#region Methods

		public static void BringToTop(IntPtr handle)
		{
			SetWindowPos(handle, _notTopMost, 0, 0, 0, 0, SetWindowPosFlags.NoMove | SetWindowPosFlags.NoSize);
			SetWindowPos(handle, _topMost, 0, 0, 0, 0, SetWindowPosFlags.NoMove | SetWindowPosFlags.NoSize);
			SetWindowPos(handle, _notTopMost, 0, 0, 0, 0, SetWindowPosFlags.NoMove | SetWindowPosFlags.NoSize);
		}

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool BringWindowToTop(IntPtr hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
		internal static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		internal static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool GetClientRect(IntPtr hWnd, out Rect lpRect);

		[DllImport("user32.dll", EntryPoint = "GetCursorPos", SetLastError = true)]
		internal static extern bool GetCursorPosition(out System.Drawing.Point lpMousePoint);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int GetKeyState(VirtualKeyStates keyState);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern IntPtr GetModuleHandle(string lpModuleName);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern IntPtr GetParent(IntPtr hWnd);

		internal static WindowPlacement GetWindowPlacement(IntPtr handle)
		{
			var placement = new WindowPlacement();
			placement.length = Marshal.SizeOf(placement);
			GetWindowPlacement(handle, ref placement);
			return placement;
		}

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		internal static int GetZoneId(string uri)
		{
			var t = Type.GetTypeFromCLSID(_securityManagerClsid);
			var securityManager = Activator.CreateInstance(t);
			var manager = securityManager as IInternetSecurityManager;
			if (manager == null)
			{
				return -1;
			}

			try
			{
				uint zone;
				manager.MapUrlToZone(uri, out zone, 0);
				return (int) zone;
			}
			finally
			{
				Marshal.ReleaseComObject(securityManager);
			}
		}

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("user32.dll", SetLastError = true, EntryPoint = "mouse_event")]
		internal static extern void MouseEvent(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

		[DllImport("user32.dll", SetLastError = true, EntryPoint = "SetCursorPos")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool SetCursorPosition(int x, int y);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool SetFocus(IntPtr hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int SetWindowsHookEx(int idHook, HookDelegate lpfn, IntPtr hMod, int dwThreadId);

		internal static void ShowWindow(IntPtr handle)
		{
			ShowWindowAsync(handle, 10);
			ShowWindowAsync(handle, 5);
		}

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool ShowWindowAsync(IntPtr windowHandle, int nCmdShow);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int UnhookWindowsHookEx(int idHook);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement lpwndpl);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, SetWindowPosFlags uFlags);

		#endregion

		#region Delegates

		internal delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

		internal delegate int HookDelegate(int code, int wParam, IntPtr lParam);

		#endregion

		#region Structures

		[StructLayout(LayoutKind.Sequential)]
		internal struct MouseHook
		{
			#region Fields

			internal Point pt;
			internal uint mouseData;
			internal uint flags;
			internal uint time;
			internal IntPtr dwExtraInfo;

			#endregion
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct Point
		{
			#region Fields

			internal int x;
			internal int y;

			#endregion
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct Rect
		{
			internal int Left;
			internal int Top;
			internal int Right;
			internal int Bottom;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct WindowPlacement
		{
			internal int length;
			internal int flags;
			internal int ShowState;
			internal System.Drawing.Point ptMinPosition;
			internal System.Drawing.Point ptMaxPosition;
			internal Rectangle rcNormalPosition;
		}

		#endregion

		#region Enumerations

		internal enum MouseMessages
		{
			WM_LBUTTONDOWN = 0x0201,
			WM_LBUTTONUP = 0x0202,
			WM_MOUSEMOVE = 0x0200,
			WM_MOUSEWHEEL = 0x020A,
			WM_RBUTTONDOWN = 0x0204,
			WM_RBUTTONUP = 0x0205
		}

		[Flags]
		internal enum SetWindowPosFlags : uint
		{
			/// <summary>
			/// Retains the current position (ignores X and Y parameters).
			/// </summary>
			NoMove = 0x0002,

			/// <summary>
			/// Retains the current size (ignores the cx and cy parameters).
			/// </summary>
			NoSize = 0x0001
		}

		internal enum VirtualKeyStates
		{
			VK_LSHIFT = 0xA0,
			VK_RSHIFT = 0xA1,
			VK_LCONTROL = 0xA2,
			VK_RCONTROL = 0xA3
		}

		#endregion

		#region Interfaces

		[ComImport]
		[Guid("79EAC9EE-BAF9-11CE-8C82-00AA004BA90B")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface IInternetSecurityManager
		{
			#region Methods

			[return: MarshalAs(UnmanagedType.I4)]
			[PreserveSig]
			int SetSecuritySite([In] IntPtr pSite);

			[return: MarshalAs(UnmanagedType.I4)]
			[PreserveSig]
			int GetSecuritySite([Out] IntPtr pSite);

			[return: MarshalAs(UnmanagedType.I4)]
			[PreserveSig]
			int MapUrlToZone([In] [MarshalAs(UnmanagedType.LPWStr)] string pwszUrl, out uint pdwZone, uint dwFlags);

			[return: MarshalAs(UnmanagedType.I4)]
			[PreserveSig]
			int GetSecurityId([MarshalAs(UnmanagedType.LPWStr)] string pwszUrl, [MarshalAs(UnmanagedType.LPArray)] byte[] pbSecurityId, ref uint pcbSecurityId, uint dwReserved);

			[return: MarshalAs(UnmanagedType.I4)]
			[PreserveSig]
			int ProcessUrlAction([In] [MarshalAs(UnmanagedType.LPWStr)] string pwszUrl, uint dwAction, out byte pPolicy, uint cbPolicy, byte pContext, uint cbContext, uint dwFlags, uint dwReserved);

			[return: MarshalAs(UnmanagedType.I4)]
			[PreserveSig]
			int QueryCustomPolicy([In] [MarshalAs(UnmanagedType.LPWStr)] string pwszUrl, ref Guid guidKey, ref byte ppPolicy, ref uint pcbPolicy, ref byte pContext, uint cbContext, uint dwReserved);

			[return: MarshalAs(UnmanagedType.I4)]
			[PreserveSig]
			int SetZoneMapping(uint dwZone, [In] [MarshalAs(UnmanagedType.LPWStr)] string lpszPattern, uint dwFlags);

			[return: MarshalAs(UnmanagedType.I4)]
			[PreserveSig]
			int GetZoneMappings(uint dwZone, out IEnumString ppenumString, uint dwFlags);

			#endregion
		}

		#endregion
	}
}