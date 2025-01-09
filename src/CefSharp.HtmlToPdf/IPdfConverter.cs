using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefSharp.HtmlToPdf
{
    /// <summary>
    /// Pdf converter.
    /// </summary>
    public interface IPdfConverter : IDisposable
    {
        /// <summary>
        /// Root directory of resources about to download relative resource in html.<br/>
        /// Default is null, use <see cref="System.IO.Directory.GetCurrentDirectory()">CurrentDirectory</see> instead.
        /// </summary>
        string? RootDirectory { get; set; }

        /// <summary>
        /// Convert html to pdf.<br/>
        /// Thread safty.<br/>
        /// PdfPrintSettings parameters usage: <see href="https://chromedevtools.github.io/devtools-protocol/tot/Page/#method-printToPDF">link</see>.
        /// </summary>
        /// <param name="html">Html string.</param>
        /// <param name="printSettings">
        /// Print settings.
        /// </param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <returns>Return pdf bytes.</returns>
        Task<byte[]> ConvertAsync(string html, PdfPrintSettings? printSettings = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Convert html to pdf.<br/>
        /// Thread safty.<br/>
        /// PdfPrintSettings parameters usage: <see href="https://chromedevtools.github.io/devtools-protocol/tot/Page/#method-printToPDF">link</see>.
        /// </summary>
        /// <param name="html">Html UTF-8 encoding bytes.</param>
        /// <param name="printSettings">
        /// Print settings.
        /// </param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <returns>Return pdf bytes.</returns>
        Task<byte[]> ConvertAsync(byte[] html, PdfPrintSettings? printSettings = null, CancellationToken cancellationToken = default);
    }
}
