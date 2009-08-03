﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Martin Koníček" email="martin.konicek@gmail.com"/>
//     <version>$Revision$</version>
// </file>
using System;

namespace ICSharpCode.SharpDevelop.Debugging
{
	/// <summary>
	/// Content of debugger tooltip, specifying whether it should be displayed in a WPF Popup.
	/// </summary>
	public interface ITooltip
	{
		/// <summary>
		/// If true, this ITooltip will be displayed in a WPF Popup.
		/// Otherwise this will be displayed in a WPF Tooltip.
		/// WPF Popups are (unlike WPF Tooltips) focusable.
		/// </summary>
		bool ShowAsPopup { get; }
		
		/// <summary>
		/// Closes this debugger tooltip.
		/// </summary>
		/// <returns>True if Close succeeded, false otherwise.</returns>
		bool Close();
	}
}
