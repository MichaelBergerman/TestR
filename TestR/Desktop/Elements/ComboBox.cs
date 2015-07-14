﻿#region References

using TestR.Desktop.Automation;

#endregion

namespace TestR.Desktop.Elements
{
	/// <summary>
	/// Represents a combo box element.
	/// </summary>
	public class ComboBox : Element
	{
		#region Constructors

		internal ComboBox(AutomationElement element, IElementParent parent)
			: base(element, parent)
		{
		}

		#endregion
	}
}