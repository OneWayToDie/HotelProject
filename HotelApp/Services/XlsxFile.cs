using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace HotelApp.Services
{
	// Минимальный читатель/писатель .xlsx (zip + XML) без сторонних библиотек.
	public static class XlsxFile
	{
		private const string Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
		private const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
		private const string PkgRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";

		private static readonly XNamespace Main = Ns;
		private static readonly XNamespace Rel = RelNs;
		private static readonly XNamespace PkgRel = PkgRelNs;

		#region Чтение

		// Возвращает все строки первого листа. Пустые ячейки — пустые строки.
		// Даты (ячейки с форматом даты) преобразуются в строки "dd.MM.yyyy".
		public static List<string[]> ReadFirstSheet(string path)
		{
			using (var zip = ZipFile.OpenRead(path))
			{
				var shared = ReadSharedStrings(zip);
				var dateStyles = ReadDateStyles(zip);

				string sheetPath = FindFirstSheetPath(zip);
				var entry = zip.GetEntry(sheetPath);
				if (entry == null) throw new InvalidDataException("В файле не найден лист Excel.");

				XDocument doc;
				using (var stream = entry.Open())
					doc = XDocument.Load(stream);

				var sheetData = doc.Root?.Element(Main + "sheetData");
				if (sheetData == null) return new List<string[]>();

				var rows = new List<string[]>();
				foreach (var rowEl in sheetData.Elements(Main + "row"))
				{
					var cells = rowEl.Elements(Main + "c").ToList();
					if (cells.Count == 0)
					{
						rows.Add(new string[0]);
						continue;
					}

					var values = new Dictionary<int, string>();
					int maxIndex = 0;

					foreach (var cell in cells)
					{
						int index = ColumnIndex((string)cell.Attribute("r") ?? "A1");
						values[index] = ReadCell(cell, shared, dateStyles);
						if (index > maxIndex) maxIndex = index;
					}

					var result = new string[maxIndex + 1];
					for (int i = 0; i < result.Length; i++)
						result[i] = values.TryGetValue(i, out string v) ? v : string.Empty;

					rows.Add(result);
				}

				return rows;
			}
		}

		private static string ReadCell(XElement cell, List<string> shared, HashSet<int> dateStyles)
		{
			string type = (string)cell.Attribute("t");
			string styleAttr = (string)cell.Attribute("s");
			int styleIndex = 0;
			if (styleAttr != null) int.TryParse(styleAttr, NumberStyles.Integer, CultureInfo.InvariantCulture, out styleIndex);

			if (type == "s")
			{
				string v = cell.Element(Main + "v")?.Value;
				if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx) &&
					idx >= 0 && idx < shared.Count)
					return shared[idx];
				return string.Empty;
			}

			if (type == "inlineStr")
			{
				var inline = cell.Element(Main + "is");
				if (inline == null) return string.Empty;
				return string.Concat(inline.Descendants(Main + "t").Select(t => t.Value));
			}

			if (type == "str" || type == "b" || type == "e")
				return cell.Element(Main + "v")?.Value ?? string.Empty;

			// Обычное число (может быть датой)
			string raw = cell.Element(Main + "v")?.Value ?? string.Empty;
			if (raw.Length == 0) return string.Empty;

			if (dateStyles.Contains(styleIndex) &&
				double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double serial))
			{
				try { return DateTime.FromOADate(serial).ToString("dd.MM.yyyy"); }
				catch { return raw; }
			}

			return raw;
		}

		private static List<string> ReadSharedStrings(ZipArchive zip)
		{
			var result = new List<string>();
			var entry = zip.GetEntry("xl/sharedStrings.xml");
			if (entry == null) return result;

			XDocument doc;
			using (var stream = entry.Open())
				doc = XDocument.Load(stream);

			foreach (var si in doc.Root.Elements(Main + "si"))
				result.Add(string.Concat(si.Descendants(Main + "t").Select(t => t.Value)));

			return result;
		}

		private static HashSet<int> ReadDateStyles(ZipArchive zip)
		{
			var result = new HashSet<int>();
			var entry = zip.GetEntry("xl/styles.xml");
			if (entry == null) return result;

			XDocument doc;
			using (var stream = entry.Open())
				doc = XDocument.Load(stream);

			var root = doc.Root;
			if (root == null) return result;

			var customFormats = new Dictionary<int, string>();
			var numFmts = root.Element(Main + "numFmts");
			if (numFmts != null)
			{
				foreach (var fmt in numFmts.Elements(Main + "numFmt"))
				{
					if (!int.TryParse((string)fmt.Attribute("numFmtId"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
						continue;
					customFormats[id] = (string)fmt.Attribute("formatCode") ?? string.Empty;
				}
			}

			var cellXfs = root.Element(Main + "cellXfs");
			if (cellXfs == null) return result;

			int index = 0;
			foreach (var xf in cellXfs.Elements(Main + "xf"))
			{
				int numFmtId = 0;
				int.TryParse((string)xf.Attribute("numFmtId") ?? "0", NumberStyles.Integer, CultureInfo.InvariantCulture, out numFmtId);

				if (IsDateFormat(numFmtId, customFormats)) result.Add(index);
				index++;
			}

			return result;
		}

		private static bool IsDateFormat(int numFmtId, Dictionary<int, string> customFormats)
		{
			// Встроенные форматы даты/времени Excel
			if ((numFmtId >= 14 && numFmtId <= 22) ||
				(numFmtId >= 27 && numFmtId <= 36) ||
				(numFmtId >= 45 && numFmtId <= 47) ||
				(numFmtId >= 50 && numFmtId <= 58))
				return true;

			if (customFormats.TryGetValue(numFmtId, out string code))
			{
				string clean = code.ToLowerInvariant();
				return clean.Contains("d") || clean.Contains("y");
			}

			return false;
		}

		private static string FindFirstSheetPath(ZipArchive zip)
		{
			var workbookEntry = zip.GetEntry("xl/workbook.xml");
			if (workbookEntry == null) throw new InvalidDataException("В файле нет книги Excel.");

			XDocument workbook;
			using (var stream = workbookEntry.Open())
				workbook = XDocument.Load(stream);

			var sheet = workbook.Root?.Element(Main + "sheets")?.Elements(Main + "sheet").FirstOrDefault();
			string relId = (string)sheet?.Attribute(Rel + "id");

			var relsEntry = zip.GetEntry("xl/_rels/workbook.xml.rels");
			if (relsEntry != null)
			{
				XDocument rels;
				using (var stream = relsEntry.Open())
					rels = XDocument.Load(stream);

				var rel = rels.Root?.Elements(PkgRel + "Relationship")
					.FirstOrDefault(r => (string)r.Attribute("Id") == relId);

				string target = (string)rel?.Attribute("Target");
				if (!string.IsNullOrEmpty(target))
				{
					target = target.Replace('\\', '/');
					if (target.StartsWith("/")) target = target.TrimStart('/');
					else target = "xl/" + target;
					return target;
				}
			}

			return "xl/worksheets/sheet1.xml";
		}

		private static int ColumnIndex(string cellRef)
		{
			int index = 0;
			foreach (char ch in cellRef)
			{
				if (ch >= 'A' && ch <= 'Z') index = index * 26 + (ch - 'A' + 1);
				else if (ch >= 'a' && ch <= 'z') index = index * 26 + (ch - 'a' + 1);
				else break;
			}
			return Math.Max(0, index - 1);
		}

		#endregion

		#region Запись

		// Пишет один лист с шапкой. Поддерживаются string, int, double, DateTime.
		public static void Write(string path, string sheetName, string[] headers, IList<object[]> rows)
		{
			if (File.Exists(path)) File.Delete(path);

			using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
			{
				WriteEntry(zip, "[Content_Types].xml", w => WriteContentTypes(w));
				WriteEntry(zip, "_rels/.rels", w => WriteRootRels(w));
				WriteEntry(zip, "xl/workbook.xml", w => WriteWorkbook(w, sheetName));
				WriteEntry(zip, "xl/_rels/workbook.xml.rels", w => WriteWorkbookRels(w));
				WriteEntry(zip, "xl/styles.xml", w => WriteStyles(w));
				WriteEntry(zip, "xl/worksheets/sheet1.xml", w => WriteSheet(w, headers, rows));
			}
		}

		private static void WriteEntry(ZipArchive zip, string name, Action<XmlWriter> write)
		{
			var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
			using (var stream = entry.Open())
			using (var writer = XmlWriter.Create(stream, new XmlWriterSettings
			{
				Encoding = new UTF8Encoding(false),
				Indent = false,
				OmitXmlDeclaration = false
			}))
			{
				write(writer);
			}
		}

		private static void WriteContentTypes(XmlWriter w)
		{
			w.WriteStartDocument(true);
			w.WriteStartElement("Types", "http://schemas.openxmlformats.org/package/2006/content-types");

			WriteDefault(w, "rels", "application/vnd.openxmlformats-package.relationships+xml");
			WriteDefault(w, "xml", "application/xml");

			WriteOverride(w, "/xl/workbook.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
			WriteOverride(w, "/xl/worksheets/sheet1.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");
			WriteOverride(w, "/xl/styles.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml");

			w.WriteEndElement();
			w.WriteEndDocument();
		}

		private static void WriteDefault(XmlWriter w, string extension, string contentType)
		{
			w.WriteStartElement("Default");
			w.WriteAttributeString("Extension", extension);
			w.WriteAttributeString("ContentType", contentType);
			w.WriteEndElement();
		}

		private static void WriteOverride(XmlWriter w, string partName, string contentType)
		{
			w.WriteStartElement("Override");
			w.WriteAttributeString("PartName", partName);
			w.WriteAttributeString("ContentType", contentType);
			w.WriteEndElement();
		}

		private static void WriteRootRels(XmlWriter w)
		{
			w.WriteStartDocument(true);
			w.WriteStartElement("Relationships", PkgRelNs);
			w.WriteStartElement("Relationship");
			w.WriteAttributeString("Id", "rId1");
			w.WriteAttributeString("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument");
			w.WriteAttributeString("Target", "xl/workbook.xml");
			w.WriteEndElement();
			w.WriteEndElement();
			w.WriteEndDocument();
		}

		private static void WriteWorkbook(XmlWriter w, string sheetName)
		{
			w.WriteStartDocument(true);
			w.WriteStartElement("workbook", Ns);
			w.WriteAttributeString("xmlns", "r", null, RelNs);
			w.WriteStartElement("sheets");
			w.WriteStartElement("sheet");
			w.WriteAttributeString("name", sheetName);
			w.WriteAttributeString("sheetId", "1");
			w.WriteAttributeString("id", RelNs, "rId1");
			w.WriteEndElement();
			w.WriteEndElement();
			w.WriteEndElement();
			w.WriteEndDocument();
		}

		private static void WriteWorkbookRels(XmlWriter w)
		{
			w.WriteStartDocument(true);
			w.WriteStartElement("Relationships", PkgRelNs);
			WriteRelationship(w, "rId1", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet", "worksheets/sheet1.xml");
			WriteRelationship(w, "rId2", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles", "styles.xml");
			w.WriteEndElement();
			w.WriteEndDocument();
		}

		private static void WriteRelationship(XmlWriter w, string id, string type, string target)
		{
			w.WriteStartElement("Relationship");
			w.WriteAttributeString("Id", id);
			w.WriteAttributeString("Type", type);
			w.WriteAttributeString("Target", target);
			w.WriteEndElement();
		}

		private static void WriteStyles(XmlWriter w)
		{
			w.WriteStartDocument(true);
			w.WriteStartElement("styleSheet", Ns);

			// 164 = dd.mm.yyyy
			w.WriteStartElement("numFmts");
			w.WriteAttributeString("count", "1");
			w.WriteStartElement("numFmt");
			w.WriteAttributeString("numFmtId", "164");
			w.WriteAttributeString("formatCode", "dd.mm.yyyy");
			w.WriteEndElement();
			w.WriteEndElement();

			// 0 - обычный, 1 - белый жирный для шапки
			w.WriteStartElement("fonts");
			w.WriteAttributeString("count", "2");
			WriteFont(w, false, "FF000000");
			WriteFont(w, true, "FFFFFFFF");
			w.WriteEndElement();

			// 0 - нет заливки, 1 - обязательная gray125, 2 - шапка
			w.WriteStartElement("fills");
			w.WriteAttributeString("count", "3");
			WritePatternFill(w, "none", null);
			WritePatternFill(w, "gray125", null);
			w.WriteStartElement("fill");
			w.WriteStartElement("patternFill");
			w.WriteAttributeString("patternType", "solid");
			w.WriteStartElement("fgColor");
			w.WriteAttributeString("rgb", "FF1F4E79");
			w.WriteEndElement();
			w.WriteStartElement("bgColor");
			w.WriteAttributeString("indexed", "64");
			w.WriteEndElement();
			w.WriteEndElement();
			w.WriteEndElement();
			w.WriteEndElement();

			// 0 - без рамки, 1 - тонкая рамка
			w.WriteStartElement("borders");
			w.WriteAttributeString("count", "2");
			WriteBorder(w, false);
			WriteBorder(w, true);
			w.WriteEndElement();

			w.WriteStartElement("cellStyleXfs");
			w.WriteAttributeString("count", "1");
			WriteXf(w, 0, 0, 0, 0, null);
			w.WriteEndElement();

			// 0 - default, 1 - шапка, 2 - текст, 3 - дата, 4 - число
			w.WriteStartElement("cellXfs");
			w.WriteAttributeString("count", "5");
			WriteXf(w, 0, 0, 0, 0, null);
			WriteXf(w, 0, 1, 2, 1, "center");
			WriteXf(w, 0, 0, 0, 1, "left");
			WriteXf(w, 164, 0, 0, 1, "center");
			WriteXf(w, 3, 0, 0, 1, "right");
			w.WriteEndElement();

			w.WriteStartElement("cellStyles");
			w.WriteAttributeString("count", "1");
			w.WriteStartElement("cellStyle");
			w.WriteAttributeString("name", "Normal");
			w.WriteAttributeString("xfId", "0");
			w.WriteAttributeString("builtinId", "0");
			w.WriteEndElement();
			w.WriteEndElement();

			w.WriteEndElement();
			w.WriteEndDocument();
		}

		private static void WriteFont(XmlWriter w, bool bold, string color)
		{
			w.WriteStartElement("font");
			if (bold) w.WriteElementString("b", string.Empty);
			w.WriteStartElement("sz");
			w.WriteAttributeString("val", "11");
			w.WriteEndElement();
			w.WriteStartElement("color");
			w.WriteAttributeString("rgb", color);
			w.WriteEndElement();
			w.WriteStartElement("name");
			w.WriteAttributeString("val", "Calibri");
			w.WriteEndElement();
			w.WriteStartElement("family");
			w.WriteAttributeString("val", "2");
			w.WriteEndElement();
			w.WriteEndElement();
		}

		private static void WritePatternFill(XmlWriter w, string patternType, string color)
		{
			w.WriteStartElement("fill");
			w.WriteStartElement("patternFill");
			w.WriteAttributeString("patternType", patternType);
			if (color != null)
			{
				w.WriteStartElement("fgColor");
				w.WriteAttributeString("rgb", color);
				w.WriteEndElement();
			}
			w.WriteEndElement();
			w.WriteEndElement();
		}

		private static void WriteBorder(XmlWriter w, bool thin)
		{
			w.WriteStartElement("border");
			WriteBorderSide(w, thin, "left");
			WriteBorderSide(w, thin, "right");
			WriteBorderSide(w, thin, "top");
			WriteBorderSide(w, thin, "bottom");
			w.WriteStartElement("diagonal");
			w.WriteEndElement();
			w.WriteEndElement();
		}

		private static void WriteBorderSide(XmlWriter w, bool thin, string side)
		{
			w.WriteStartElement(side);
			if (thin)
			{
				w.WriteAttributeString("style", "thin");
				w.WriteStartElement("color");
				w.WriteAttributeString("rgb", "FFBFBFBF");
				w.WriteEndElement();
			}
			w.WriteEndElement();
		}

		private static void WriteXf(XmlWriter w, int numFmtId, int fontId, int fillId, int borderId, string horizontal)
		{
			w.WriteStartElement("xf");
			w.WriteAttributeString("numFmtId", numFmtId.ToString(CultureInfo.InvariantCulture));
			w.WriteAttributeString("fontId", fontId.ToString(CultureInfo.InvariantCulture));
			w.WriteAttributeString("fillId", fillId.ToString(CultureInfo.InvariantCulture));
			w.WriteAttributeString("borderId", borderId.ToString(CultureInfo.InvariantCulture));
			w.WriteAttributeString("xfId", "0");

			if (numFmtId != 0) w.WriteAttributeString("applyNumberFormat", "1");
			if (fontId != 0) w.WriteAttributeString("applyFont", "1");
			if (fillId != 0) w.WriteAttributeString("applyFill", "1");
			if (borderId != 0) w.WriteAttributeString("applyBorder", "1");

			if (horizontal != null)
			{
				w.WriteAttributeString("applyAlignment", "1");
				w.WriteStartElement("alignment");
				w.WriteAttributeString("horizontal", horizontal);
				w.WriteAttributeString("vertical", "center");
				if (horizontal == "center") w.WriteAttributeString("wrapText", "1");
				w.WriteEndElement();
			}

			w.WriteEndElement();
		}

		private static void WriteSheet(XmlWriter w, string[] headers, IList<object[]> rows)
		{
			int lastRow = rows.Count + 1;
			string lastColumn = ColumnName(headers.Length);

			w.WriteStartDocument(true);
			w.WriteStartElement("worksheet", Ns);

			w.WriteStartElement("dimension");
			w.WriteAttributeString("ref", $"A1:{lastColumn}{lastRow}");
			w.WriteEndElement();

			w.WriteStartElement("sheetViews");
			w.WriteStartElement("sheetView");
			w.WriteAttributeString("workbookViewId", "0");
			w.WriteStartElement("pane");
			w.WriteAttributeString("ySplit", "1");
			w.WriteAttributeString("topLeftCell", "A2");
			w.WriteAttributeString("activePane", "bottomLeft");
			w.WriteAttributeString("state", "frozen");
			w.WriteEndElement();
			w.WriteEndElement();
			w.WriteEndElement();

			w.WriteStartElement("sheetFormatPr");
			w.WriteAttributeString("defaultRowHeight", "15");
			w.WriteEndElement();

			w.WriteStartElement("cols");
			WriteColumn(w, 1, 5);
			WriteColumn(w, 2, 32);
			WriteColumn(w, 3, 18);
			WriteColumn(w, 4, 20);
			WriteColumn(w, 5, 20);
			WriteColumn(w, 6, 8);
			WriteColumn(w, 7, 15);
			WriteColumn(w, 8, 15);
			WriteColumn(w, 9, 15);
			w.WriteEndElement();

			w.WriteStartElement("sheetData");

			// Шапка
			w.WriteStartElement("row");
			w.WriteAttributeString("r", "1");
			w.WriteAttributeString("ht", "26");
			w.WriteAttributeString("customHeight", "1");
			for (int i = 0; i < headers.Length; i++)
				WriteInlineCell(w, CellRef(i, 1), headers[i], 1);
			w.WriteEndElement();

			// Данные
			for (int r = 0; r < rows.Count; r++)
			{
				int rowNumber = r + 2;
				w.WriteStartElement("row");
				w.WriteAttributeString("r", rowNumber.ToString(CultureInfo.InvariantCulture));

				var cells = rows[r] ?? new object[0];
				for (int c = 0; c < cells.Length; c++)
					WriteValueCell(w, CellRef(c, rowNumber), cells[c]);

				w.WriteEndElement();
			}

			w.WriteEndElement(); // sheetData

			w.WriteStartElement("autoFilter");
			w.WriteAttributeString("ref", $"A1:{lastColumn}{lastRow}");
			w.WriteEndElement();

			// Выпадающий список для типа номера (колонка E)
			if (lastRow >= 2)
			{
				w.WriteStartElement("dataValidations");
				w.WriteAttributeString("count", "1");
				w.WriteStartElement("dataValidation");
				w.WriteAttributeString("type", "list");
				w.WriteAttributeString("allowBlank", "0");
				w.WriteAttributeString("showInputMessage", "1");
				w.WriteAttributeString("showErrorMessage", "1");
				w.WriteAttributeString("errorTitle", "Тип номера");
				w.WriteAttributeString("error", "Допустимые значения: Стандарт, Люкс, Конференц-зал");
				w.WriteAttributeString("sqref", $"E2:E{lastRow}");
				w.WriteStartElement("formula1");
				w.WriteString("\"Стандарт,Люкс,Конференц-зал\"");
				w.WriteEndElement();
				w.WriteEndElement();
				w.WriteEndElement();
			}

			w.WriteEndElement(); // worksheet
			w.WriteEndDocument();
		}

		private static void WriteColumn(XmlWriter w, int index, double width)
		{
			w.WriteStartElement("col");
			w.WriteAttributeString("min", index.ToString(CultureInfo.InvariantCulture));
			w.WriteAttributeString("max", index.ToString(CultureInfo.InvariantCulture));
			w.WriteAttributeString("width", width.ToString(CultureInfo.InvariantCulture));
			w.WriteAttributeString("customWidth", "1");
			w.WriteEndElement();
		}

		private static void WriteValueCell(XmlWriter w, string cellRef, object value)
		{
			if (value == null) return;

			if (value is DateTime date)
			{
				w.WriteStartElement("c");
				w.WriteAttributeString("r", cellRef);
				w.WriteAttributeString("s", "3");
				w.WriteStartElement("v");
				w.WriteString(((long)date.Date.ToOADate()).ToString(CultureInfo.InvariantCulture));
				w.WriteEndElement();
				w.WriteEndElement();
				return;
			}

			if (value is int || value is long || value is double || value is float || value is decimal)
			{
				double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
				w.WriteStartElement("c");
				w.WriteAttributeString("r", cellRef);
				w.WriteAttributeString("s", "4");
				w.WriteStartElement("v");
				w.WriteString(number.ToString(CultureInfo.InvariantCulture));
				w.WriteEndElement();
				w.WriteEndElement();
				return;
			}

			WriteInlineCell(w, cellRef, Convert.ToString(value, CultureInfo.InvariantCulture), 2);
		}

		private static void WriteInlineCell(XmlWriter w, string cellRef, string text, int style)
		{
			w.WriteStartElement("c");
			w.WriteAttributeString("r", cellRef);
			w.WriteAttributeString("s", style.ToString(CultureInfo.InvariantCulture));
			w.WriteAttributeString("t", "inlineStr");
			w.WriteStartElement("is");
			w.WriteStartElement("t");
			w.WriteString(text ?? string.Empty);
			w.WriteEndElement();
			w.WriteEndElement();
			w.WriteEndElement();
		}

		private static string CellRef(int columnIndex, int rowNumber)
		{
			return ColumnName(columnIndex + 1) + rowNumber.ToString(CultureInfo.InvariantCulture);
		}

		private static string ColumnName(int columnNumber)
		{
			string result = string.Empty;
			while (columnNumber > 0)
			{
				int modulo = (columnNumber - 1) % 26;
				result = (char)('A' + modulo) + result;
				columnNumber = (columnNumber - modulo) / 26;
			}
			return result;
		}

		#endregion
	}
}
