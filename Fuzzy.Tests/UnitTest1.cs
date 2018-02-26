using System;
using Xunit;
using Fuzzy;
using System.Linq;

namespace Fuzzy.Tests
{
    public class UnitTest1
    {
        [Fact]
		public void TestFuzzyRules1()
		{
			Linguistic Beurteilung, Note1, Note2;
			Beurteilung = new Linguistic(nameof(Beurteilung));
			Beurteilung.Add("schlecht", (float)-0.01, 0, 8);
			Beurteilung.Add("mittel", 5, (float)8.5, 12);
			Beurteilung.Add("gut", 7, 15, (float)15.1);

			Note1 = new Linguistic(nameof(Note1));
			Note1.Add("1", float.MinValue, 1, 3);
			Note1.Add("2", 0, 2, 4);
			Note1.Add("3", 1, 3, 5);
			Note1.Add("4", 2, 4, 6);
			Note1.Add("5", 3, 5, float.MaxValue);

			Note2 = new Linguistic(nameof(Note2));
			Note2.Add("1", float.MinValue, 1, 3);
			Note2.Add("2", 0, 2, 4);
			Note2.Add("3", 1, 3, 5);
			Note2.Add("4", 2, 4, 6);
			Note2.Add("5", 3, 5, float.MaxValue);

			//Berechnung
			float erg = 0;
			Reasoning reasoning = new Reasoning(Beurteilung, Note1, Note2);

			//Beurteilung super
			reasoning.AddRule(nameof(Beurteilung), "gut", FuzzyOperator.AND, nameof(Note1), "1", nameof(Note2), "1");

			//Beurteilung gut
			reasoning.AddRule(Beurteilung, "gut", FuzzyOperator.AND, Note1, "1", Note2, "2");
			reasoning.AddRule(Beurteilung, "gut", FuzzyOperator.AND, Note1, "2", Note2, "3");
			reasoning.AddRule(Beurteilung, "gut", FuzzyOperator.AND, Note1, "1", Note2, "3");
			reasoning.AddRule(Beurteilung, "gut", FuzzyOperator.AND, Note1, "1", Note2, "4");

			//Beurteilung mittel
			reasoning.AddRule(Beurteilung, "mittel", FuzzyOperator.AND, Note1, "1", Note2, "5");
			reasoning.AddRule(Beurteilung, "mittel", FuzzyOperator.AND, Note1, "2", Note2, "4");
			reasoning.AddRule(Beurteilung, "mittel", FuzzyOperator.AND, Note1, "3", Note2, "4");
			reasoning.AddRule(Beurteilung, "mittel", FuzzyOperator.AND, Note1, "2", Note2, "5");

			//Beurteilung schlecht
			reasoning.AddRule(Beurteilung, "schlecht", FuzzyOperator.AND, Note1, "3", Note2, "5");
			reasoning.AddRule(Beurteilung, "schlecht", FuzzyOperator.AND, Note1, "4", Note2, "5");

			erg = reasoning.Execute(); // note1, note2, 0, 15
			Assert.True(erg > 12 && erg < 14);
			erg = reasoning.Execute(); // 1, 1, 1, 15
			Assert.True(erg > 12 && erg < 15);
        }
        [Fact]
        public void TestFuzzyRules2()
		{
			#region define linguistic variables
			Linguistic TileSizeRating, TileWidth, TileHeight;
			TileSizeRating = new Linguistic(nameof(TileSizeRating), "too small", "good size", "too big", 0.0, 6.0, 8.0, 21.0);    // means: 7 is the optimum for "good size"
			TileWidth = new Linguistic(nameof(TileWidth), "not wide enough", "a good width", "too wide", 50, 420, 420, 700);
			TileHeight = new Linguistic(nameof(TileHeight), "not high enough", "a good height", "too high", 50, 420, 420, 700);
			#endregion

			Reasoning TileSizeReasoning = new Reasoning(TileSizeRating, TileWidth, TileHeight);

			#region add rules
			TileSizeReasoning.AddRule(TileSizeRating, "good size", FuzzyOperator.AND, TileWidth, "a good width", TileHeight, "a good height");
			TileSizeReasoning.AddRule(TileSizeRating, "too small", FuzzyOperator.AND, TileWidth, "not wide enough", TileHeight, "not high enough");
			TileSizeReasoning.AddRule(TileSizeRating, "too small", FuzzyOperator.AND, TileWidth, "not wide enough", TileHeight, "too high");
			TileSizeReasoning.AddRule(TileSizeRating, "too big", FuzzyOperator.AND, TileWidth, "too wide", TileHeight, "too high");
			TileSizeReasoning.AddRule(TileSizeRating, "too small", FuzzyOperator.AND, TileWidth, "too wide", TileHeight, "not high enough");
			#endregion

			float result0 = TileSizeReasoning.Execute(new { TileWidth = 208, TileHeight = 260 });
			float howGoodA = TileSizeReasoning.OutVar["good size"].RuleMax;
			float howGoodB = TileSizeRating["good size"].Value();
			float howTooBigA = TileSizeRating["too big"].RuleMax;
			float howTooBigB = TileSizeRating["too big"].Value();

			TileWidth.Value = 180;
			TileHeight.Value = 320;
			float result1 = TileSizeReasoning.Execute();
			Assert.True(TileSizeRating["good size"].Value() > 0.83);
			Assert.True(9.9 < result1 && result1 < 10.1);

			float result3 = TileSizeReasoning.Execute(new { TileWidth = 235, TileHeight = 235 });
			Assert.True(TileSizeRating["too small"].Value() >= 0);
			Assert.True(TileSizeRating["good size"].Value() > 0.88);
			Assert.True(9.4 < result3 && result3 < 9.5);

			float result2 = TileSizeReasoning.Execute(new { TileWidth = 95, TileHeight = 121 });
			Assert.True(TileSizeReasoning.OutVar["good size"].Value() > TileSizeReasoning.OutVar["too big"].Value());

			float result4 = TileSizeReasoning.Execute(new { TileWidth = 30, TileHeight = 410 });
			Assert.True(TileSizeReasoning.OutVar.IsTotally("good size"));   // why would this be a good size for a tile; reconsider the examples
		}
        [Fact]
        public void TestFuzzyRulesTwoLevelInference()
		{
			#region rules for the TileSize

			#region define linguistic variables
			Linguistic TileSizeRating, TileWidth, TileHeight;  // to declare variables here enables the use of nameof in the following lines
			TileSizeRating = new Linguistic(nameof(TileSizeRating), "too small", "good size", "too big", 0, 6, 8, 14);    // means: 6..8 is the optimum for "good size" (like pH levels: 7 is balanced)
			TileWidth = new Linguistic(nameof(TileWidth), "not wide enough", "a good width", "too wide", 50, 208, 258, 420);
			TileHeight = new Linguistic(nameof(TileHeight), "not high enough", "a good height", "too high", 50, 210, 260, 420);
			#endregion

			var TileSizeReasoning = new Reasoning(TileSizeRating, TileWidth, TileHeight);
			TileSizeReasoning.OutVar.Value = 16F;
			Assert.Same(TileSizeReasoning.OutVar, TileSizeRating);
			TileSizeReasoning.OutVar = new Linguistic("TileSizeRating", "too small", "good size", "too big", 0, 6, 8, 14);
			Assert.NotSame(TileSizeReasoning.OutVar, TileSizeRating);
			TileSizeReasoning.OutVar = TileSizeRating;
			Assert.Same(TileSizeReasoning.OutVar, TileSizeRating);

			#region add rules
			TileSizeReasoning.AddRule(TileSizeRating, "good size", FuzzyOperator.AND, TileWidth, "a good width", TileHeight, "a good height");
			//TileSizeReasoning.AddRule(TileSizeRating, "good size", TileWidth, "not wide enough", FuzzyOperator.AND, TileHeight, "a good height");
			TileSizeReasoning.AddRule(TileSizeRating, "too small", FuzzyOperator.AND, TileWidth, "not wide enough", TileHeight, "not high enough");
			//TileSizeReasoning.AddRule(TileSizeRating, "too small", TileWidth, "not wide enough", FuzzyOperator.AND, TileHeight, "too high");
			TileSizeReasoning.AddRule(TileSizeRating, "too big", FuzzyOperator.AND, TileWidth, "too wide", TileHeight, "too high");
			//TileSizeReasoning.AddRule(TileSizeRating, "too small", TileWidth, "too wide", FuzzyOperator.AND, TileHeight, "not high enough");
			#endregion

			// 1st level inference
			float result00 = TileSizeReasoning.Execute(new { TileWidth = 190, TileHeight = 180 });
			float result01 = TileSizeReasoning.Execute(new { TileWidth = 120, TileHeight = 120 });
			float result02 = TileSizeReasoning.Execute(new { TileWidth = 360, TileHeight = 480 });
			float result0 = TileSizeReasoning.Execute(new { TileWidth = 208, TileHeight = 260 });
			Assert.True(TileSizeRating["good size"].Value() > 0.95);

			#endregion

			#region rules for the TotalSize

			#region define linguistic variables
			Linguistic TotalSizeRating, TotalWidth, TotalHeight;
			TotalSizeRating = new Linguistic(nameof(TotalSizeRating), "too small", "good size", "too big", 0, 6, 8, 14);  // means: 6..8 is the optimum for "good size" (like pH levels: 7 is balanced)
			TotalWidth = new Linguistic(nameof(TotalWidth), "not wide enough", "a good width", "too wide", 500, 2000, 2580, 4200);
			TotalHeight = new Linguistic(nameof(TotalHeight), "not high enough", "a good height", "too high", 500, 2020, 2600, 4200);
			#endregion

			Reasoning TotalSizeReasoning = new Reasoning(TotalSizeRating, TotalWidth, TotalHeight);

			#region add rules
			TotalSizeReasoning.AddRule(TotalSizeRating, "good size", FuzzyOperator.AND, TotalWidth, "a good width", TotalHeight, "a good height");
			TotalSizeReasoning.AddRule(TotalSizeRating, "too small", FuzzyOperator.AND, TotalWidth, "not wide enough", TotalHeight, "not high enough");
			//TotalSizeReasoning.AddRule(TotalSizeRating, "too small", TotalWidth, "not wide enough", FuzzyOperator.AND, TotalHeight, "too high");
			TotalSizeReasoning.AddRule(TotalSizeRating, "too big", FuzzyOperator.AND, TotalWidth, "too wide", TotalHeight, "too high");
			//TotalSizeReasoning.AddRule(TotalSizeRating, "too small", TotalWidth, "too wide", FuzzyOperator.AND, TotalHeight, "not high enough");
			#endregion

			// still 1st level inference (parallel branch)
			TotalWidth.Value = 2080;
			TotalHeight.Value = 2600;
			float result1 = TotalSizeReasoning.Execute();
			Assert.True(TotalSizeRating["good size"].Value() > 0.95);

			#endregion

			#region rules to combine TileSize and TotalSize
			Linguistic SizeRating;
			SizeRating = new Linguistic(nameof(SizeRating), "useless", "useful", 0, 10);
			Reasoning SizeReasoning = new Reasoning(SizeRating, TotalSizeRating, TileSizeRating);
			SizeReasoning.AddRule(SizeRating, "useful", FuzzyOperator.AND, TotalSizeRating, "good size", TileSizeRating, "good size");
			SizeReasoning.AddRule(SizeRating, "useful", FuzzyOperator.AND, TotalSizeRating, "too big", TileSizeRating, "good size");
			SizeReasoning.AddRule(SizeRating, "useful", FuzzyOperator.AND, TotalSizeRating, "good size", TileSizeRating, "too small");
			SizeReasoning.AddRule(SizeRating, "useless", FuzzyOperator.AND, TotalSizeRating, "too small", TileSizeRating, "too big");
			SizeReasoning.AddRule(SizeRating, "useless", FuzzyOperator.AND, TotalSizeRating, "too small", TileSizeRating, "too small");
			#endregion

			#region reasoning for the 2nd level
			float result0and1 = SizeReasoning.Execute();
			#endregion
		}
		[Fact]
		public void TestFuzzyOneStepInference()
		{
			Linguistic TileWidth, TileHeight, TotalWidth, TotalHeight, SizeRating;
			TileWidth = new Linguistic(nameof(TileWidth), "not wide enough", "a good width", "too wide", 50, 160, 200, 420);
			TileHeight = new Linguistic(nameof(TileHeight), "not high enough", "a good height", "too high", 50, 200, 250, 420);
			TotalWidth = new Linguistic(nameof(TotalWidth), "not wide enough", "a good width", "too wide", 500, 2040, 2560, 4200);
			TotalHeight = new Linguistic(nameof(TotalHeight), "not high enough", "a good height", "too high", 500, 2400, 3200, 4200);

			SizeRating = new Linguistic(nameof(SizeRating), "useless", "useful", 0, 6);
			Reasoning SizeReasoning = new Reasoning(SizeRating, TileWidth, TileHeight, TotalWidth, TotalHeight);
			SizeReasoning.AddRuleSet(SizeRating, "useful", TileWidth, "a good width", TileHeight, "a good height", TotalWidth, "a good width", TotalHeight, "a good height");
			SizeReasoning.AddRulePermutations(SizeRating, "useless", TotalWidth, "not wide enough", "too wide", TotalHeight, "not high enough", "too high");
			SizeReasoning.AddRulePermutations(SizeRating, "useless", TileWidth, "not wide enough", "too wide", TileHeight, "not high enough", "too high");

			#region write test results to output window
			float result = SizeReasoning.Execute(new { TileWidth = 208, TileHeight = 260, TotalWidth = 2080, TotalHeight = 2600 });
			Console.WriteLine(TileWidth.Value + "x" + TileHeight.Value + " " + TotalWidth.Value + "x" + TotalHeight.Value + ": result {4:#.##}\t|\tuseful {0:##.#%}/{1:##.#%}\t|\tuseless {2:##.#%}/{3:##.#%}",
					SizeRating["useful"].Value(), SizeRating["useful"].RuleMax, SizeRating["useless"].Value(), SizeRating["useless"].RuleMax, result);

			result = SizeReasoning.Execute(new { TileWidth = 208, TileHeight = 260, TotalWidth = 1040, TotalHeight = 1300 });
			Console.WriteLine(TileWidth.Value + "x" + TileHeight.Value + " " + TotalWidth.Value + "x" + TotalHeight.Value + ": result {4:#.##}\t|\tuseful {0:##.#%}/{1:##.#%}\t|\tuseless {2:##.#%}/{3:##.#%}",
					SizeRating["useful"].Value(), SizeRating["useful"].RuleMax, SizeRating["useless"].Value(), SizeRating["useless"].RuleMax, result);

			result = SizeReasoning.Execute(new { TileWidth = 208, TileHeight = 260, TotalWidth = 832, TotalHeight = 1040 });
			Console.WriteLine(TileWidth.Value + "x" + TileHeight.Value + " " + TotalWidth.Value + "x" + TotalHeight.Value + ": result {4:#.##}\t|\tuseful {0:##.#%}/{1:##.#%}\t|\tuseless {2:##.#%}/{3:##.#%}",
					SizeRating["useful"].Value(), SizeRating["useful"].RuleMax, SizeRating["useless"].Value(), SizeRating["useless"].RuleMax, result);

			result = SizeReasoning.Execute(new { TileWidth = 208, TileHeight = 260, TotalWidth = 624, TotalHeight = 780 });
			Console.WriteLine(TileWidth.Value + "x" + TileHeight.Value + " " + TotalWidth.Value + "x" + TotalHeight.Value + ": result {4:#.##}\t|\tuseful {0:##.#%}/{1:##.#%}\t|\tuseless {2:##.#%}/{3:##.#%}",
					SizeRating["useful"].Value(), SizeRating["useful"].RuleMax, SizeRating["useless"].Value(), SizeRating["useless"].RuleMax, result);

			result = SizeReasoning.Execute(new { TileWidth = 208, TileHeight = 260, TotalWidth = 416, TotalHeight = 520 });
			Console.WriteLine(TileWidth.Value + "x" + TileHeight.Value + " " + TotalWidth.Value + "x" + TotalHeight.Value + ": result {4:#.##}\t|\tuseful {0:##.#%}/{1:##.#%}\t|\tuseless {2:##.#%}/{3:##.#%}",
					SizeRating["useful"].Value(), SizeRating["useful"].RuleMax, SizeRating["useless"].Value(), SizeRating["useless"].RuleMax, result);

			result = SizeReasoning.Execute(new { TileWidth = 208, TileHeight = 260, TotalWidth = 320, TotalHeight = 400 });
			Console.WriteLine(TileWidth.Value + "x" + TileHeight.Value + " " + TotalWidth.Value + "x" + TotalHeight.Value + ": result {4:#.##}\t|\tuseful {0:##.#%}/{1:##.#%}\t|\tuseless {2:##.#%}/{3:##.#%}",
					SizeRating["useful"].Value(), SizeRating["useful"].RuleMax, SizeRating["useless"].Value(), SizeRating["useless"].RuleMax, result);

			result = SizeReasoning.Execute(new { TileWidth = 208, TileHeight = 260, TotalWidth = 1664, TotalHeight = 2080 });
			Console.WriteLine(TileWidth.Value + "x" + TileHeight.Value + " " + TotalWidth.Value + "x" + TotalHeight.Value + ": result {4:#.##}\t|\tuseful {0:##.#%}/{1:##.#%}\t|\tuseless {2:##.#%}/{3:##.#%}",
					SizeRating["useful"].Value(), SizeRating["useful"].RuleMax, SizeRating["useless"].Value(), SizeRating["useless"].RuleMax, result);

			result = SizeReasoning.Execute(new { TileWidth = 110, TileHeight = 110, TotalWidth = 880, TotalHeight = 1100 });
			Console.WriteLine(TileWidth.Value + "x" + TileHeight.Value + " " + TotalWidth.Value + "x" + TotalHeight.Value + ": result {4:#.##}\t|\tuseful {0:##.#%}/{1:##.#%}\t|\tuseless {2:##.#%}/{3:##.#%}",
					SizeRating["useful"].Value(), SizeRating["useful"].RuleMax, SizeRating["useless"].Value(), SizeRating["useless"].RuleMax, result);

			result = SizeReasoning.Execute(new { TileWidth = 88, TileHeight = 110, TotalWidth = 880, TotalHeight = 1100 });
			Console.WriteLine(TileWidth.Value + "x" + TileHeight.Value + " " + TotalWidth.Value + "x" + TotalHeight.Value + ": result {4:#.##}\t|\tuseful {0:##.#%}/{1:##.#%}\t|\tuseless {2:##.#%}/{3:##.#%}",
					SizeRating["useful"].Value(), SizeRating["useful"].RuleMax, SizeRating["useless"].Value(), SizeRating["useless"].RuleMax, result);
			#endregion

			#region test boolean operators
			result = SizeReasoning.Execute(new { TileWidth = 180, TileHeight = 225, TotalWidth = 2300, TotalHeight = 2800 });
			Console.WriteLine(TileWidth.Value + "x" + TileHeight.Value + " " + TotalWidth.Value + "x" + TotalHeight.Value + ": result {4:#.##}\t|\tuseful {0:##.#%}/{1:##.#%}\t|\tuseless {2:##.#%}/{3:##.#%}",
					SizeRating["useful"].Value(), SizeRating["useful"].RuleMax, SizeRating["useless"].Value(), SizeRating["useless"].RuleMax, result);

			bool b0 = SizeReasoning.OutVar.IsTotally("useful");
			bool b1 = SizeReasoning.OutVar.IsRather("useful");
			bool b2 = SizeReasoning.OutVar.IsNotReally("useful");
			bool b3 = SizeReasoning.OutVar.IsNotAtAll("useful");
			bool b4 = SizeReasoning.OutVar.IsTotally("useless");
			bool b5 = SizeReasoning.OutVar.IsRather("useless");
			bool b6 = SizeReasoning.OutVar.IsNotReally("useless");
			bool b7 = SizeReasoning.OutVar.IsNotAtAll("useless");
			Assert.True(b0 && !b1 && !b2 && !b3 && !b4 && !b5 && !b6 && b7);
			#endregion
		}
		[Fact]
		public void TestSelectionOfFuzzyTerms()
		{
			Linguistic TileWidth;
			TileWidth = new Linguistic(nameof(TileWidth), "not wide enough", "a good width", "too wide", 50, 160, 200, 420);
			TileWidth.Value = 180;
			var q = (from x in TileWidth where TileWidth.IsTotally(x.Name) select x);
			var list = q.ToList().OrderByDescending(x => x.Value());
			Assert.Equal("a good width", list.First().Name);
			TileWidth.Value = 350;
			q = from x in TileWidth
				where x.Value() > 0
				select x;
			list = q.ToList().OrderByDescending(x => x.Value());
			Assert.Equal(2, list.Count());
			Assert.True(list.First().Value() > 0.73);
			Assert.True(list.Last().Value() < 0.32);
		}
		[Fact]
		public void TestUnaryRule()
		{
			Linguistic TotalWidth, TotalHeight, SizeRating;
			TotalWidth = new Linguistic(nameof(TotalWidth), "not wide enough", "a good width", "too wide", 500, 2040, 2560, 4200);
			TotalHeight = new Linguistic(nameof(TotalHeight), "not high enough", "a good height", "too high", 500, 2400, 3200, 4200);
			SizeRating = new Linguistic(nameof(SizeRating), "useless", "useful", 0, 6);
			Reasoning UnaryReasoning = new Reasoning(SizeRating, TotalWidth, TotalHeight);

			UnaryReasoning.AddRule(SizeRating, "useful", TotalWidth, "a good width");
			UnaryReasoning.AddRule(SizeRating, "useless", TotalWidth, "not wide enough");
			UnaryReasoning.AddRule(SizeRating, "useless", TotalWidth, "too wide");
			UnaryReasoning.AddRule(SizeRating, "useful", TotalHeight, "a good height");
			UnaryReasoning.AddRule(SizeRating, "useless", TotalHeight, "not high enough");
			UnaryReasoning.AddRule(SizeRating, "useless", TotalHeight, "too high");

			UnaryReasoning.Execute(new { TotalWidth = 500, TotalHeight = 650 });

			float erg = UnaryReasoning.OutVar.Value;

			// without adjusted tresholds this underspecified knowledgebase creates quite reasonable results either in term useful or useless
			// more rules with conjunctions (AND connected) would bring the reasoning quality up - see TestFuzzyOneStepInference
			bool b1 = UnaryReasoning.OutVar.IsTotally("useful");
			Assert.False(b1);
			bool b2 = UnaryReasoning.OutVar.IsRather("useful");
			Assert.False(b2);
			bool b3 = UnaryReasoning.OutVar.IsNotReally("useful");
			Assert.False(b3);
			bool b4 = UnaryReasoning.OutVar.IsNotAtAll("useful");
			Assert.True(b4);
			bool b5 = UnaryReasoning.OutVar.IsTotally("useless");
			Assert.False(b5);
			bool b6 = UnaryReasoning.OutVar.IsRather("useless");
			Assert.False(b6);
			bool b7 = UnaryReasoning.OutVar.IsNotReally("useless");
			Assert.True(b7);
			bool b8 = UnaryReasoning.OutVar.IsNotAtAll("useless");
			Assert.False(b8);
		}
	}
}
