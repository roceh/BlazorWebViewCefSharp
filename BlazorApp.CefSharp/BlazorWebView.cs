using BlazorWebView;
using CefSharp;
using CefSharp.Handler;
using CefSharp.WinForms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace BlazorApp.CefSharp
{
    public class BlazorWebView : Control, IBlazorWebView
    {
        private ChromiumWebBrowser _browser;
        private int _ownerThreadId;

        public class BrowserRequestHandler : IRequestHandler
        {
            private Dictionary<string, ResolveWebResourceDelegate> _schemeHandlers;

            private const string InitScriptSource =
                @"window.__receiveMessageCallbacks = [];
	 		 window.__dispatchMessageCallback = function(message) {
			 	window.__receiveMessageCallbacks.forEach(function(callback) { callback(message); });
			 };
			 window.external = {
			 	sendMessage: function(message) {
			 		CefSharp.PostMessage(message);
			 	},
			 	receiveMessage: function(callback) {
			 		window.__receiveMessageCallbacks.push(callback);
			 	}
			 };";

            public class CustomResourceRequestHandler : ResourceRequestHandler
            {
                private Stream _stream;
                private string _contentType;
                private string _charSet;

                public CustomResourceRequestHandler(Stream stream, string contentType, string charSet)
                {
                    _stream = stream;
                    _contentType = contentType;
                    _charSet = charSet;
                }

                protected override IResourceHandler GetResourceHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request)
                {
                    return ResourceHandler.FromStream(_stream, _contentType, true, _charSet);
                }
            }

            public BrowserRequestHandler(IDictionary<string, ResolveWebResourceDelegate> schemeHandlers)
            {
                _schemeHandlers = new Dictionary<string, ResolveWebResourceDelegate>(schemeHandlers);
            }

            public bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
            {
                return false;
            }

            public bool OnOpenUrlFromTab(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture)
            {
                return false;
            }

            public bool OnCertificateError(IWebBrowser browserControl, IBrowser browser, CefErrorCode errorCode, string requestUrl, ISslInfo sslInfo, IRequestCallback callback)
            {
                callback.Dispose();
                return false;
            }

            public void OnPluginCrashed(IWebBrowser browserControl, IBrowser browser, string pluginPath)
            {
            }

            public CefReturnValue OnBeforeResourceLoad(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
            {
                return CefReturnValue.Continue;
            }

            public bool GetAuthCredentials(IWebBrowser browserControl, IBrowser browser, IFrame frame, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback)
            {
                callback.Dispose();
                return false;
            }

            public bool OnSelectClientCertificate(IWebBrowser browserControl, IBrowser browser, bool isProxy, string host, int port, X509Certificate2Collection certificates, ISelectClientCertificateCallback callback)
            {
                callback.Dispose();
                return false;
            }

            public void OnRenderProcessTerminated(IWebBrowser browserControl, IBrowser browser, CefTerminationStatus status)
            {
            }

            public bool OnQuotaRequest(IWebBrowser browserControl, IBrowser browser, string originUrl, long newSize, IRequestCallback callback)
            {
                callback.Dispose();
                return false;
            }

            public void OnResourceRedirect(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response, ref string newUrl)
            {
                var url = newUrl;
                newUrl = url;
            }

            public bool OnProtocolExecution(IWebBrowser browserControl, IBrowser browser, string url)
            {
                return url.StartsWith("mailto");
            }

            public void OnRenderViewReady(IWebBrowser browserControl, IBrowser browser)
            {
            }

            public bool OnResourceResponse(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response)
            {
                return false;
            }

            public IResponseFilter GetResourceResponseFilter(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response)
            {
                return null;
            }

            public void OnResourceLoadComplete(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
            {
            }            

            public IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
            {
                Uri uri = new Uri(request.Url);

                if (_schemeHandlers.TryGetValue(uri.Scheme, out var handler))
                {
                    // handle the scheme and url by executing the handler.
                    var stream = handler(request.Url, out string contentType, out Encoding encoding);

                    if (stream != null)
                    {
                        if (uri.Scheme == "framework")
                        {
                            var memoryStream = new MemoryStream();
                            var buffer = encoding.GetBytes(InitScriptSource);
                            memoryStream.Write(buffer, 0, buffer.Length);
                            stream.CopyTo(memoryStream);
                            stream.Dispose();
                            memoryStream.Position = 0;
                            stream = memoryStream;
                        }

                        return new CustomResourceRequestHandler(stream, contentType, encoding.WebName);
                    }
                }

                return null;
            }

            public bool GetAuthCredentials(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback)
            {
                return false;
            }
        }

        public class CustomMenuHandler : IContextMenuHandler
        {
            public void OnBeforeContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
            {
                model.Clear();
            }

            public bool OnContextMenuCommand(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
            {
                return false;
            }

            public void OnContextMenuDismissed(IWebBrowser browserControl, IBrowser browser, IFrame frame)
            {

            }

            public bool RunContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
            {
                return false;
            }
        }

        public BlazorWebView()
        {
        }

        public event EventHandler<string> OnWebMessageReceived;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_browser != null)
                {
                    _browser.Dispose();
                    _browser = null;
                }
            }

            base.Dispose(disposing);
        }


        public void Initialize(Action<WebViewOptions> configure)
        {
            _ownerThreadId = Thread.CurrentThread.ManagedThreadId;

            var options = new WebViewOptions();
            configure.Invoke(options);

            _browser = new ChromiumWebBrowser("");
            _browser.RequestHandler = new BrowserRequestHandler(options.SchemeHandlers);
            _browser.MenuHandler = new CustomMenuHandler();
            _browser.JavascriptMessageReceived += Browser_JavascriptMessageReceived;
            _browser.Dock = DockStyle.Fill;
            Controls.Add(_browser);
        }

        public void ShowDevTools()
        {
            if (_browser.IsBrowserInitialized)
            {
                _browser.ShowDevTools();
            }
        }

        public void Invoke(Action callback)
        {
            if (Thread.CurrentThread.ManagedThreadId == _ownerThreadId)
            {
                callback();
            }
            else
            {
                base.Invoke(callback);
            }
        }

        public void SendMessage(string message)
        {
            message = JsonConvert.ToString(message);
            _browser.EvaluateScriptAsync($"__dispatchMessageCallback({message})");
        }

        public void NavigateToUrl(string url)
        {
            _browser.Load(url);
        }

        public void ShowMessage(string title, string message)
        {
            Invoke(() =>
            {
                MessageBox.Show(message, title);
            });
        }

        private void Browser_JavascriptMessageReceived(object sender, JavascriptMessageReceivedEventArgs e)
        {
            OnWebMessageReceived.Invoke(this, (string)e.Message);
        }
    }    
}
