﻿#region References

using UIAutomationClient;

#endregion

namespace TestR.Desktop.Elements
{
	/// <summary>
	/// Represents the list for a window.
	/// </summary>
	public class List : Element
	{
		#region Constructors

		internal List(IUIAutomationElement element, IElementParent parent)
			: base(element, parent)
		{
		}

		#endregion
	}
}