using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace yERD.Printing.yworks {
	public class YWorksFontInfo {
		public string FontFamily { get; protected set; }
		public double FontWidth { get; protected set; }
		public double FontHeight { get; protected set; }

		public YWorksFontInfo(string fontfamily, double fontWidth, double fontHeight) {
			FontFamily = fontfamily;
			FontWidth = fontWidth;
			FontHeight = fontHeight;
		}
	}
}
