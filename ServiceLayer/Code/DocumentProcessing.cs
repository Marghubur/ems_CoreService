using CommonModal.Modal.HtmlTagModel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ServiceLayer.Interface;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ServiceLayer.Code
{
    public class DocumentProcessing : IDocumentProcessing
    {
        public void ProcessHtml(HtmlNodeDetail htmlNodeDetail)
        {
            if (htmlNodeDetail != null && htmlNodeDetail.TagName == "html")
            {
                this.CreateDocument(htmlNodeDetail);
            }
        }

        private void CreateDocument(HtmlNodeDetail parentNode)
        {
            string destinationFolder = @"E:\projects\OnlineDataBuilderServer\OnlineDataBuilder\ApplicationFiles\test.docx";
            if (File.Exists(destinationFolder)) File.Delete(destinationFolder);
            using (MemoryStream mem = new MemoryStream())
            {
                // Create Document
                using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(mem, WordprocessingDocumentType.Document, true))
                {
                    // Add a main document part. 
                    MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();

                    // Create the document structure and add some text.
                    mainPart.Document = new Document();

                    var bodyNode = parentNode.ChildNodes.Find(x => x.TagName == "body");
                    Body docBody = new Body();
                    this.BuildNodes(docBody, bodyNode, wordDocument);

                    mainPart.Document.AppendChild(docBody);
                    // Add your docx content here
                }

                File.WriteAllBytes(destinationFolder, mem.ToArray());
            }
        }

        private void BuildNodes(Body docBody, HtmlNodeDetail bodyNode, WordprocessingDocument wordDocument)
        {
            foreach (var node in bodyNode.ChildNodes)
            {
                if (node.TagType == "node")
                {
                    switch (node.TagName)
                    {
                        case "table":
                            docBody.Append(this.CreateTable(node, wordDocument));
                            break;
                        default:
                            docBody.Append(this.CreateDivision(node.ChildNodes, wordDocument));
                            break;
                    }
                }
                else
                {
                    var para = this.CreateParagraph(node, wordDocument);
                    docBody.Append(para);
                }
            }
        }

        private Table CreateTable(HtmlNodeDetail nodes, WordprocessingDocument wordDocument)
        {
            Table table = new Table();
            TableProperties tableProperties = this.SetTableProperty(nodes);
            table.PrependChild<TableProperties>(tableProperties);
            int i = 0;
            HtmlNodeDetail item = null;
            while (i < nodes.ChildNodes.Count)
            {
                item = nodes.ChildNodes.ElementAt(i);
                if (item.TagType == "node")
                {
                    switch (item.TagName)
                    {
                        case "table":
                            CreateTable(item, wordDocument);
                            break;
                        case "thead":
                        case "tbody":
                        case "tfoot":
                            i = 0;
                            nodes = item;
                            continue;
                        case "tr":
                            table.Append(this.CreateTableRow(item.ChildNodes, wordDocument));
                            break;
                        case "td":
                            break;
                        case "th":
                            break;
                        default:
                            table.Append(this.CreateDivision(item.ChildNodes, wordDocument));
                            break;
                    }
                }
                else
                {
                    table.Append(this.CreateParagraph(item, wordDocument));
                }
                i++;
            }
            return table;
        }

        private TableProperties SetTableProperty(HtmlNodeDetail node)
        {
            TableProperties props = new TableProperties();
            var borderValues = new EnumValue<BorderValues>(BorderValues.Single);
            var tableBorders = new TableBorders(
                                 new TopBorder { Val = borderValues, Size = 4 },
                                 new BottomBorder { Val = borderValues, Size = 4 },
                                 new LeftBorder { Val = borderValues, Size = 4 },
                                 new RightBorder { Val = borderValues, Size = 4 },
                                 new InsideHorizontalBorder { Val = borderValues, Size = 4 },
                                 new InsideVerticalBorder { Val = borderValues, Size = 4 });

            TableStyle tableStyle = new TableStyle()
            {
                Val = "GridTable4-Accent5"
            };
            props.Append(tableStyle);

            var tableWidth = new TableWidth()
            {
                Width = "5000",
                Type = TableWidthUnitValues.Pct
            };
            props.Append(tableWidth);

            props.Append(tableBorders);
            return props;
        }

        private TableRow CreateTableRow(List<HtmlNodeDetail> nodes, WordprocessingDocument wordDocument)
        {
            TableRow row = new TableRow();
            int i = 0;
            HtmlNodeDetail item = null;
            while (i < nodes.Count)
            {
                item = nodes.ElementAt(i);
                if (item.TagType == "node")
                {
                    switch (item.TagName)
                    {
                        case "table":
                            this.CreateTable(item, wordDocument);
                            break;
                        case "thead":
                        case "tbody":
                        case "tfoot":
                            break;
                        case "tr":
                            row.Append(this.CreateTableRow(item.ChildNodes, wordDocument));
                            break;
                        case "td":
                            row.Append(this.CreateCells(item.ChildNodes, false, wordDocument));
                            break;
                        case "th":
                            row.Append(this.CreateCells(item.ChildNodes, true, wordDocument));
                            break;
                        default:
                            row.Append(this.CreateDivision(item.ChildNodes, wordDocument));
                            break;
                    }
                }
                else
                {
                    row.Append(this.CreateParagraph(item, wordDocument));
                }
                i++;
            }
            return row;
        }

        private TableCell CreateCells(List<HtmlNodeDetail> nodes, bool isHeader, WordprocessingDocument wordDocument)
        {
            int i = 0;
            HtmlNodeDetail item = null;
            TableCell cell = new TableCell();
            TableCellProperties cellProperties = new TableCellProperties();
            var cellWitdh = new TableCellWidth
            {
                Width = "3005",
                Type = TableWidthUnitValues.Pct
            };
            cellProperties.Append(cellWitdh);
            cell.Append(cellProperties);
            while (i < nodes.Count)
            {
                item = nodes.ElementAt(i);
                if (item.TagType == "node")
                {
                    switch (item.TagName)
                    {
                        case "table":
                            this.CreateTable(item, wordDocument);
                            break;
                        case "thead":
                        case "tbody":
                        case "tfoot":
                            break;
                        case "tr":
                            cell.Append(this.CreateTableRow(item.ChildNodes, wordDocument));
                            break;
                        case "td":
                            cell.Append(this.CreateCells(item.ChildNodes, false, wordDocument));
                            break;
                        case "th":
                            cell.Append(this.CreateCells(item.ChildNodes, true, wordDocument));
                            break;
                        default:
                            cell.Append(this.CreateDivision(item.ChildNodes, wordDocument));
                            break;
                    }
                }
                else
                {
                    if (item.ChildNodes.Count > 0)
                        cell.Append(this.CreateDivision(item.ChildNodes, wordDocument));
                    else
                        cell.Append(this.CreateParagraph(item, wordDocument));
                }
                i++;
            }
            return cell;
        }

        private Paragraph CreateDivision(List<HtmlNodeDetail> nodes, WordprocessingDocument wordDocument)
        {
            Paragraph para = new Paragraph();
            Run r = new Run();
            r.Append(new RunProperties());
            para.Append(r);
            int i = 0;
            HtmlNodeDetail item = null;
            while (i < nodes.Count)
            {
                item = nodes.ElementAt(i);
                if (item.TagType == "node")
                {
                    switch (item.TagName)
                    {
                        case "table":
                            this.CreateTable(item, wordDocument);
                            break;
                        case "thead":
                        case "tbody":
                        case "tfoot":
                            break;
                        case "tr":
                            para.Append(this.CreateTableRow(item.ChildNodes, wordDocument));
                            break;
                        case "td":
                            para.Append(this.CreateCells(item.ChildNodes, false, wordDocument));
                            break;
                        case "th":
                            para.Append(this.CreateCells(item.ChildNodes, true, wordDocument));
                            break;
                        default:
                            var innerParas = this.CreateDivision(item.ChildNodes, wordDocument);
                            para.Append(innerParas);
                            break;
                    }
                }
                else
                {
                    para = this.CreateParagraph(item, wordDocument);
                }
                i++;
            }
            return para;
        }

        private Paragraph CreateParagraph(HtmlNodeDetail node, WordprocessingDocument wordDocument)
        {
            Paragraph p = new Paragraph();
            Run r = new Run();
            RunProperties rp = new RunProperties();
            
            //rp.Italic = new Italic();
            //rp.Bold = new Bold();            
            //rp.Underline = new Underline();
            r.Append(rp);
            string message = string.Empty;
            if (!string.IsNullOrEmpty(node.Value))
                message = node.Value;
            Text t = new Text(message) { Space = SpaceProcessingModeValues.Preserve };
            r.Append(t);
            p.Append(r);
            return p;
        }
    }
}
