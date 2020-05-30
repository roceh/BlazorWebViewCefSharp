using BlazorApp.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace BlazorApp.CefSharp.Services
{
    public class WindowService : IWindowService
    {
        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }
    }
}
