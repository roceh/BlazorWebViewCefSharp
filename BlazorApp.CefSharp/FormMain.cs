using System;
using System.Windows.Forms;
using BlazorWebView;

namespace BlazorApp.CefSharp
{
    public partial class FormMain : Form
    {
        private IDisposable _run;
        private BlazorWebView.CefSharp.BlazorWebView _browser;

        public FormMain()
        {
            InitializeComponent();

            _browser = new BlazorWebView.CefSharp.BlazorWebView();
            _browser.Dock = DockStyle.Fill;

            Controls.Add(_browser);
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            _run = BlazorWebViewHost.Run<StartupCefSharp>(_browser, "wwwroot/index.html");
        }

        private void FormMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            _run.Dispose();
        }
    }
}
