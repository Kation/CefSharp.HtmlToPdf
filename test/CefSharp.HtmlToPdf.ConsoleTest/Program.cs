// See https://aka.ms/new-console-template for more information

using CefSharp.HtmlToPdf;
using System.Drawing.Imaging;
using System.Drawing.Printing;

ConcurrentPdfConverter converter = new ConcurrentPdfConverter();
converter.RootDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "resources";
var html = File.ReadAllText("page.html");
//var pdf = await converter.ConvertAsync(html);
//await File.WriteAllBytesAsync("page.pdf", pdf);

//bool cancelled = false;
//ManualResetEvent eventSet = new ManualResetEvent(false);
//var task = Task.Run(async () =>
//{
//    int count = 0;
//    while (!cancelled)
//    {
//        await converter.ConvertAsync(html);
//        count++;
//        Console.WriteLine(count);
//    }
//    eventSet.Set();
//    eventSet.Reset();
//    eventSet.WaitOne();
//});
//Console.ReadLine();
//cancelled = true;
//eventSet.WaitOne();
//Console.WriteLine("转换已停止");
//Console.ReadLine();
//eventSet.Set();
////await disposeTcs.Task;
////thread.Join();
//task.Wait();
//converter.Dispose();
//Console.WriteLine("转换器已释放");
//Console.ReadLine();


await Parallel.ForAsync(0, 100, async (i, cts) =>
{
    await converter.ConvertAsync(html);
    Console.WriteLine(i);
});
Console.ReadLine();
converter.Dispose();
Console.ReadLine();