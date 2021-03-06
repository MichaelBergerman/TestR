﻿#region References

using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TestR.Desktop;

#endregion

namespace TestR.Extension
{
	/// <summary>
	/// Interaction logic for ExtensionWindowControl.
	/// </summary>
	public partial class ExtensionWindowControl
	{
		#region Fields

		private readonly Project _project;

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ExtensionWindowControl" /> class.
		/// </summary>
		public ExtensionWindowControl()
		{
			InitializeComponent();
			_project = new Project();
			DataContext = _project;
		}

		#endregion

		#region Methods

		private void ApplicationOnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			var element = e.NewValue as Element;
			if (element == null)
			{
				_project.ElementDetails = string.Empty;
				_project.Highlight((Element) null);
				return;
			}

			_project.ElementDetails = element.ToDetailString();
			_project.Highlight(element);
		}

		private void BrowserOnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			var element = e.NewValue as Web.Element;
			if (element == null)
			{
				_project.ElementDetails = string.Empty;
				_project.Highlight((Web.Element) null);
				return;
			}

			_project.ElementDetails = element.ToDetailString();
			_project.Highlight(element);
		}

		private void CloseApplication(object sender, RoutedEventArgs e)
		{
			_project.Highlight((Web.Element) null);
			_project.Highlight((Element) null);
			_project.Close();
		}

		private void RefreshChildren(object sender, RoutedEventArgs e)
		{
			_project.Refresh();
		}

		private void RefreshDesktopElement(object sender, RoutedEventArgs e)
		{
			var menuItem = sender as MenuItem;
			var element = menuItem?.DataContext as Element;
			element?.UpdateChildren();
		}

		private void RefreshWebElement(object sender, RoutedEventArgs e)
		{
			var menuItem = sender as MenuItem;
			var element = menuItem?.DataContext as Web.Element;
			element?.Browser.Refresh();
		}

		private void SelectApplication(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog();
			dialog.DefaultExt = ".exe";
			dialog.Filter = "EXE Files (*.exe)|*.exe";
			dialog.Multiselect = false;
			dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

			var result = dialog.ShowDialog();
			if (!result.HasValue || !result.Value)
			{
				return;
			}

			try
			{
				_project.Initialize(dialog.FileName);
				_project.Application.BringToFront();
			}
			catch (InvalidOperationException)
			{
				_project.Close();
			}
		}

		private void SelectProcess(object sender, RoutedEventArgs e)
		{
			var dialog = new ProcessWindow();
			dialog.Owner = (Window) Parent;

			var result = dialog.ShowDialog();
			if (!result.HasValue || !result.Value)
			{
				return;
			}

			var process = dialog.SelectedProcess;

			try
			{
				_project.Initialize(process);
				_project.Application.BringToFront();
			}
			catch (InvalidOperationException)
			{
				_project.Close();
			}
		}

		#endregion
	}
}