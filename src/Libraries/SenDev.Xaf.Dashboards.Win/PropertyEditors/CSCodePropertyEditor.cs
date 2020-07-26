﻿using System;
using System.Collections.Generic;
using System.Drawing;
using DevExpress.CodeParser;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.Office.Utils;
using DevExpress.Utils;
using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.API.Native;
using DevExpress.XtraRichEdit.Services;
using DevExpress.XtraLayout.Utils;

namespace SenDev.Xaf.Dashboards.Win.PropertyEditors
{
	[PropertyEditor(typeof(string), EditorAliases.CSCodePropertyEditor, false)]
	public class CSCodePropertyEditor : WinPropertyEditor
	{
		public CSCodePropertyEditor(Type objectType, IModelMemberViewItem model)
			: base(objectType, model)
		{
			ControlBindingProperty = "Text";
		}
		public new RichEditControl Control => ((RichEditControl)base.Control);
		public override bool IsCaptionVisible => false;
		protected override object CreateControlCore()
		{
			var richEditControl = new RichEditControl();
			richEditControl.ReadOnly = !AllowEdit;
			richEditControl.ActiveViewType = RichEditViewType.Draft;
			richEditControl.Dock = System.Windows.Forms.DockStyle.Fill;
			richEditControl.LayoutUnit = DocumentLayoutUnit.Pixel;
			richEditControl.Location = new Point(2, 28);
			richEditControl.Name = "richEditControl";
			richEditControl.Options.AutoCorrect.DetectUrls = false;
			richEditControl.Options.AutoCorrect.ReplaceTextAsYouType = false;
			richEditControl.Options.DocumentCapabilities.Bookmarks = DocumentCapability.Disabled;
			richEditControl.Options.DocumentCapabilities.CharacterStyle = DocumentCapability.Disabled;
			richEditControl.Options.DocumentCapabilities.HeadersFooters = DocumentCapability.Disabled;
			richEditControl.Options.DocumentCapabilities.Hyperlinks = DocumentCapability.Disabled;
			richEditControl.Options.DocumentCapabilities.InlinePictures = DocumentCapability.Disabled;
			richEditControl.Options.DocumentCapabilities.Numbering.Bulleted = DocumentCapability.Disabled;
			richEditControl.Options.DocumentCapabilities.Numbering.MultiLevel = DocumentCapability.Disabled;
			richEditControl.Options.DocumentCapabilities.Numbering.Simple = DocumentCapability.Disabled;
			richEditControl.Options.DocumentCapabilities.ParagraphFormatting = DocumentCapability.Disabled;
			richEditControl.Options.DocumentCapabilities.Paragraphs = DocumentCapability.Enabled;
			richEditControl.Options.DocumentCapabilities.ParagraphStyle = DocumentCapability.Disabled;
			richEditControl.Options.DocumentCapabilities.Sections = DocumentCapability.Disabled;
			richEditControl.Options.DocumentCapabilities.Tables = DocumentCapability.Disabled;
			richEditControl.Options.DocumentCapabilities.TableStyle = DocumentCapability.Disabled;
			richEditControl.Options.HorizontalRuler.Visibility = RichEditRulerVisibility.Hidden;
			richEditControl.Text = "richEditControl1";
			richEditControl.Views.DraftView.AllowDisplayLineNumbers = true;
			////richEditControl.Views.DraftView.Padding =   ToPortable(ScaleUtils.ScalePadding(new System.Windows.Forms.Padding(70, 4, 0, 0)));
			richEditControl.InitializeDocument += new EventHandler(this.RichEditControl_InitializeDocument);


			richEditControl.AddService(typeof(ISyntaxHighlightService), new SyntaxHighlightService(richEditControl));
			return richEditControl;
		}

		////private static PortablePadding ToPortable(System.Windows.Forms.Padding padding) => 
		////	new PortablePadding(padding.Left, padding.Top, padding.Right, padding.Bottom);
		private void RichEditControl_InitializeDocument(object sender, EventArgs e)
		{
			var document = Control.Document;
			document.BeginUpdate();
			try
			{
				document.DefaultCharacterProperties.FontName = "Courier New";
				document.DefaultCharacterProperties.FontSize = 10;
				document.Sections[0].Page.Width = Units.InchesToDocumentsF(100);
				document.Sections[0].LineNumbering.CountBy = 1;
				document.Sections[0].LineNumbering.RestartType = LineNumberingRestart.Continuous;

				var tabSize = Control.MeasureSingleLineString("    ", document.DefaultCharacterProperties);
				var tabs = document.Paragraphs[0].BeginUpdateTabs(true);
				try
				{
					for (int i = 1; i <= 30; i++)
					{
						var tab = new TabInfo();
						tab.Position = i * tabSize.Width;
						tabs.Add(tab);
					}
				}
				finally
				{
					document.Paragraphs[0].EndUpdateTabs(tabs);
				}
			}
			finally
			{
				document.EndUpdate();
			}
		}
	}
	#region SyntaxHighlightService
	public class SyntaxHighlightService : ISyntaxHighlightService
	{
		#region Fields
		readonly RichEditControl editor;
		readonly SyntaxHighlightInfo syntaxHighlightInfo;
		#endregion


