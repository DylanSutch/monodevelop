//
// WavedLineMarker.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using Mono.TextEditor;
using MonoDevelop.Components;
using MonoDevelop.Core.Text;
using MonoDevelop.Debugger;
using Pango;

namespace MonoDevelop.SourceEditor
{
	class GenericUnderlineMarker : UnderlineTextSegmentMarker, MonoDevelop.Ide.Editor.IGenericTextSegmentMarker
	{
		HslColor MonoDevelop.Ide.Editor.IGenericTextSegmentMarker.Color {
			get {
				return Color;
			}
			set {
				Color = value;
			}
		}

		public GenericUnderlineMarker (ISegment segment, MonoDevelop.Ide.Editor.TextSegmentMarkerEffect effect) : base ("", segment)
		{
			this.effect = effect;
		}

		public override void Draw (MonoTextEditor editor, Cairo.Context cr, LineMetrics layout, int startOffset, int endOffset)
		{
			if (DebuggingService.IsDebugging)
				return;
			int markerStart = Segment.Offset;
			int markerEnd = Segment.EndOffset;
			if (markerEnd < startOffset || markerStart > endOffset) 
				return;

			double drawFrom;
			double drawTo;
			double y = layout.LineYRenderStartPosition;
			double startXPos = layout.TextRenderStartPosition;
			double endXPos = layout.TextRenderEndPosition;

			if (markerStart < startOffset && endOffset < markerEnd) {
				drawTo = endXPos;
				var line = editor.GetLineByOffset (startOffset);
				int offset = line.GetIndentation (editor.Document).Length;
				drawFrom = startXPos + (layout.Layout.IndexToPos (offset).X  / Pango.Scale.PangoScale);
			} else {
				int start;
				if (startOffset < markerStart) {
					start = markerStart;
				} else {
					var line = editor.GetLineByOffset (startOffset);
					int offset = line.GetIndentation (editor.Document).Length;
					start = startOffset + offset;
				}
				int end = endOffset < markerEnd ? endOffset : markerEnd;
				int x_pos;

				x_pos = layout.Layout.IndexToPos (start - startOffset).X;
				drawFrom = startXPos + (int)(x_pos / Pango.Scale.PangoScale);
				x_pos = layout.Layout.IndexToPos (end - startOffset).X;
	
				drawTo = startXPos + (int)(x_pos * editor.Options.Zoom / Pango.Scale.PangoScale);
			}
			
			drawFrom = Math.Max (drawFrom, editor.TextViewMargin.XOffset);
			drawTo = Math.Max (drawTo, editor.TextViewMargin.XOffset);

			if (drawFrom > drawTo)
				return;

			if (drawFrom == drawTo)
				drawTo += editor.TextViewMargin.charWidth;
			
			double height = editor.LineHeight / 5;
			cr.SetSourceColor (Color);
			if (effect == MonoDevelop.Ide.Editor.TextSegmentMarkerEffect.WavedLine) {	
				Pango.CairoHelper.ShowErrorUnderline (cr, drawFrom, y + editor.LineHeight - height, drawTo - drawFrom, height);
			} else if (effect == MonoDevelop.Ide.Editor.TextSegmentMarkerEffect.DottedLine) {
				cr.Save ();
				cr.LineWidth = editor.Options.Zoom;
				cr.MoveTo (drawFrom + 1, y + editor.LineHeight - editor.Options.Zoom + 0.5);
				cr.RelLineTo (editor.TextViewMargin.charWidth, 0);
				cr.SetDash (new double[] { 2 * editor.Options.Zoom, 2 * editor.Options.Zoom}, 0);
				cr.Stroke ();
				cr.Restore ();
			} else {
				cr.MoveTo (drawFrom, y + editor.LineHeight - 1);
				cr.LineTo (drawTo, y + editor.LineHeight - 1);
				cr.Stroke ();
			}
		}

		#pragma warning disable 0067
		public event EventHandler<MonoDevelop.Ide.Editor.TextMarkerMouseEventArgs> MousePressed;

		public event EventHandler<MonoDevelop.Ide.Editor.TextMarkerMouseEventArgs> MouseHover;
		#pragma warning restore 0067

		object MonoDevelop.Ide.Editor.ITextSegmentMarker.Tag {
			get;
			set;
		}

		MonoDevelop.Ide.Editor.TextSegmentMarkerEffect effect;
		MonoDevelop.Ide.Editor.TextSegmentMarkerEffect MonoDevelop.Ide.Editor.IGenericTextSegmentMarker.Effect {
			get {
				return effect;
			}
		}

	}
}

