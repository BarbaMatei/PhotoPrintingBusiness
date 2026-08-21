namespace PhotoPrint.API.Services.Invoicing;

// The base class keeps the guards' old contract; the type is what tells the worker no retry can repair the row.
public sealed class InvoiceNotBuildableException : InvalidOperationException
{
    public InvoiceNotBuildableException(string message) : base(message) { }
}