		public SyntaxHighlightService(RichEditControl editor)
		{
			this.editor = editor;
			this.syntaxHighlightInfo = new SyntaxHighlightInfo();
		}


		#region ISyntaxHighlightService Members
		public void ForceExecute()
		{
			Execute();
		}
		public void Execute()
		{
			var tokens = Parse(editor.Text);
			HighlightSyntax(tokens);
		}
		#endregion
		TokenCollection Parse(string code)
		{
			if (string.IsNullOrEmpty(code))
				return null;
			var tokenizer = CreateTokenizer();
			if (tokenizer == null)
				return new TokenCollection();
			return tokenizer.GetTokens(code);
		}

		ITokenCategoryHelper CreateTokenizer()
		{
			return TokenCategoryHelperFactory.CreateHelperForFileExtensions("cs");
		}

		void HighlightSyntax(TokenCollection tokens)
		{
			if (tokens == null || tokens.Count == 0)
				return;
			var document = editor.Document;
			var cp = document.BeginUpdateCharacters(0, 1);

			var syntaxTokens = new List<SyntaxHighlightToken>(tokens.Count);
			foreach (Token token in tokens)
			{
				HighlightCategorizedToken((CategorizedToken)token, syntaxTokens);
			}
			document.ApplySyntaxHighlight(syntaxTokens);
			document.EndUpdateCharacters(cp);
		}
		void HighlightCategorizedToken(CategorizedToken token, List<SyntaxHighlightToken> syntaxTokens)
		{
			var highlightProperties = syntaxHighlightInfo.CalculateTokenCategoryHighlight(token.Category);
			var syntaxToken = SetTokenColor(token, highlightProperties);
			if (syntaxToken != null)
				syntaxTokens.Add(syntaxToken);
		}
		SyntaxHighlightToken SetTokenColor(Token token, SyntaxHighlightProperties foreColor)
		{
			if (editor.Document.Paragraphs.Count < token.Range.Start.Line)
				return null;
			int paragraphStart = DocumentHelper.GetParagraphStart(editor.Document.Paragraphs[token.Range.Start.Line - 1]);
			int tokenStart = paragraphStart + token.Range.Start.Offset - 1;
			if (token.Range.End.Line != token.Range.Start.Line)
				paragraphStart = DocumentHelper.GetParagraphStart(editor.Document.Paragraphs[token.Range.End.Line - 1]);

			int tokenEnd = paragraphStart + token.Range.End.Offset - 1;
			return new SyntaxHighlightToken(tokenStart, tokenEnd - tokenStart, foreColor);
		}
	}
	#endregion

	#region SyntaxHighlightInfo
	public class SyntaxHighlightInfo
	{
		readonly Dictionary<TokenCategory, SyntaxHighlightProperties> properties;

		public SyntaxHighlightInfo()
		{
			this.properties = new Dictionary<TokenCategory, SyntaxHighlightProperties>();
			Reset();
		}
		public void Reset()
		{
			properties.Clear();
			Add(TokenCategory.Text, DXColor.Black);
			Add(TokenCategory.Keyword, DXColor.Blue);
			Add(TokenCategory.String, DXColor.Brown);
			Add(TokenCategory.Comment, DXColor.Green);
			Add(TokenCategory.Identifier, DXColor.Black);
			Add(TokenCategory.PreprocessorKeyword, DXColor.Blue);
			Add(TokenCategory.Number, DXColor.Red);
			Add(TokenCategory.Operator, DXColor.Black);
			Add(TokenCategory.Unknown, DXColor.Black);
			Add(TokenCategory.XmlComment, DXColor.Gray);
		}
		void Add(TokenCategory category, Color foreColor)
		{
			var item = new SyntaxHighlightProperties();
			item.ForeColor = foreColor;
			properties.Add(category, item);
		}

		public SyntaxHighlightProperties CalculateTokenCategoryHighlight(TokenCategory category)
		{
			if (properties.TryGetValue(category, out var result))
				return result;
			else
				return properties[TokenCategory.Text];
		}
	}
	#endregion
}
