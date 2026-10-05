using System;
using accounting_project.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace accounting_project.Reports
{
    public class VoucherDocument : IDocument
    {
        public VoucherHeader Model { get; }

        public VoucherDocument(VoucherHeader model)
        {
            Model = model;
        }

        // تطبيق متطلبات واجهة IDocument
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container
                .Page(page =>
                {
                    page.Size(PageSizes.A5.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(11));

                    page.ContentFromRightToLeft();

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().Element(ComposeFooter);
                });
        }

        void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("شركة النظم المحاسبية").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                    column.Item().Text($"تاريخ السند: {Model.VoucherDate:yyyy-MM-dd}").FontSize(10);
                });

                row.ConstantItem(150).AlignLeft().Column(column =>
                {
                    column.Item().Text($"سند قبض/صرف").FontSize(14).Bold();
                    column.Item().Text($"رقم السند: #{Model.VoucherNo}").FontSize(12).FontColor(Colors.Red.Medium);
                });
            });
        }

        void ComposeContent(IContainer container)
        {
            container.PaddingVertical(10).Column(column =>
            {
                column.Spacing(8);

                column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Row(row =>
                {
                    row.RelativeItem().Text($"حساب الخزينة / الصندوق: {Model.TreasuryAccountName}");
                    row.RelativeItem().Text($"المبلغ الإجمالي: {Model.TotalAmount:N2} YER").Bold().FontSize(12);
                });

                column.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                {
                    c.Item().Text("البيان / الملاحظات:").Bold();
                    c.Item().Text(string.IsNullOrWhiteSpace(Model.Notes) ? "لا يوجد ملاحظات مدونة." : Model.Notes);
                });

                if (Model.Details != null && Model.Details.Count > 0)
                {
                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Text("#");
                            header.Cell().Background(Colors.Grey.Lighten2).Text("اسم الحساب");
                            header.Cell().Background(Colors.Grey.Lighten2).Text("المبلغ");
                        });

                        foreach (var detail in Model.Details)
                        {
                            table.Cell().Text(detail.VoucherLineNo.ToString());
                            table.Cell().Text(detail.AccountName ?? detail.AccountID);
                            table.Cell().Text(detail.Amount.ToString("N2"));
                        }
                    });
                }
            });
        }

        void ComposeFooter(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Text("توقيع المحاسب: ....................").FontSize(10);
                row.RelativeItem().AlignLeft().Text("توقيع المستلم: ....................").FontSize(10);
            });
        }
    }
}