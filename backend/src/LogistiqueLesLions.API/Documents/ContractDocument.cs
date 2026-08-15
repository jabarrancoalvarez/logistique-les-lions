using System.Globalization;
using LogistiqueLesLions.Application.Features.Negotiations;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LogistiqueLesLions.API.Documents;

/// <summary>
/// «Contrat de vente» descargable en PDF.
/// </summary>
/// <remarks>
/// Se compone a partir de los datos congelados del contrato, así que el documento sale
/// idéntico cada vez que se genera. Por eso no se almacena el fichero: el contrato en la
/// base de datos <b>es</b> el archivo histórico.
/// </remarks>
public class ContractDocument(ContractDocumentDto contract, string verificationUrl) : IDocument
{
    /// <summary>Azul profundo de la marca.</summary>
    private const string Navy = "#0A2E4D";
    private const string Azure = "#157FA8";

    /// <summary>Los importes van en FCFA con separador de millar: 8.300.000 FCFA.</summary>
    private static string Fcfa(decimal amount) =>
        $"{amount.ToString("N0", CultureInfo.GetCultureInfo("de-DE"))} FCFA";

    private static string Date(DateTimeOffset value) => value.ToString("dd/MM/yyyy");

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(40);
            page.Size(PageSizes.A4);
            page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Grey.Darken4));

            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text("Yoon u Auto").FontSize(18).Bold().FontColor(Navy);
                        left.Item().Text("Services Automobiles au Sénégal")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                    row.ConstantItem(160).AlignRight().Column(right =>
                    {
                        right.Item().Text("CONTRAT DE VENTE").FontSize(13).Bold().FontColor(Navy);
                        right.Item().Text($"Réf. #{contract.PublicReference}")
                            .FontSize(10).FontColor(Azure);
                        right.Item().Text($"Vente vérifiée le {Date(contract.ValidatedAt)}")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });
                col.Item().PaddingTop(10).LineHorizontal(1.5f).LineColor(Azure);
            });

            page.Content().PaddingVertical(18).Column(col =>
            {
                col.Spacing(16);

                col.Item().Element(c => Section(c, "Véhicule", inner =>
                {
                    Field(inner, "Marque et modèle", string.Join(' ', new[]
                    {
                        contract.VehicleMake, contract.VehicleModel, contract.VehicleVersion
                    }.Where(s => !string.IsNullOrWhiteSpace(s))));
                    Field(inner, "Année", contract.VehicleYear.ToString(CultureInfo.InvariantCulture));
                    Field(inner, "Kilométrage", contract.VehicleMileage is { } km
                        ? $"{km.ToString("N0", CultureInfo.GetCultureInfo("de-DE"))} km"
                        : "—");
                    Field(inner, "Numéro de châssis (VIN)", contract.VehicleVin ?? "—");
                    Field(inner, "Immatriculation", contract.RegistrationPlate ?? "—");
                    Field(inner, "Référence de l'annonce", $"#{contract.VehicleReference}");
                }));

                col.Item().Element(c => Section(c, "Vendeur", inner =>
                {
                    Field(inner, "Nom", contract.SellerLegalName);
                    Field(inner, "Pièce d'identité", contract.SellerIdDocument ?? "—");
                    Field(inner, "Adresse", contract.SellerAddress ?? "—");
                    Field(inner, "Téléphone", contract.SellerPhone ?? "—");
                }));

                col.Item().Element(c => Section(c, "Acheteur", inner =>
                {
                    Field(inner, "Nom", contract.BuyerLegalName);
                    Field(inner, "Pièce d'identité", contract.BuyerIdDocument ?? "—");
                    Field(inner, "Adresse", contract.BuyerAddress ?? "—");
                    Field(inner, "Téléphone", contract.BuyerPhone ?? "—");
                }));

                col.Item().Element(c => Section(c, "Conditions de la vente", inner =>
                {
                    Field(inner, "Prix convenu", Fcfa(contract.AgreedPrice));
                    Field(inner, "Date de la vente", Date(contract.SaleDate));
                }));

                col.Item().PaddingTop(4).Text(
                    "Le vendeur déclare que le véhicule décrit ci-dessus lui appartient et qu'il "
                    + "est libre de tout gage. L'acheteur déclare avoir examiné le véhicule et "
                    + "l'accepter dans l'état où il se trouve. Les deux parties ont validé ce "
                    + "contrat sur Yoon u Auto.")
                    .FontSize(9).FontColor(Colors.Grey.Darken2);

                // Firmas: el documento se imprime y se firma a mano.
                col.Item().PaddingTop(20).Row(row =>
                {
                    row.RelativeItem().Element(c => SignatureBox(c, "Le vendeur"));
                    row.ConstantItem(30);
                    row.RelativeItem().Element(c => SignatureBox(c, "L'acheteur"));
                });
            });

            page.Footer().Column(col =>
            {
                col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                col.Item().PaddingTop(8).Row(row =>
                {
                    row.ConstantItem(70).Image(QrPng(verificationUrl));
                    row.ConstantItem(10);
                    row.RelativeItem().AlignMiddle().Column(text =>
                    {
                        text.Item().Text("Vérification de ce contrat")
                            .FontSize(9).Bold().FontColor(Navy);
                        text.Item().Text(verificationUrl).FontSize(7).FontColor(Colors.Grey.Darken1);
                        text.Item().Text($"Code : {contract.VerificationCode}")
                            .FontSize(8).FontColor(Colors.Grey.Darken2);
                    });
                });
            });
        });
    }

    /// <summary>QR en PNG. Se genera en memoria, sin dependencias de System.Drawing.</summary>
    private static byte[] QrPng(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(data).GetGraphic(6);
    }

    private static void Section(IContainer container, string title, Action<ColumnDescriptor> body)
    {
        container.Column(col =>
        {
            col.Item().PaddingBottom(6).Text(title.ToUpperInvariant())
                .FontSize(9).Bold().LetterSpacing(0.08f).FontColor(Azure);
            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(inner =>
            {
                inner.Spacing(4);
                body(inner);
            });
        });
    }

    private static void Field(ColumnDescriptor col, string label, string value)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(150).Text(label).FontColor(Colors.Grey.Darken1);
            row.RelativeItem().Text(value).SemiBold();
        });
    }

    private static void SignatureBox(IContainer container, string title)
    {
        container.Column(col =>
        {
            col.Item().Text(title).FontSize(9).FontColor(Colors.Grey.Darken1);
            col.Item().PaddingTop(34).LineHorizontal(1).LineColor(Colors.Grey.Medium);
            col.Item().PaddingTop(3).Text("Signature").FontSize(7).FontColor(Colors.Grey.Medium);
        });
    }
}
