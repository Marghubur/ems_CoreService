using SautinSoft.Document;
using System;
using System.Collections.Generic;
using System.Text;

namespace OnlineDataBuilder.DocumentService
{
    public class CreateDocument
    {
        private DocumentCore document = default;
        private DocumentBuilder db = default;
        private Section section = default;
        private readonly string DefaultFontFamily;
        private readonly int DefaultFontSize;
        private readonly Color DefaultTextColor;
        public enum Align { Left, Top, Right, Bottom, Center }

        public static class FontFamily
        {
            public const string Verdata = "Verdana", Ariel = "Ariel";
        }
        public CreateDocument()
        {
            DefaultTextColor = Color.Black;
            DefaultFontSize = 16;
            DefaultFontFamily = FontFamily.Verdata;
            document = new DocumentCore();
            db = new DocumentBuilder(document);
            section = document.Document.Sections[0];
        }

        private void SetPageSize(PaperType paperType)
        {
            switch (paperType)
            {
                case PaperType.A4:
                    section.PageSetup.PaperType = PaperType.A4;
                    break;
            }
        }

        private void Configure(bool isBold, int? FontSize = null, Color? color = null, string FontName = null)
        {
            db.CharacterFormat.ClearFormatting();
            db.CharacterFormat.FontName = FontName ?? DefaultFontFamily;
            db.CharacterFormat.Size = FontSize ?? DefaultFontSize;
            db.CharacterFormat.FontColor = color ?? DefaultTextColor;
            db.CharacterFormat.Bold = isBold;
        }
        private void AddParagraph(string Message, Align align, bool IsBreak)
        {
            db.Write(Message);
            if (IsBreak)
                db.InsertSpecialCharacter(SpecialCharacterType.LineBreak);
            if (align == Align.Center)
                (section.Blocks[0] as Paragraph).ParagraphFormat.Alignment = HorizontalAlignment.Center;
        }

        public void Create()
        {
            SetPageSize(PaperType.A4);
            AddParagraph("This is a sample text for demo-1", Align.Left, true);
            AddParagraph("This is a sample text for demo-2", Align.Left, true);
        }
    }
}
