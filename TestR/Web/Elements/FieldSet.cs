﻿#region References

using Newtonsoft.Json.Linq;

#endregion

namespace TestR.Web.Elements
{
	/// <summary>
	/// Represents a browser fieldset element.
	/// </summary>
	public class FieldSet : Element
	{
		#region Constructors

		/// <summary>
		/// Initializes an instance of a browser element.
		/// </summary>
		/// <param name="element"> The browser element this is for. </param>
		/// <param name="browser"> The browser this element is associated with. </param>
		/// <param name="collection"> The collection this element is associated with. </param>
		public FieldSet(JToken element, Browser browser, ElementCollection collection)
			: base(element, browser, collection)
		{
		}

		#endregion
	}
}