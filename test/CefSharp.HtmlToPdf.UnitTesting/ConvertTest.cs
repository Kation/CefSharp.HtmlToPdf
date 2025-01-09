using CefSharp.OffScreen;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace CefSharp.HtmlToPdf.UnitTesting
{
    [TestClass]
    public sealed class ConvertTest
    {
        [TestMethod]
        public async Task PdfConverterTest()
        {
            using PdfConverter converter = new PdfConverter();
            converter.RootDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "resources";
            var html = await File.ReadAllTextAsync("page.html");
            var pdf = await converter.ConvertAsync(html);
            await File.WriteAllBytesAsync("page.pdf", pdf);
        }

        [TestMethod]
        public async Task ConcurrentPdfConverterTest()
        {
            using ConcurrentPdfConverter converter = new ConcurrentPdfConverter();
            converter.RootDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "resources";
            var html = await File.ReadAllTextAsync("page.html");

            Parallel.ForAsync(0, 100, async (i, cts) =>
            {
                await converter.ConvertAsync(html);
            }).Wait();
        }
    }
}
