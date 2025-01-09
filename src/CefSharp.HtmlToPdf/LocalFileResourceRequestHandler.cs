using CefSharp.Handler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefSharp.HtmlToPdf
{
    internal class LocalFileResourceRequestHandler : ResourceRequestHandler
    {
        private string _filePath;

        public LocalFileResourceRequestHandler(string filePath)
        {
            _filePath = filePath;
        }

        protected override IResourceHandler GetResourceHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request)
        {
            if (File.Exists(_filePath))
                return ResourceHandler.FromFilePath(_filePath);
            else
                return ResourceHandler.ForErrorMessage("File Not Found.", System.Net.HttpStatusCode.NotFound);
        }
    }
}
