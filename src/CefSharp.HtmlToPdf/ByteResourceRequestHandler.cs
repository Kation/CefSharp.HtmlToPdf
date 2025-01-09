using CefSharp.Handler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefSharp.HtmlToPdf
{
    internal class ByteResourceRequestHandler : ResourceRequestHandler
    {
        private byte[] _data;

        public ByteResourceRequestHandler(byte[] data)
        {
            _data = data;
        }

        protected override IResourceHandler GetResourceHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request)
        {
            return ResourceHandler.FromByteArray(_data, mimeType: "text/html");
        }
    }
}
