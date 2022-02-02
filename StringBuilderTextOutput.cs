// Based on PlainTextOutput.cs
// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System.Reflection.Metadata;
using System.Text;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace Cilurbo {

	public sealed class StringBuilderTextOutput : ITextOutput {
		readonly StringBuilder builder;
		int indent;
		bool needsIndent;

		public string IndentationString { get; set; } = "\t";

		public StringBuilderTextOutput (StringBuilder builder)
		{
			if (builder is null)
				throw new ArgumentNullException (nameof (builder));
			this.builder = builder;
		}

		public override string ToString ()
		{
			return builder.ToString ();
		}

		public void Indent ()
		{
			indent++;
		}

		public void Unindent ()
		{
			indent--;
		}

		void AppendIndentation ()
		{
			if (needsIndent) {
				for (int i = 0; i < indent; i++) {
					builder.Append (IndentationString);
				}
				needsIndent = false;
			}
		}

		public void Write (char ch)
		{
			AppendIndentation ();
			builder.Append (ch);
		}

		public void Write (string text)
		{
			AppendIndentation ();
			builder.Append (text);
		}

		public void WriteLine ()
		{
			builder.AppendLine ();
			needsIndent = true;
		}

		public void WriteReference (ICSharpCode.Decompiler.Disassembler.OpCodeInfo opCode, bool omitSuffix = false)
		{
			if (omitSuffix) {
				int lastDot = opCode.Name.LastIndexOf ('.');
				if (lastDot > 0)
					Write (opCode.Name.Remove (lastDot + 1));
			} else {
				Write (opCode.Name);
			}
		}

		public void WriteReference (PEFile module, Handle handle, string text, string protocol = "decompile", bool isDefinition = false)
		{
			Write (text);
		}

		public void WriteReference (IType type, string text, bool isDefinition = false)
		{
			Write (text);
		}

		public void WriteReference (IMember member, string text, bool isDefinition = false)
		{
			Write (text);
		}

		public void WriteLocalReference (string text, object reference, bool isDefinition = false)
		{
			Write (text);
		}

		void ITextOutput.MarkFoldStart (string collapsedText, bool defaultCollapsed)
		{
		}

		void ITextOutput.MarkFoldEnd ()
		{
		}
	}
}
