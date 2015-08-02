﻿#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using TestR.Desktop.Elements;
using TestR.Extensions;
using TestR.Helpers;
using TestR.Native;

#endregion

namespace TestR.Desktop
{
	/// <summary>
	/// Represents an application that can be automated.
	/// </summary>
	public class Application : IDisposable, IElementParent
	{
		#region Constructors

		/// <summary>
		/// Creates an instance of the application.
		/// </summary>
		/// <param name="process"> The process for the application. </param>
		internal Application(Process process)
		{
			Children = new ElementCollection<Element>(this);
			Process = process;
			Process.Exited += (sender, args) => OnClosed();
			Process.EnableRaisingEvents = true;
			Timeout = TimeSpan.FromSeconds(5);
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets a flag to auto close the application when disposed of.
		/// </summary>
		public bool AutoClose { get; set; }

		/// <summary>
		/// Gets the children for this element.
		/// </summary>
		public ElementCollection<Element> Children { get; }

		/// <summary>
		/// Gets the handle for this window.
		/// </summary>
		public IntPtr Handle => Process.MainWindowHandle;

		/// <summary>
		/// Gets the ID of this application.
		/// </summary>
		public string Id => Process.Id.ToString();

		/// <summary>
		/// Gets the value indicating that the process is running.
		/// </summary>
		public bool IsRunning => Process != null && !Process.HasExited;

		/// <summary>
		/// Gets the name of this element.
		/// </summary>
		public string Name => Handle.ToString();

		/// <summary>
		/// Gets the underlying process for this application.
		/// </summary>
		public Process Process { get; private set; }

		/// <summary>
		/// Gets or sets the time out for delay request. Defaults to 5 seconds.
		/// </summary>
		public TimeSpan Timeout { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Attaches the application to an existing process.
		/// </summary>
		/// <param name="executablePath"> The path to the executable. </param>
		/// <param name="arguments"> The arguments for the executable. Arguments are optional. </param>
		/// <param name="refresh"> The setting to determine to refresh children now. </param>
		/// <returns> The instance that represents the application. </returns>
		public static Application Attach(string executablePath, string arguments = null, bool refresh = true)
		{
			var fileName = Path.GetFileName(executablePath);
			if (fileName != null && !fileName.Contains("."))
			{
				fileName += ".exe";
			}

			var processName = Path.GetFileNameWithoutExtension(executablePath);
			var query = $"SELECT Handle, CommandLine FROM Win32_Process WHERE Name='{fileName}'";

			using (var searcher = new ManagementObjectSearcher(query))
			{
				foreach (var result in searcher.Get())
				{
					var managementObject = (ManagementObject) result;
					var handle = int.Parse(managementObject["Handle"].ToString());
					if (!string.IsNullOrWhiteSpace(arguments))
					{
						var data = managementObject["CommandLine"];
						if (data == null || !data.ToString().Contains(arguments))
						{
							continue;
						}
					}

					var process = Process.GetProcessesByName(processName).FirstOrDefault(x => x.Id == handle);
					if (process == null)
					{
						continue;
					}

					if (process.MainWindowHandle == IntPtr.Zero || !NativeMethods.IsWindowVisible(process.MainWindowHandle))
					{
						continue;
					}

					var application = new Application(process);
					if (!refresh)
					{
						return application;
					}

					application.Refresh();
					application.WaitWhileBusy();

					return application;
				}
			}

			return null;
		}

		/// <summary>
		/// Attaches the application to an existing process.
		/// </summary>
		/// <param name="handle"> The handle of the executable. </param>
		/// <param name="refresh"> The setting to determine to refresh children now. </param>
		/// <returns> The instance that represents the application. </returns>
		public static Application Attach(IntPtr handle, bool refresh = true)
		{
			var process = Process.GetProcesses().FirstOrDefault(x => x.MainWindowHandle == handle);
			if (process == null)
			{
				return null;
			}

			var application = new Application(process);
			if (!refresh)
			{
				return application;
			}

			application.Refresh();
			application.WaitWhileBusy();

			return application;
		}

		/// <summary>
		/// Attaches the application to an existing process.
		/// </summary>
		/// <param name="executablePath"> The path to the executable. </param>
		/// <param name="arguments"> The arguments for the executable. Arguments are optional. </param>
		/// <returns> The instance that represents the application. </returns>
		public static Application AttachOrCreate(string executablePath, string arguments = null)
		{
			return Attach(executablePath, arguments) ?? Create(executablePath, arguments);
		}

		/// <summary>
		/// Brings the application to the front and makes it the Top window.
		/// </summary>
		public void BringToFront()
		{
			NativeMethods.SetForegroundWindow(Handle);
			NativeMethods.BringWindowToTop(Handle);
		}

		/// <summary>
		/// Closes the window.
		/// </summary>
		public void Close()
		{
			if (Process.HasExited)
			{
				return;
			}

			Process.CloseMainWindow();
			if (!Process.WaitForExit(1500))
			{
				Process.Kill();
			}
		}

		/// <summary>
		/// Closes all windows my name and closes them.
		/// </summary>
		/// <param name="executablePath"> The path to the executable. </param>
		public static void CloseAll(string executablePath)
		{
			var processName = Path.GetFileNameWithoutExtension(executablePath);

			// Find all the main processes.
			var processes = Process.GetProcessesByName(processName)
				.Where(x => x.MainWindowHandle != IntPtr.Zero);

			processes.ForEachDisposable(process =>
			{
				// Ask to close the process nicely.
				process.CloseMainWindow();

				if (!process.WaitForExit(1000))
				{
					// The process did not close so now we are just going to kill it.
					process.Kill();
					process.WaitForExit();
				}
			});

			// Wait for the threads to sleep and child process to close.
			Thread.Sleep(50);

			// Find all the other processes.
			Process.GetProcessesByName(processName).ForEachDisposable(process =>
			{
				// The process did not close so now we are just going to kill it.
				process.Kill();
				process.WaitForExit();
			});
		}

		/// <summary>
		/// Creates a new application process.
		/// </summary>
		/// <param name="executablePath"> The path to the executable. </param>
		/// <param name="arguments"> The arguments for the executable. Arguments are optional. </param>
		/// <param name="refresh"> The flag to trigger loading to load state when creating the application. Defaults to true. </param>
		/// <returns> The instance that represents an application. </returns>
		public static Application Create(string executablePath, string arguments = null, bool refresh = true)
		{
			var processStartInfo = new ProcessStartInfo(executablePath);
			if (!string.IsNullOrWhiteSpace(arguments))
			{
				processStartInfo.Arguments = arguments;
			}

			var process = Process.Start(processStartInfo);
			if (process == null)
			{
				throw new InvalidOperationException("Failed to start the application.");
			}

			process.WaitForInputIdle();
			var application = new Application(process);

			if (refresh)
			{
				application.Refresh();
				application.WaitWhileBusy();
			}

			return application;
		}

		/// <summary>
		/// Gets a list of structure elements into a single collection.
		/// </summary>
		/// <returns> A collection of the items. </returns>
		public IEnumerable<Element> Descendants()
		{
			var nodes = new Stack<Element>(Children);
			while (nodes.Any())
			{
				var node = nodes.Pop();
				yield return node;
				foreach (var n in node.Children)
				{
					nodes.Push(n);
				}
			}
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Checks to see if an application process exist by path and optional arguments.
		/// </summary>
		/// <param name="executablePath"> The path to the executable. </param>
		/// <param name="arguments"> The arguments for the executable. Arguments are optional. </param>
		/// <returns> True if the application exists and false otherwise. </returns>
		public static bool Exists(string executablePath, string arguments = null)
		{
			var fileName = Path.GetFileName(executablePath);
			var processName = Path.GetFileNameWithoutExtension(executablePath);
			var query = $"SELECT Handle, CommandLine FROM Win32_Process WHERE Name='{fileName}'";

			using (var searcher = new ManagementObjectSearcher(query))
			{
				foreach (var result in searcher.Get())
				{
					var managementObject = (ManagementObject) result;
					var handle = int.Parse(managementObject["Handle"].ToString());
					if (!string.IsNullOrWhiteSpace(arguments))
					{
						var data = managementObject["CommandLine"];
						if (data == null || !data.ToString().Contains(arguments))
						{
							continue;
						}
					}

					using (var process = Process.GetProcessesByName(processName).FirstOrDefault(x => x.Id == handle))
					{
						if (process == null)
						{
							continue;
						}

						if (process.MainWindowHandle == IntPtr.Zero || !NativeMethods.IsWindowVisible(process.MainWindowHandle))
						{
							continue;
						}

						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Get a child of a certain type and key.
		/// </summary>
		/// <typeparam name="T"> The type of the child. </typeparam>
		/// <param name="key"> The key of the child. </param>
		/// <param name="includeDescendants"> Flag to determine to include descendants or not. </param>
		/// <returns> The child if found or null if otherwise. </returns>
		public T GetChild<T>(string key, bool includeDescendants = true) where T : Element, IElementParent
		{
			return (T) Children.GetChild(key, includeDescendants);
		}

		/// <summary>
		/// Get a child of a certain type that meets the condition.
		/// </summary>
		/// <typeparam name="T"> The type of the child. </typeparam>
		/// <param name="condition"> A function to test each element for a condition. </param>
		/// <param name="includeDescendants"> Flag to determine to include descendants or not. </param>
		/// <returns> The child if found or null if otherwise. </returns>
		public T GetChild<T>(Func<T, bool> condition, bool includeDescendants = true) where T : Element, IElementParent
		{
			return Children.GetChild(condition, includeDescendants);
		}

		/// <summary>
		/// Returns a value indicating if the windows is in front of all other windows.
		/// </summary>
		/// <returns> </returns>
		public bool IsInFront()
		{
			var handle = NativeMethods.GetForegroundWindow();
			return handle == Process.MainWindowHandle;
		}

		/// <summary>
		/// Move the window and resize it.
		/// </summary>
		/// <param name="x"> The x coordinate to move to. </param>
		/// <param name="y"> The y coordinate to move to. </param>
		/// <param name="width"> The width of the window. </param>
		/// <param name="height"> The height of the window. </param>
		public void MoveWindow(int x, int y, int width, int height)
		{
			NativeMethods.MoveWindow(Handle, x, y, width, height, true);
		}

		/// <summary>
		/// Refresh the list of items for the application.
		/// </summary>
		public void Refresh()
		{
			try
			{
				Utility.Wait(() =>
				{
					Children.Clear();
					Children.AddRange(Process.GetWindows().Select(x => new Window(x, this)));
					return Children.Any();
				}, Timeout.TotalMilliseconds, 10);

				WaitWhileBusy();
				Children.ForEach(x => x.UpdateChildren());
				WaitWhileBusy();
			}
			catch (Exception)
			{
				// todo: we need to get the real exception type.
				Debugger.Break();

				// A window close while trying to enumerate it. Wait for a second then try again.
				Thread.Sleep(250);
				Refresh();
			}
		}

		/// <summary>
		/// Update the children for this element.
		/// </summary>
		public void UpdateChildren()
		{
			Refresh();
			OnChildrenUpdated();
		}

		/// <summary>
		/// Wait for the child to be available then return it.
		/// </summary>
		/// <param name="id"> The ID of the child to wait for. </param>
		/// <param name="includeDescendants"> Flag to determine to include descendants or not. </param>
		/// <returns> The child element for the ID. </returns>
		public Element WaitForChild(string id, bool includeDescendants = true)
		{
			return WaitForChild<Element>(id, includeDescendants);
		}

		/// <summary>
		/// Wait for the child to be available then return it.
		/// </summary>
		/// <param name="id"> The ID of the child to wait for. </param>
		/// <param name="includeDescendants"> Flag to determine to include descendants or not. </param>
		/// <returns> The child element for the ID. </returns>
		public T WaitForChild<T>(string id, bool includeDescendants = true) where T : Element
		{
			T response = null;

			Utility.Wait(() =>
			{
				response = GetChild<T>(id, includeDescendants);
				if (response != null)
				{
					return true;
				}

				UpdateChildren();
				return false;
			}, Timeout.TotalMilliseconds, 10);

			if (response == null)
			{
				throw new ArgumentException("Failed to find the child by ID.");
			}

			return response;
		}

		/// <summary>
		/// Wait for the child to be available and meet the condition then return it.
		/// </summary>
		/// <param name="condition"> A function to test each element for a condition. </param>
		/// <param name="includeDescendants"> Flag to determine to include descendants or not. </param>
		/// <returns> The child element for the ID. </returns>
		public T WaitForChild<T>(Func<T, bool> condition, bool includeDescendants = true) where T : Element
		{
			T response = null;

			Utility.Wait(() =>
			{
				response = GetChild(condition, includeDescendants);
				if (response != null)
				{
					return true;
				}

				UpdateChildren();
				return false;
			}, Timeout.TotalMilliseconds, 10);

			if (response == null)
			{
				throw new ArgumentException("Failed to find the child by ID.");
			}

			return response;
		}

		/// <summary>
		/// Waits for the Process to not be busy.
		/// </summary>
		/// <param name="minimumDelay"> The minimum delay in milliseconds to wait. Defaults to 0 milliseconds. </param>
		public void WaitWhileBusy(int minimumDelay = 0)
		{
			var watch = Stopwatch.StartNew();
			Process.WaitForInputIdle(Timeout.Milliseconds);

			while (watch.Elapsed.TotalMilliseconds < minimumDelay && minimumDelay > 0)
			{
				Thread.Sleep(10);
			}
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// <param name="disposing"> True if disposing and false if otherwise. </param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			if (AutoClose && Process != null && Process.HasExited)
			{
				Close();
			}

			if (Process != null)
			{
				Process.Dispose();
				Process = null;
			}
		}

		/// <summary>
		/// Handles the children updated event.
		/// </summary>
		protected virtual void OnChildrenUpdated()
		{
			ChildrenUpdated?.Invoke();
		}

		/// <summary>
		/// Handles the closed event.
		/// </summary>
		protected virtual void OnClosed()
		{
			Closed?.Invoke();
		}

		/// <summary>
		/// Handles the excited event.
		/// </summary>
		/// <param name="sender"> </param>
		/// <param name="e"> </param>
		private void OnExited(object sender, EventArgs e)
		{
			Exited?.Invoke();
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the children are updated.
		/// </summary>
		public event Action ChildrenUpdated;

		/// <summary>
		/// Event called when the application process closes.
		/// </summary>
		public event Action Closed;

		/// <summary>
		/// Occurs when the application exits.
		/// </summary>
		public event Action Exited;

		#endregion
	}
}