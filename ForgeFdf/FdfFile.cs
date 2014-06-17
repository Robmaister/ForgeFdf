#region License
/*Copyright (c) 2014 Robert Rouhani <robert.rouhani@gmail.com>

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.*/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ForgeFdf
{
	public class FdfFile
	{
		public FdfFile()
		{

		}

		public FdfFile(string formUrl, IEnumerable<Tuple<string, string>> fields, IEnumerable<string> hiddenFields, IEnumerable<string> readonlyFields)
		{
			FormUrl = formUrl;
			Fields = new Dictionary<string, string>();
			foreach (var field in fields)
				Fields.Add(field.Item1, field.Item2);

			HiddenFields = hiddenFields ?? Enumerable.Empty<string>();
			ReadonlyFields = readonlyFields ?? Enumerable.Empty<string>();
		}

		public FdfFile(string formUrl, IDictionary<string, string> fields, IEnumerable<string> hiddenFields, IEnumerable<string> readonlyFields)
		{
			FormUrl = formUrl;
			Fields = fields;
			HiddenFields = hiddenFields ?? Enumerable.Empty<string>();
			ReadonlyFields = readonlyFields ?? Enumerable.Empty<string>();
		}

		public string FormUrl { get; set; }

		public IDictionary<string, string> Fields { get; set; }

		public IEnumerable<string> HiddenFields { get; set; }

		public IEnumerable<string> ReadonlyFields { get; set; }

		public void Write(MemoryStream stream)
		{
			var header = Encoding.Default.GetBytes("%FDF-1.2\n%\xE2\xE3\xCF\xD3\n\n");
			var field_header = Encoding.Default.GetBytes("1 0 obj\n<<\n/FDF\n<<\n/Fields [\n");
			var field_footer = Encoding.Default.GetBytes("]\n>>\n>>\nendobj\n");
			var trailer = Encoding.Default.GetBytes("trailer\n\n<<\n/Root 1 0 R\n>>\n%%EOF\n\n");

			stream.Write(header);
			stream.Write(field_header);

			WriteFieldData(stream);

			if (!string.IsNullOrWhiteSpace(FormUrl))
			{
				var formStart = Encoding.Default.GetBytes("/F (");
				var formUrl = Encoding.BigEndianUnicode.GetBytes(FormUrl);
				var formEnd = Encoding.Default.GetBytes(")\n");

				stream.Write(formStart);
				stream.Write(new byte[] { 0xFE, 0xFF });
				stream.Write(formUrl);
				stream.Write(formEnd);
			}

			stream.Write(field_footer);
			stream.Write(trailer);
		}

		public void Save(string path)
		{
			using (var ms = new MemoryStream())
			{
				Write(ms);
				using (var fs = File.OpenWrite(path))
				{
					fs.SetLength(0);
					ms.WriteTo(fs);
				}
			}
		}

		private void WriteFieldData(MemoryStream stream)
		{
			var newLine = Encoding.Default.GetBytes("\n");
			var leftArrow = Encoding.Default.GetBytes("<<");
			var rightArrow = Encoding.Default.GetBytes(">>");
			var leftParen = Encoding.Default.GetBytes("(");
			var rightParen = Encoding.Default.GetBytes(")");
			var value = Encoding.Default.GetBytes("/V ");
			var type = Encoding.Default.GetBytes("/T ");
			var setHidden = Encoding.Default.GetBytes("/SetF 2");
			var clearHidden = Encoding.Default.GetBytes("/ClrF 2");
			var setReadonly = Encoding.Default.GetBytes("/SetFf 1");
			var clearReadonly = Encoding.Default.GetBytes("/ClrFf 1");
			var utf16bom = new byte[] { 0xFE, 0xFF };

			foreach (var pair in Fields)
			{
				var keyBytes = Encoding.BigEndianUnicode.GetBytes(EscapeValue(pair.Key));
				var valBytes = Encoding.BigEndianUnicode.GetBytes(EscapeValue(pair.Value));

				stream.Write(leftArrow);
				stream.Write(newLine);
				stream.Write(value);
				stream.Write(leftParen);
				stream.Write(utf16bom);
				stream.Write(valBytes);
				stream.Write(rightParen);
				stream.Write(newLine);
				stream.Write(type);
				stream.Write(leftParen);
				stream.Write(utf16bom);
				stream.Write(keyBytes);
				stream.Write(rightParen);
				stream.Write(newLine);

				if (HiddenFields.Contains(pair.Key))
					stream.Write(setHidden);
				else
					stream.Write(clearHidden);

				stream.Write(newLine);

				if (ReadonlyFields.Contains(pair.Key))
					stream.Write(setReadonly);
				else
					stream.Write(clearReadonly);

				stream.Write(newLine);
				stream.Write(rightArrow);
				stream.Write(newLine);
			}
		}

		private string EscapeValue(string str)
		{
			return str.Replace("\x00)", "\x00\\)").Replace("\x00(", "\x00\\(");
		}
	}
}
