﻿namespace Vexe.Runtime.Types.Examples
{
	[MinimalView]
	public class ParagraphExample : BetterBehaviour
	{
		[Paragraph]
		public string p1;

		[Paragraph(3, 15)]
		public string P2 { get; set; }
	}
}
