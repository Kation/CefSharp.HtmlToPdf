using CefSharp.Handler;
using CefSharp.OffScreen;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CefSharp.HtmlToPdf
{
    /// <summary>
    /// Concurrent Pdf Converter.
    /// </summary>
    public class ConcurrentPdfConverter : IPdfConverter
    {
        private CefContext? _cefContext;
        private BrowserSettings _browserSettings;
        private ConcurrentStack<ChromiumWebBrowser> _browsers;
        private bool _disposed;
        private DateTime _lastConvertTime;
        private SemaphoreSlim _semaphore;
        private int _converting;
        private AutoResetEvent _resetEvent;
        private CancellationTokenSource _cts;

        /// <summary>
        /// ConcurrentPdfConverter constructor.
        /// </summary>
        /// <param name="initCef">Initialize Cef when create pdf converter.<br/>
        /// Use false if Cef have been used elsewhere.</param>
        /// <param name="settings">Cef settings. Used when initCef is true.</param>
        /// <param name="browserSettings">Cef browser settings.<br/>
        /// Default settings will disable Javascript, Databases, LocalStorage, WebGl.</param>
        public ConcurrentPdfConverter(bool initCef = true, CefSettings? settings = null, BrowserSettings? browserSettings = null)
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
            _browserSettings = browserSettings ?? new BrowserSettings
            {
                Javascript = CefState.Disabled,
                Databases = CefState.Disabled,
                LocalStorage = CefState.Disabled,
                WebGl = CefState.Disabled
            };
            _browsers = new ConcurrentStack<ChromiumWebBrowser>();
            _semaphore = new SemaphoreSlim(1);
            _resetEvent = new AutoResetEvent(false);
            _cts = new CancellationTokenSource();
            _lastConvertTime = DateTime.Now;
            Task.Run(ReleaseLoop);
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
                throw new ObjectDisposedException(nameof(ConcurrentPdfConverter));
            if (html == null)
                throw new ArgumentNullException(nameof(html));
            if (html.Length == 0)
                throw new ArgumentException("Html content can't be null.", nameof(html));
            await _semaphore.WaitAsync(cancellationToken);
            _converting++;
            _semaphore.Release();
            byte[] data;
            try
            {
                _lastConvertTime = DateTime.Now;
                if (!_browsers.TryPop(out var browser))
                {
                    browser = new ChromiumWebBrowser(browserSettings: _browserSettings);
                    browser.RequestHandler = new PdfConverterRequestHandler(this);
                }
                PdfConverterRequestHandler requestHandler = (PdfConverterRequestHandler)browser.RequestHandler;
                try
                {
                    requestHandler.Html = html;
                    browser.Load("local://html/");
                    await browser.WaitForInitialLoadAsync();

                    data = await PrintToPdfAsync(browser, printSettings, cancellationToken);
                }
                finally
                {
                    requestHandler.Html = null;
                    _browsers.Push(browser);
                }
            }
            finally
            {
                await _semaphore.WaitAsync(cancellationToken);
                _converting--;
                _semaphore.Release();
                _resetEvent.Set();
            }
            return data;
        }

        private async Task<byte[]> PrintToPdfAsync(ChromiumWebBrowser browser, PdfPrintSettings? printSettings, CancellationToken cancellationToken)
        {
            var tempFile = Path.GetTempFileName();
            await browser.PrintToPdfAsync(tempFile, printSettings);
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
                //_cts maybe null if constructor throw exception
                if (_cts != null)
                    _cts.Cancel();
                if (disposing)
                {
                    while (true)
                    {
                        _semaphore.Wait();
                        var converting = _converting;
                        _semaphore.Release();
                        if (converting == 0)
                            break;
                        _resetEvent.WaitOne();
                    }
                    while (_browsers.TryPop(out var browser))
                        browser.Dispose();
                }
                if (_cefContext != null)
                {
                    _cefContext.Shutdown();
                    _cefContext.Dispose();
                    _cefContext = null;
                }
                _disposed = true;
            }
        }

        private async Task ReleaseLoop()
        {
            while (!_disposed)
            {
                if ((DateTime.Now - _lastConvertTime).TotalMinutes > 1)
                {
                    while (_browsers.TryPop(out var browser))
                    {
                        try
                        {
                            browser.GetBrowser().CloseBrowser(true);
                        }
                        catch { }
                        browser.Dispose();
                    }
                }
                try
                {
                    await Task.Delay(60000, _cts.Token);
                }
                catch
                {
                    return;
                }
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~ConcurrentPdfConverter()
        {
            Dispose(disposing: false);
        }
    }
}
