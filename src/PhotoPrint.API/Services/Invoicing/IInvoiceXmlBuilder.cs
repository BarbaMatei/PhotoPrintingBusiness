using PhotoPrint.API.Configuration;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services.Invoicing;

/// <summary>
/// Pure builder for the UBL 2.1 + CIUS-RO XML body of an <see cref="Invoice"/>.
/// No DI besides <see cref="SellerSettings"/>; no DB, no I/O, no logger.
/// </summary>
public interface IInvoiceXmlBuilder
{
    /// <summary>
    /// Returns a UTF-8 XML byte array. The same bytes are persisted to
    /// <c>Invoice.XmlPayload</c> and POSTed to ANAF SPV without
    /// re-serialisation. Throws <see cref="InvalidOperationException"/>
    /// when the order has zero items.
    /// </summary>
    byte[] Build(Order order, Invoice invoice, SellerSettings seller);
}
