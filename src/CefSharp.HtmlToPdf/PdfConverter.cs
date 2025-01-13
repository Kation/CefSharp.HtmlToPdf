using CefSharp.Handler;
using CefSharp.OffScreen;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CefSharp.HtmlToPdf
{
    /// <summary>
    /// Pdf Converter.<br/>
    /// Concurrent convert will queue to wait.
    /// </summary>
    public class PdfConverter : IPdfConverter, IDisposable
    {
        private CefContext? _cefContext;
        private ChromiumWebBrowser _browser;
        private SemaphoreSlim _semaphore;
        private bool _disposed;

        /// <summary>
        /// PdfConverter constructor.
        /// </summary>
        /// <param name="initCef">Initialize Cef when create pdf converter.<br/>
        /// Use false if Cef have been used elsewhere.</param>
        /// <param name="settings">Cef settings. Used when initCef is true.</param>
        /// <param name="browserSettings">Cef browser settings.<br/>
        /// Default settings will disable Javascript, Databases, LocalStorage, WebGl.</param>
        public PdfConverter(bool initCef = true, CefSettings? settings = null, BrowserSettings? browserSettings = null)
        {
            if (initCef)
            {
                settings ??= new CefSettings
                {
                    CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache"),
                    LogSeverity = LogSeverity.Verbose
                };
                _cefContext = new CefContext(settings);
                _cefContext.Initialize();
            }
            browserSettings ??= new BrowserSettings
            {
                Javascript = CefState.Disabled,
                Databases = CefState.Disabled,
                LocalStorage = CefState.Disabled,
                WebGl = CefState.Disabled
            };
            _browser = new ChromiumWebBrowser(browserSettings: browserSettings);
            _browser.RequestHandler = new PdfConverterRequestHandler(this);
            _semaphore = new SemaphoreSlim(1);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string? RootDirectory { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Task<byte[]> ConvertAsync(string html, PdfPrintSettings? printSettings = null, CancellationToken cancellationToken = default)
        {
            return ConvertAsync(Encoding.UTF8.GetBytes(html), printSettings, cancellationToken);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task<byte[]> ConvertAsync(byte[] html, PdfPrintSettings? printSettings = null, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PdfConverter));
            if (html == null)
                throw new ArgumentNullException(nameof(html));
            if (html.Length == 0)
                throw new ArgumentException("Html content can't be null.", nameof(html));
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                PdfConverterRequestHandler requestHandler = (PdfConverterRequestHandler)_browser.RequestHandler;
                requestHandler.Html = html;
                _browser.Load("local://html/");
                await _browser.WaitForInitialLoadAsync();
                requestHandler.Html = null;

                return await PrintToPdfAsync(printSettings, cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<byte[]> PrintToPdfAsync(PdfPrintSettings? printSettings, CancellationToken cancellationToken)
        {
            var tempFile = Path.GetTempFileName();
            await _browser.PrintToPdfAsync(tempFile, printSettings);
            byte[] data;
            try
            {
#if NET6_0_OR_GREATER
                data = await File.ReadAllBytesAsync(tempFile, cancellationToken);
#else
                using var stream = File.OpenRead(tempFile);
                data = new byte[stream.Length];
                await stream.ReadAsync(data, 0, data.Length);
#endif
            }
            finally
            {
                File.Delete(tempFile);
            }
            return data;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        _browser.GetBrowser().CloseBrowser(true);
                    }
                    catch { }
                    _browser.Dispose();
                }
                if (_cefContext != null)
                {
                    _cefContext.Shutdown();
                    _cefContext = null;
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Dispose PdfConverter.<br/>
        /// Cef will be shutdown if Cef initialized by PdfConverter.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~PdfConverter()
        {
            Dispose(disposing: false);
        }
    }
}
