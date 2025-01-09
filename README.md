# Usage
Create a `PdfConverter` or `ConcurrentPdfConverter` then use `ConvertAsync` method to convert html to pdf.

Both `PdfConverter` and `ConcurrentPdfConverter` can manage Cef lifecycle.
**DO NOT** use converter to manage Cef lifecycle if your application uses Cef elsewhere.
When use converter to manage Cef lifecycle, you can only create **one** converter.

You should alse see [how to setup CefSharp](https://github.com/cefsharp/CefSharp/wiki/Quick-Start-For-MS-.Net-5.0-or-greater) for your application project.  
You should reference `CefSharp.Common.NETCore` if Cef missing pak dependencies.