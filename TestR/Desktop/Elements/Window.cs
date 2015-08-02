﻿#region References

using System.Linq;
using TestR.Helpers;
using TestR.Native;
using UIAutomationClient;

#endregion

namespace TestR.Desktop.Elements
{
	/// <summary>
	/// Represents a window for an application.
	/// </summary>
	public class Window : Element
	{
		#region Constructors

		/// <summary>
		/// Instantiates a window element.
		/// </summary>
		/// <param name="element"> The automation element for this element. </param>
		/// <param name="parent"> The parent for this element. </param>
		internal Window(IUIAutomationElement element, IElementParent parent)
			: base(element, parent)
		{
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the status bar for the window. Returns null if the window does not have a status bar.
		/// </summary>
		public StatusBar StatusBar
		{
			get { return Children.StatusBars.FirstOrDefault(); }
		}

		/// <summary>
		/// Gets the title bar for the window. Returns null if the window does not have a title bar.
		/// </summary>
		public TitleBar TitleBar
		{
			get { return Children.TitleBars.FirstOrDefault(); }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Bring the window to the front.
		/// </summary>
		public void BringToFront()
		{
			var handle = NativeElement.CurrentNativeWindowHandle;
			NativeMethods.SetForegroundWindow(handle);
			NativeMethods.BringWindowToTop(handle);
		}

		/// <summary>
		/// Closes a window.
		/// </summary>
		public void Close()
		{
			if (TitleBar != null)
			{
				TitleBar.CloseButton.MoveMouseTo();
				//TitleBar.CloseButton.Click();
			}
		}

		/// <summary>
		/// Waits for the window to no longer be busy.
		/// </summary>
		public void WaitWhileBusy()
		{
			WaitForWindow();
			HourGlassWait();
		}

		private static void HourGlassWait()
		{
			Utility.Wait(() => MouseCursor.WaitCursors.Contains(Mouse.Cursor));
		}

		private void WaitForWindow()
		{
			// todo: why does this not work for window?
			//if (_pattern.WaitForInputIdle(100) < 0)
			//{
			//	throw new Exception("Timed out waiting for window to respond.");
			//}

			//Utility.Wait(() => _pattern.Current.WindowInteractionState != WindowInteractionState.NotResponding);
		}

		#endregion
	}
}