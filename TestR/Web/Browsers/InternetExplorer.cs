﻿#region References

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using mshtml;
using SHDocVw;
using TestR.Desktop;
using TestR.Helpers;
using TestR.Logging;

#endregion

namespace TestR.Web.Browsers
{
	/// <summary>
	/// Represents an Internet Explorer browser.
	/// </summary>
	public class InternetExplorer : Browser
	{
		#region Constants

		/// <summary>
		/// The name of the browser.
		/// </summary>
		public const string Name = "iexplore";

		#endregion

		#region Fields

		private SHDocVw.InternetExplorer _browser;

		#endregion

		#region Constructors

		private InternetExplorer(SHDocVw.InternetExplorer browser)
			: base(Application.Attach(new IntPtr(browser.HWND), false))
		{
			_browser = browser;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the type of the browser.
		/// </summary>
		public override BrowserType BrowserType
		{
			get { return BrowserType.InternetExplorer; }
		}

		/// <summary>
		/// Gets the ID of the browser.
		/// </summary>
		public override int Id
		{
			get { return Application.Handle.ToInt32(); }
		}

		/// <summary>
		/// Gets the raw HTML of the page.
		/// </summary>
		public override string RawHtml
		{
			get { return ((HTMLDocument) _browser.Document).documentElement.outerHTML; }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Attempts to attach to an existing browser.
		/// </summary>
		/// <returns> An instance of an Internet Explorer browser. </returns>
		public static InternetExplorer Attach()
		{
			var foundBrowser = GetBrowserToAttachTo();
			if (foundBrowser == null)
			{
				return null;
			}

			var browser = new InternetExplorer(foundBrowser);
			browser.Refresh();

			return browser;
		}

		/// <summary>
		/// Attempts to attach to an existing browser. If one is not found then create and return a new one.
		/// </summary>
		/// <returns> An instance of an Internet Explorer browser. </returns>
		public static InternetExplorer AttachOrCreate()
		{
			return Attach() ?? Create();
		}

		/// <summary>
		/// Clears the cookies for the provided URL. If the URL is an empty string then all cookies will be cleared.
		/// </summary>
		/// <param name="url"> The URL of the cookies to remove. Empty string removes all cookies. </param>
		/// <exception cref="ArgumentNullException"> The URL parameter cannot be null. </exception>
		public static void ClearCookies(string url = "")
		{
			if (url == null)
			{
				throw new ArgumentNullException("url");
			}

			var path = Environment.GetFolderPath(Environment.SpecialFolder.Cookies);
			var files = Directory.GetFiles(path)
				.Union(Directory.GetFiles(path + "\\low"))
				.ToList();

			if (string.IsNullOrWhiteSpace(url))
			{
				files.ForEach(File.Delete);
				return;
			}

			foreach (var file in files)
			{
				var text = File.ReadAllText(file);
				if (!text.Contains(url))
				{
					continue;
				}

				File.Delete(file);
			}
		}

		/// <summary>
		/// Creates a new instance of an Internet Explorer browser.
		/// </summary>
		/// <returns> An instance of an Internet Explorer browser. </returns>
		public static InternetExplorer Create()
		{
			return new InternetExplorer(CreateInternetExplorerClass());
		}

		/// <summary>
		/// Navigates the browser to the provided URI.
		/// </summary>
		/// <param name="uri"> The URI to navigate to. </param>
		protected override void BrowserNavigateTo(string uri)
		{
			try
			{
				LogManager.Write("InternetExplorer navigated to " + uri + ".", LogLevel.Verbose);

				object nil = null;
				object absoluteUri = uri;
				_browser.Navigate2(ref absoluteUri, ref nil, ref nil, ref nil, ref nil);
				WaitForComplete();

				var htmlDocument = _browser.Document as IHTMLDocument2;
				if (htmlDocument == null)
				{
					throw new Exception("Failed to run script because no document is loaded.");
				}

				Thread.Sleep(50);
			}
			catch (COMException)
			{
				// A COMException with "RPC_E_DISCONNECTED" happens when the browser switches security context.
				Thread.Sleep(150);
				ReinitializeBrowser();
				BrowserNavigateTo(uri);
			}
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// <param name="disposing"> True if disposing and false if otherwise. </param>
		protected override void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			try
			{
				if (_browser == null || !AutoClose)
				{
					return;
				}

				WaitForComplete();

				// We cannot allow the browser to close within a second.
				// I assume that add-on(s) need time to start before closing the browser.
				var timeout = TimeSpan.FromMilliseconds(1000);
				var watch = Stopwatch.StartNew();

				while (watch.Elapsed <= timeout)
				{
					Thread.Sleep(50);
				}

				_browser.Quit();
			}
			catch
			{
				_browser = null;
			}
		}

		/// <summary>
		/// Execute JavaScript code in the current document.
		/// </summary>
		/// <param name="script"> The code script to execute. </param>
		/// <param name="expectResponse"> The script will return response. </param>
		/// <returns> The response from the execution. </returns>
		protected override string ExecuteJavaScript(string script, bool expectResponse = true)
		{
			LogManager.Write("Request: " + script, LogLevel.Verbose);

			// If the URL is empty and is not initialized means browser is no page is loaded.
			if (!_browser.LocationURL.StartsWith("http", StringComparison.OrdinalIgnoreCase) || _browser.ReadyState == tagREADYSTATE.READYSTATE_UNINITIALIZED)
			{
				return string.Empty;
			}

			var document = _browser.Document as IHTMLDocument2;
			if (document == null)
			{
				throw new Exception("Failed to run script because no document is loaded.");
			}

			try
			{
				// See if we are injecting the test script.
				if (script.Contains("var TestR=TestR") || script.Contains("var TestR = TestR"))
				{
					document.parentWindow.execScript(script, "javascript");
					return "Injected TestR Script";
				}

				// Run the script using TestR.
				var wrappedScript = "TestR.runScript('" + script.Replace("'", "\\'") + "');";
				document.parentWindow.execScript(wrappedScript, "javascript");

				return GetJavascriptResult((IHTMLDocument3) document);
			}
			catch (COMException ex)
			{
				if (ex.HResult == -2147352319)
				{
					throw new Exception("Invalid JavaScript request.", ex);
				}

				throw;
			}
		}

		/// <summary>
		/// Reads the current URI directly from the browser.
		/// </summary>
		/// <returns> The current URI that was read from the browser. </returns>
		protected override string GetBrowserUri()
		{
			LogManager.Write("Get browser's URI.", LogLevel.Verbose);
			return _browser.LocationURL;
		}

		/// <summary>
		/// Waits until the browser to complete any outstanding operations.
		/// </summary>
		protected override void WaitForComplete()
		{
			LogManager.Write("InterenetExploreBrowser.WaitForComplete", LogLevel.Verbose);

			// If the URL is empty and is not initialized means browser is no page is loaded.
			if (_browser.LocationURL == string.Empty && _browser.ReadyState == tagREADYSTATE.READYSTATE_UNINITIALIZED)
			{
				return;
			}

			var states = new[] { tagREADYSTATE.READYSTATE_COMPLETE, tagREADYSTATE.READYSTATE_INTERACTIVE };
			var readyStates = new[] { "complete", "interactive" };

			// Wait for browser to completely load or become interactive.
			if (!Utility.Wait(() => states.Contains(_browser.ReadyState), Timeout.TotalMilliseconds))
			{
				throw new Exception("The browser never finished loading...");
			}

			// Wait for browser document to completely load or become interactive.
			if (!Utility.Wait(() => readyStates.Contains(((IHTMLDocument2) _browser.Document).readyState), Timeout.TotalMilliseconds))
			{
				throw new Exception("The browser document never finished loading...");
			}

			// Wait while the browser is busy and not complete.
			if (!Utility.Wait(() => !(_browser.Busy && _browser.ReadyState != tagREADYSTATE.READYSTATE_COMPLETE), Timeout.TotalMilliseconds))
			{
				throw new Exception("The browser is currently busy.");
			}
		}

		/// <summary>
		/// Creates an instance of the InternetExplorer.
		/// </summary>
		/// <returns> An instance of Internet Explorer. </returns>
		private static SHDocVw.InternetExplorer CreateInternetExplorerClass()
		{
			try
			{
				var explorer = new InternetExplorerClass();
				explorer.Visible = true;
				return explorer;
			}
			catch (COMException)
			{
				return CreateInternetExplorerClass();
			}
		}

		private static SHDocVw.InternetExplorer GetBrowserToAttachTo(string host = null)
		{
			var explorers = new ShellWindowsClass()
				.Cast<SHDocVw.InternetExplorer>()
				.Where(x => x.FullName.ToLower().Contains("iexplore.exe"))
				.ToList();

			foreach (var explorer in explorers)
			{
				try
				{
					if (host != null && !(new Uri(explorer.LocationURL)).Host.Contains(host))
					{
						continue;
					}

					using (var browser = new InternetExplorer(explorer))
					{
						LogManager.Write("Found browser with id of " + browser.Id + " at location " + browser.Uri + ".", LogLevel.Verbose);
					}

					return explorer;
				}
				catch (Exception ex)
				{
					// Ignore this browser and move to the next one.
					LogManager.Write("Error with finding browser to attach to. Exception: " + ex.Message + ".", LogLevel.Verbose);
				}
			}

			return null;
		}

		private string GetJavascriptResult(IHTMLDocument3 document)
		{
			try
			{
				var resultElement = document.getElementById("testrResult");
				var result = resultElement.getAttribute("value");

				LogManager.Write("Response: " + result, LogLevel.Verbose);
				return result ?? string.Empty;
			}
			catch
			{
				// The document may have been redirected which means the member will not be there.
				return string.Empty;
			}
		}

		/// <summary>
		/// Disconnects from the current browser and finds the new instance.
		/// </summary>
		private void ReinitializeBrowser()
		{
			Application.Dispose();
			_browser = GetBrowserToAttachTo() ?? CreateInternetExplorerClass();
			Application = Application.Attach(new IntPtr(_browser.HWND), false);
		}

		#endregion
	}
}