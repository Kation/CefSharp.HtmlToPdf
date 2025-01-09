using CefSharp.OffScreen;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefSharp.HtmlToPdf
{
    internal class CefContext : IDisposable
    {
        private ManualResetEvent _cefEvent, _waitEvent;
        private CefSettings _settings;
        private bool _disposed, _initialized;
        private Exception? _exception;

        public CefContext(CefSettings settings)
        {
            _cefEvent = new ManualResetEvent(false);
            _waitEvent = new ManualResetEvent(false);
            _settings = settings;
        }

        private void CefThread()
        {
            try
            {
                _initialized = Cef.Initialize(_settings, performDependencyCheck: true, browserProcessHandler: null);
            }
            catch (Exception ex)
            {
                _exception = ex;
                _waitEvent.Set();
                return;
            }
            _exception = null;
            _waitEvent.Set();
            _cefEvent.Reset();
            _cefEvent.WaitOne();
            Cef.Shutdown();
            _initialized = false;
            _waitEvent.Set();
        }

        public void Initialize()
        {
            if (_initialized)
                throw new InvalidOperationException("Cef is initialized.");
            var thread = new Thread(CefThread);
            _waitEvent.Reset();
            thread.Start();
            _waitEvent.WaitOne();
            if (_exception != null)
                throw _exception;
            if (!_initialized)
                throw new Exception("Unable to initialize CEF, check the log file.");
        }

        public void Shutdown()
        {
            if (!_initialized)
                throw new InvalidOperationException("Cef is not initialized.");
            _waitEvent.Reset();
            _cefEvent.Set();
            _waitEvent.WaitOne();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                }
                if (_initialized)
                    Shutdown();
                _disposed = true;
            }
        }

        ~CefContext()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
