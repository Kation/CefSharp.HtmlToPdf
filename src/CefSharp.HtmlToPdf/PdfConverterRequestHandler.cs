using CefSharp.Handler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefSharp.HtmlToPdf
{
    internal class PdfConverterRequestHandler : RequestHandler
    {
        private readonly IPdfConverter _converter;

        public PdfConverterRequestHandler(IPdfConverter converter)
        {
            _converter = converter;
        }

        public byte[]? Html { get; set; }

        protected override IResourceRequestHandler? GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        {
            if (request.Url == "local://html/")
                return new ByteResourceRequestHandler(Html ?? Array.Empty<byte>());
            try
            {
                var uri = new Uri(request.Url);
                if (uri.Scheme == "local" && uri.AbsolutePath.Length > 1)
                {
                    return new LocalFileResourceRequestHandler(Path.Combine(_converter.RootDirectory ?? Directory.GetCurrentDirectory(), uri.AbsolutePath.Substring(1).Replace('/', Path.DirectorySeparatorChar)));
                }
            }
            catch
            {
                return null;
            }
            return null;
        }
    }
}
