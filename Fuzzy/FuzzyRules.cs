using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using static Newtonsoft.Json.JsonConvert;

namespace Fuzzy
{
    public class FuzzyTerm
    {
        private Linguistic var;
		public string Name { get; private set; }
		public float X1 { get; private set; }
		public float X2 { get; private set; }
		public float X3 { get; private set; }
		public float X4 { get; private set; }
		public float RuleMax { get; private set; }
		public void SetRuleMax(float max, string text) 
        {
			if (RuleMax <= max)
			{
				RuleMax = max;
				RuleMaxText = text;
			}
		}
		public void ResetRuleMax()
		{
			RuleMax = 0;
			RuleMaxText = string.Empty;
		}
		public void SetParameters(float x1, float x2, float x3)
		{
			SetParameters(x1, x2, x2, x3);
		}
		public void SetParameters(float x1, float x2, float x3, float x4)
		{
			X1 = x1;
			X2 = x2;
			X3 = x3;
			X4 = x4;
		}
		public string RuleMaxText { get; private set; }
		/// <summary>
		/// reads x from the surrounding linguistic var and calculates f(x) for the function defined by the term
		/// </summary>
		/// <remarks>sets ruleMaxText</remarks>
		/// <returns>0..1</returns>
		public float Value()
		{
			RuleMaxText = var.Name + " is " + Name;
			if (var.Value <= X1 || var.Value >= X4)
				return 0F;
			if (var.Value >= X2 && var.Value <= X3)
				return 1F;
			if (var.Value < X2)
			{
				if (float.IsNegativeInfinity(X1) || Math.Abs(X2 - X1) < float.Epsilon)
					return 1F;
				return (var.Value - X1) / (X2 - X1);
			}
			if (float.IsPositiveInfinity(X4) || Math.Abs(X4 - X3) < float.Epsilon)
				return 1F;
			return (X4 - var.Value) / (X4 - X3);
		}
		/// <summary>
		/// calculate f(x) for the function defined by the term but not more than ruleMax allows
		/// </summary>
		/// <returns>0..1</returns>
		public float Value(float val)
		{
			float h = 0F;
			if (val <= X1 || val >= X4)
				return 0F;
			if (val >= X2 && val <= X3)
				h = 1F;
			else if (val < X2)
			{
				if (float.IsNegativeInfinity(X1) || Math.Abs(X2 - X1) < float.Epsilon)
					h = 1F;
				else h = (val - X1) / (X2 - X1);
			}
			else
			{
				if (float.IsPositiveInfinity(X4) || Math.Abs(X4 - X3) < float.Epsilon)
					h = 1F;
				else h = (X4 - val) / (X4 - X3);
			}
			return Math.Min(h, RuleMax);
		}
		internal float MinX()
		{
			return float.IsInfinity(X1) ? float.IsInfinity(X2) ? float.IsInfinity(X3) ? float.IsInfinity(X4) ? float.NaN : X4 : X3 : X2 : X1;
		}
		internal float MaxX()
		{
			return float.IsInfinity(X4) ? float.IsInfinity(X3) ? float.IsInfinity(X2) ? float.IsInfinity(X1) ? float.NaN : X1 : X2 : X3 : X4;
		}
		/// <summary>
		/// Constructor for 3-point term
		/// </summary>
		/// <param name="lingVar">backreference to the hosting Linguistic</param>
		/// <param name="name">identifying name</param>
		/// <param name="x1">first x value with f(x) == 0; typical forms: _/\_, ‾\_, _/‾</param>
		/// <param name="x2">second x value with f(x) == 1; typical form: _/\_, ‾\_, _/‾</param>
		/// <param name="x3">third x value with f(x) == 0; typical form: _/\_, ‾\_, _/‾</param>
		public FuzzyTerm(Linguistic lingVar, string name, float x1, float x2, float x3)
			: this(lingVar, name, x1, x2, x2, x3)
		{
		}
		/// <summary>
		/// Constructor for 4-point term
		/// </summary>
		/// <param name="lingVar">backreference to the hosting Linguistic</param>
		/// <param name="name">identifying name</param>
		/// <param name="x1">first x value with f(x) == 0; typical form: _/‾\_</param>
		/// <param name="x2">second x value with f(x) == 1; typical form: _/‾\_</param>
		/// <param name="x3">third x value with f(x) == 1; typical form: _/‾\_</param>
		/// <param name="x4">fourth x value with f(x) == 0; typical form: _/‾\_</param>
		public FuzzyTerm(Linguistic lingVar, string name, float x1, float x2, float x3, float x4)
		{
			if (lingVar == null)
			{
				throw new Exception("Error in constructor of FuzzyTerm");
			}
			if (!(x1 <= x2 && x2 <= x3 && x3 <= x4))
			{
				throw new Exception("Error in definition of FuzzyTerm, precondition violated: m1 <= m2 <= m3 <= m4");
			}
			var = lingVar;
			Name = name;
			X1 = x1;
			X2 = x2;
			X3 = x3;
			X4 = x4;
			//lingVar.Add(this);
			RuleMax = 0;
		}
    }
    public class Linguistic : IEnumerable<FuzzyTerm>
    {
        /// <summary>
		/// number of integration steps for Trapezoid Method
		/// </summary>
		//private int Intervals = 1000;
		public string Name { get; private set; }
		public float Value { get; set; }
		private Dictionary<string,FuzzyTerm> terms;
		/// <summary>
		/// access to terms of the current Linguistic variable
		/// </summary>
		/// <param name="termName"></param>
		/// <returns></returns>
		public FuzzyTerm this[string termName]
		{
			get
			{
				return terms[termName];
			}
			set
			{
				terms[termName] = value;
			}
		}
		/// <summary>
		/// find the term identified by the given name; like this[] but throws exception if there is no term with the identifier name
		/// </summary>
		/// <param name="name">name of the FuzzyTerm</param>
		/// <param name="throwException">set it to false to return null if term was not found</param>
		/// <returns>null or the FuzzyTerm</returns>
		public FuzzyTerm FindTerm(string name)
		{
			if (!terms.ContainsKey(name))
				throw new KeyNotFoundException(this.Name + "." + name);
			return terms[name];
		}
		/// <summary>
		/// adds or replaces 3-point-term with given name
		/// </summary>
		/// <param name="termName">identifies the term in this Linguistic variable</param>
		/// <param name="x0">first x-value of a triangular definition y(x0) == 0; float.NegativeInfinity can be used for this kind of term: ‾\_</param>
		/// <param name="x1">second x-value of a triangular definition y(x0) == 1</param>
		/// <param name="x2">third x-value of a triangular definition y(x0) == 0; float.PositiveInfinity can be used for this kind of term: _/‾</param>
		public void Add(string termName, float x0, float x1, float x2)
		{
			terms[termName] = new FuzzyTerm(this, termName, x0, x1, x2);
		}
		/// <summary>
		/// adds or replaces 4-point-term with given name
		/// </summary>
		/// <param name="termName">identifies the term in this Linguistic variable</param>
		/// <param name="x0">first x-value of a triangular definition y(x0) == 0; float.NegativeInfinity can be used for this kind of term: ‾\_</param>
		/// <param name="x1">second x-value of a trapez definition y(x0) == 1; typical form: _/‾\_</param>
		/// <param name="x2">fourth x-value of a trapez definition y(x0) == 1; typical form: _/‾\_</param>
		/// <param name="x3">fourth x-value of a triangular definition y(x0) == 0; float.PositiveInfinity can be used for this kind of term: _/‾</param>
		public void Add(string termName, float x0, float x1, float x2, float x4)
		{
			terms[termName] = new FuzzyTerm(this, termName, x0, x1, x2, x4); 
		}
		/// <summary>
		/// gets the highest contributing term's value at x
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		float Fmax(float x)
		{
			float max, h;
			max = 0;
			foreach (string termName in terms.Keys)
			{
				h = terms[termName].Value(x);
				if (h > max)
					max = h;
			}
			return max;
		}
		/// <summary>
		/// reset ruleMax for all terms
		/// </summary>
		public void ResetTerms()
		{
			foreach (string termName in terms.Keys)
				terms[termName].ResetRuleMax();
		}
		/// <summary>
		/// Defuzzification
		/// </summary>
		/// <returns>a float number between 0 and 1</returns>
		/// <remarks>might create problems if a Linguistic that is used as an outvar in a FuzzyRule uses float.MaxValue, float.MinValue, float.NegativeInfinity or float.PositiveInfinity</remarks>
		public void Calculate()
		{
			#region find limits min and max
			float min = float.MaxValue; 
			float max = float.MinValue;
			FuzzyTerm t;
			foreach (string key in terms.Keys)
			{
				t = terms[key];
				if (t.X1 > float.MinValue && t.X1 < min)
					min = t.X1;
				if (t.X4 < float.MaxValue && t.X4 > max)
					max = t.X4;
			}
			#endregion
			Calculate(min, max, Math.Max((int)(max - min), 10));
		}
		/// <summary>
		/// Defuzzification between specified left and right border
		/// </summary>
		/// <param name="left">starting x value</param>
		/// <param name="right">ending x value</param>
		/// <param name="intervals">optional: number of intervals to use</param>
		/// <remarks>to get the result of the most recent calculation, access the Value property of the output variable</returns>
		private void Calculate(float left, float right, int intervals = 0)
		{
			if (intervals == 0)
				intervals = Math.Max(10, (int)(right - left));
			#region calculate integral f(x), x * f(x)
			float h = (right - left) / intervals;
			float F1, F2, d, f2, f1, x;
			F1 = 0; F2 = 0; f1 = Fmax(left);
			for (x = left + h; x <= right; x += h)
			{
				f2 = Fmax(x);
				d = (f1 + f2) / (float)2;
				F1 += d;
				F2 += x * d;
				f1 = f2;
			}
			F1 *= h;
			F2 *= h;
			#endregion
			if (F1 <= float.Epsilon || F1 == 0.0 || F1 < 0.00001 || float.IsNaN(F1))
				throw new Exception("no rule applied for rule " + Name + " between " + left.ToString() + " and " + right.ToString() + "; specify more details");
			Value = F2 / F1;
		}
		#region converting to boolean logic
		public bool IsTotally(string term)
		{
			return this[term].Value() >= 0.685;
		}
		public bool IsRather(string term)
		{
			float erg = this[term].Value();
			return 0.64 <= erg && erg < 0.685;
		}
		public bool IsNotReally(string term)
		{
			float erg = this[term].Value();
			return 0.55 < erg && erg < 0.64; 
		}
		public bool IsNotAtAll(string term)
		{
			return this[term].Value() <= 0.55;
		}
		/// <summary>
		/// Eumerator makes sure linq queries can be used for the terms of Linguistic
		/// </summary>
		/// <returns>the selection according to the query used</returns>
		IEnumerator<FuzzyTerm> IEnumerable<FuzzyTerm>.GetEnumerator() => terms.Values.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => terms.Values.GetEnumerator();
        #endregion
        /// <summary>
        /// constructor; no terms added
        /// </summary>
        /// <param name="name"></param>
        public Linguistic(string name)
		{
			this.Name = name;
			terms = new Dictionary<string,FuzzyTerm>();
			Value = 0;
		}
		/// <summary>
		/// constructor; three terms added in a typical manner ‾✕‾✕‾
		/// </summary>
		/// <param name="name">name of linguistic variable</param>
		/// <param name="term0">name of term[0]</param>
		/// <param name="term1">name of term[1]; max1 will be calculated in the middle between max0 and max2</param>
		/// <param name="term2">name of term[2]</param>
		/// <param name="max0">x value where term[0] is 1</param>
		/// <param name="max11">x value where term[1] first becomes 1</param>
		/// <param name="max12">x value where term[1] still is 1</param>
		/// <param name="max2">x value where term[2] is 1</param>
		public Linguistic(string name, string term0, string term1, string term2, double max0, double max11, double max12, double max2)
		{
			this.Name = name;
			terms = new Dictionary<string, FuzzyTerm>();
			Value = 0;
			Add(term0, float.NegativeInfinity, (float)max0, (float)max12);
			Add(term1, (float)max0, (float)max11, (float)max12, (float)max2);
			Add(term2, (float)max11, (float)max2, float.PositiveInfinity);
		}
		/// <summary>
		/// constructor; five terms added in a typical manner ‾✕‾✕‾✕‾✕‾
		/// </summary>
		/// <param name="name">name of linguistic variable</param>
		/// <param name="term0">name of term[0]</param>
		/// <param name="term1">name of term[1]</param>
		/// <param name="term2">name of term[2]</param>
		/// <param name="term3">name of term[3]</param>
		/// <param name="term4">name of term[4]</param>
		/// <param name="max0">x value where term[0] is 1</param>
		/// <param name="max11">x value where term[1] first becomes 1</param>
		/// <param name="max12">x value where term[1] still is 1</param>
		/// <param name="max21">x value where term[2] is 1</param>
		/// <param name="max22">x value where term[2] is 1</param>
		/// <param name="max31">x value where term[3] is 1</param>
		/// <param name="max32">x value where term[3] is 1</param>
		/// <param name="max4">x value where term[4] is 1</param>
		public Linguistic(string name, string term0, string term1, string term2, string term3, string term4, double max0, double max11, double max12, double max21, double max22, double max31, double max32, double max4)
		{
			this.Name = name;
			terms = new Dictionary<string, FuzzyTerm>();
			Value = 0;
			Add(term0, float.NegativeInfinity, (float)max0, (float)max11);
			Add(term1, (float)max0, (float)max11, (float)max12, (float)max21);
			Add(term2, (float)max12, (float)max21, (float)max22, (float)max31);
			Add(term3, (float)max22, (float)max31, (float)max32, (float)max4);
			Add(term4, (float)max32, (float)max4, float.PositiveInfinity);
		}
		/// <summary>
		/// constructor; seven terms added in a typical manner ‾✕‾✕‾✕‾✕‾✕‾✕‾
		/// </summary>
		/// <param name="name">name of linguistic variable</param>
		/// <param name="term0">name of term[0]</param>
		/// <param name="term1">name of term[1]</param>
		/// <param name="term2">name of term[2]</param>
		/// <param name="term3">name of term[3]</param>
		/// <param name="term4">name of term[4]</param>
		/// <param name="term5">name of term[5]</param>
		/// <param name="term6">name of term[6]</param>
		/// <param name="x">12 x values where term[i] is 1</param>
		public Linguistic(string name, string term0, string term1, string term2, string term3, string term4, string term5, string term6, List<float> x)
		{
			this.Name = name;
			terms = new Dictionary<string, FuzzyTerm>();
			Value = 0;
			Add(term0, float.NegativeInfinity, x[0], x[1]);
			Add(term1, x[0], x[1], x[2], x[3]);
			Add(term2, x[2], x[3], x[4], x[5]);
			Add(term3, x[4], x[5], x[6], x[7]);
			Add(term4, x[6], x[7], x[8], x[9]);
			Add(term5, x[8], x[9], x[10], x[11]);
			Add(term6, x[10], x[11], float.PositiveInfinity);
		}
		/// <summary>
		/// constructor; two terms added in a typical manner ‾✕‾
		/// </summary>
		/// <param name="name">name of linguistic variable</param>
		/// <param name="term0">name of term[0]</param>
		/// <param name="term1">name of term[1]; max1 will be calculated in the middle between max0 and max2</param>
		/// <param name="max0">x value where term[0] is 1</param>
		/// <param name="max2">x value where term[1] is 1</param>
		public Linguistic(string name, string term0, string term1, double max0, double max1)
		{
			this.Name = name;
			terms = new Dictionary<string, FuzzyTerm>();
			Value = 0;
			Add(term0, float.NegativeInfinity, (float)max0, (float)max1);
			Add(term1, (float)max0, (float)max1, float.PositiveInfinity);
		}
    }
    public enum FuzzyOperator { AND, OR };
	public class FuzzyRule
	{
		// TODO how about an unlimited number of input variables?
		protected FuzzyTerm outTerm;
		protected List<FuzzyTerm> inTerms;
		protected FuzzyOperator Operation;
		/// <summary>
		/// 0..1; percentage
		/// </summary>
		protected float Possibility;
		protected Linguistic outVar;
		protected List<Linguistic> inVars;
		/// <summary>
		/// calculate minimum for AND and maximum for OR for all inTerms
		/// </summary>
		public void Calculate()
		{
			float opt;
			float current;
			string text = string.Empty;
			#region handle conjunction
			if (Operation == FuzzyOperator.AND) // commutative, minimum
			{
				opt = 1;
				foreach (FuzzyTerm term in inTerms)
				{
					current = term.Value();
					if (current <= opt)
					{
						opt = current;
						text = string.IsNullOrEmpty(text) ? term.RuleMaxText : text + " ∧ " + term.RuleMaxText;
					}
				}
			}
			#endregion
			#region handle disjunction
			else {  // FuzzyOperator.OR is default; commutative, maximum
				opt = 0;
				foreach (FuzzyTerm term in inTerms)
				{
					current = term.Value();
					if (current >= opt)
					{
						opt = current;
						text = term.RuleMaxText;
					}
				}
			}
			#endregion
			Possibility = opt;
			outTerm.SetRuleMax(opt, text);  
		}
		public FuzzyRule(Linguistic outVar, string outTermName, Linguistic in1, string in1TermName)
		{
			this.outVar = outVar;
			this.inVars = new List<Linguistic>();
			this.inVars.Add(in1);
			Operation = FuzzyOperator.OR;
			outTerm = outVar.FindTerm(outTermName);
			inTerms = new List<FuzzyTerm>();
			inTerms.Add(in1.FindTerm(in1TermName));
			Possibility = 0;
		}
		public FuzzyRule(Linguistic outVar, string outTermName, Linguistic in1, string in1TermName, FuzzyOperator op, Linguistic in2, string in2TermName)
		{
			this.outVar = outVar;
			this.inVars = new List<Linguistic>();
			this.inVars.Add(in1);
			this.inVars.Add(in2);
			Operation = op;
			outTerm = outVar.FindTerm(outTermName);
			inTerms = new List<FuzzyTerm>();
			inTerms.Add(in1.FindTerm(in1TermName));
			inTerms.Add(in2.FindTerm(in2TermName));
			Possibility = 0;
		}
	}
	public class Reasoning
	{
		public Linguistic OutVar;
		public Dictionary<string,Linguistic> InVars;
		List<FuzzyRule> rules;
		/// <summary>
		/// Make sure that all rules you add only use the lokal
		/// </summary>
        public List<FuzzyRule> Rules
		{
			get { return rules; }
			private set { rules = value; }
		}
		/// <summary>
		/// Execute reasoning without changing the input variables; output variable will be set
		/// </summary>
		/// <param name="obj">object with properties; key is name of InVar, value is new value as double, float, long or int</param>
		/// <returns>the discrete result; linguistic results can be accessed via Out["TermName"].Value()</returns>
		public float Execute(dynamic obj = null)
		{
			if (obj != null)
			{
				var jo = JObject.FromObject(obj);
				foreach (var elem in jo)
					InVars[elem.Name].Value = (float)elem.Value;
			}
			OutVar.ResetTerms();
			#region fuzzification
			foreach (FuzzyRule rule in Rules)
				rule.Calculate();
			#endregion
			#region defuzzification
			OutVar.Calculate();
			#endregion
			return OutVar.Value;
		}
		/// <summary>
		/// Conjunction of any pair out of the four (creates 6 rules)
		/// </summary>
		/// <param name="outVar"></param>
		/// <param name="outTermName">term of OutVar affected by this rule</param>
		/// <param name="in1"></param>
		/// <param name="in1TermName"></param>
		/// <param name="in2"></param>
		/// <param name="in2TermName"></param>
		/// <param name="in3"></param>
		/// <param name="in3TermName"></param>
		/// <param name="in4"></param>
		/// <param name="in4TermName"></param>
		public void AddRuleSet(string outVar, string outTermName, string in1, string in1TermName, string in2, string in2TermName, string in3, string in3TermName, string in4, string in4TermName)
		{
			#region check for non-local references
			if (outVar != OutVar.Name)
				throw new KeyNotFoundException("Only the local output variable can be use; rule refers to " + outVar + "; local OutVar.Name = " + OutVar.Name);
			if (InVars[in1] == null)
				throw new KeyNotFoundException("Only the local input variables can be use; rule refers to " + in1 + "; local InVars: " + InVars.Keys.ToArray().ToString());
			if (InVars[in2] == null)
				throw new KeyNotFoundException("Only the local input variables can be use; rule refers to " + in2 + "; local InVars: " + InVars.Keys.ToArray().ToString());
			if (InVars[in3] == null)
				throw new KeyNotFoundException("Only the local input variables can be use; rule refers to " + in3 + "; local InVars: " + InVars.Keys.ToArray().ToString());
			if (InVars[in4] == null)
				throw new KeyNotFoundException("Only the local input variables can be use; rule refers to " + in4 + "; local InVars: " + InVars.Keys.ToArray().ToString());
			if (InVars[in1][in1TermName] == null)
				throw new KeyNotFoundException("Only existing terms can be use; rule refers to " + in1TermName);
			if (InVars[in2][in2TermName] == null)
				throw new KeyNotFoundException("Only existing terms can be use; rule refers to " + in2TermName);
			if (InVars[in3][in3TermName] == null)
				throw new KeyNotFoundException("Only existing terms can be use; rule refers to " + in3TermName);
			if (InVars[in4][in4TermName] == null)
				throw new KeyNotFoundException("Only existing terms can be use; rule refers to " + in4TermName);
			#endregion
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in1], in1TermName, FuzzyOperator.AND, InVars[in2], in2TermName));
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in1], in1TermName, FuzzyOperator.AND, InVars[in3], in3TermName));
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in1], in1TermName, FuzzyOperator.AND, InVars[in4], in4TermName));
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in2], in2TermName, FuzzyOperator.AND, InVars[in3], in3TermName));
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in2], in2TermName, FuzzyOperator.AND, InVars[in4], in4TermName));
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in3], in3TermName, FuzzyOperator.AND, InVars[in4], in4TermName));
		}
		/// <summary>
		/// Conjunction of any pair out of the four (creates 6 rules)
		/// </summary>
		/// <param name="outVar"></param>
		/// <param name="outTermName">term of OutVar affected by this rule</param>
		/// <param name="in1"></param>
		/// <param name="in1TermName"></param>
		/// <param name="in2"></param>
		/// <param name="in2TermName"></param>
		/// <param name="in3"></param>
		/// <param name="in3TermName"></param>
		/// <param name="in4"></param>
		/// <param name="in4TermName"></param>
		public void AddRuleSet(Linguistic outVar, string outTermName, Linguistic in1, string in1TermName, Linguistic in2, string in2TermName, Linguistic in3, string in3TermName, Linguistic in4, string in4TermName)
		{
			#region check for non-local references
			if (outVar.Name != OutVar.Name)
				throw new KeyNotFoundException("Only the local output variable can be use; rule refers to " + outVar + "; local OutVar.Name = " + OutVar.Name);
			if (InVars[in1.Name] == null)
				throw new KeyNotFoundException("Only the local input variables can be use; rule refers to " + in1 + "; local InVars: " + InVars.Keys.ToArray().ToString()); 
			if (InVars[in2.Name] == null)
				throw new KeyNotFoundException("Only the local input variables can be use; rule refers to " + in2 + "; local InVars: " + InVars.Keys.ToArray().ToString());
			if (InVars[in3.Name] == null)
				throw new KeyNotFoundException("Only the local input variables can be use; rule refers to " + in3 + "; local InVars: " + InVars.Keys.ToArray().ToString());
			if (InVars[in4.Name] == null)
				throw new KeyNotFoundException("Only the local input variables can be use; rule refers to " + in4 + "; local InVars: " + InVars.Keys.ToArray().ToString());
			if (InVars[in1.Name][in1TermName] == null)
				throw new KeyNotFoundException("Only existing terms can be use; rule refers to " + in1TermName);
			if (InVars[in2.Name][in2TermName] == null)
				throw new KeyNotFoundException("Only existing terms can be use; rule refers to " + in2TermName);
			if (InVars[in3.Name][in3TermName] == null)
				throw new KeyNotFoundException("Only existing terms can be use; rule refers to " + in3TermName);
			if (InVars[in4.Name][in4TermName] == null)
				throw new KeyNotFoundException("Only existing terms can be use; rule refers to " + in4TermName);
			#endregion
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in1.Name], in1TermName, FuzzyOperator.AND, InVars[in2.Name], in2TermName));
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in1.Name], in1TermName, FuzzyOperator.AND, InVars[in3.Name], in3TermName));
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in1.Name], in1TermName, FuzzyOperator.AND, InVars[in4.Name], in4TermName));
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in2.Name], in2TermName, FuzzyOperator.AND, InVars[in3.Name], in3TermName));
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in2.Name], in2TermName, FuzzyOperator.AND, InVars[in4.Name], in4TermName));
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in3.Name], in3TermName, FuzzyOperator.AND, InVars[in4.Name], in4TermName));
		}
		/// <summary>
		/// Creates mirrored rules; useful if in1 and in2 are of the same type and can be given in any order/can be swapped, ie marks
		/// </summary>
		/// <param name="outVar"></param>
		/// <param name="outTermName"></param>
		/// <param name="op"></param>
		/// <param name="in1"></param>
		/// <param name="in1TermName1"></param>
		/// <param name="in2"></param>
		/// <param name="in2TermName"></param>
		public void AddRule(string outVar, string outTermName, FuzzyOperator op, string in1, string in1TermName, string in2, string in2TermName)
		{
			#region check for non-local references
			if (outVar != OutVar.Name)
				throw new KeyNotFoundException("Only the local output variable can be use; rule refers to " + outVar + "; local OutVar.Name = " + OutVar.Name);
			if (InVars[in1] == null)
				throw new KeyNotFoundException("Only the local input variables can be use; rule refers to " + in1 + "; local InVars: " + InVars.Keys.ToArray().ToString());
			if (InVars[in2] == null)
				throw new KeyNotFoundException("Only the local input variables can be use; rule refers to " + in2 + "; local InVars: " + InVars.Keys.ToArray().ToString());
			if (InVars[in1][in1TermName] == null)
				throw new KeyNotFoundException("Only existing terms can be use; rule refers to " + in1TermName);
			if (InVars[in2][in2TermName] == null)
				throw new KeyNotFoundException("Only existing terms can be use; rule refers to " + in2TermName);
			#endregion
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in1], in1TermName, op, InVars[in2], in2TermName));
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in2], in2TermName, op, InVars[in1], in1TermName));
		}
		/// <summary>
		/// Creates mirrored rules; useful if in1 and in2 are of the same type and can be given in any order/can be swapped, ie marks
		/// </summary>
		/// <param name="outVar"></param>
		/// <param name="outTermName"></param>
		/// <param name="op"></param>
		/// <param name="in1"></param>
		/// <param name="in1TermName1"></param>
		/// <param name="in2"></param>
		/// <param name="in2TermName"></param>
		public void AddRule(Linguistic outVar, string outTermName, FuzzyOperator op, Linguistic in1, string in1TermName, Linguistic in2, string in2TermName)
		{
			#region check for non-local references
			if (outVar.Name != OutVar.Name)
				throw new KeyNotFoundException("Only the local output variable can be use; rule refers to " + outVar + "; local OutVar.Name = " + OutVar.Name);
			if (InVars[in1.Name] == null)
				throw new KeyNotFoundException("Only the local input variables can be use; rule refers to " + in1 + "; local InVars: " + InVars.Keys.ToArray().ToString());
			if (InVars[in2.Name] == null)
				throw new KeyNotFoundException("Only the local input variables can be use; rule refers to " + in2 + "; local InVars: " + InVars.Keys.ToArray().ToString());
			if (InVars[in1.Name][in1TermName] == null)
				throw new KeyNotFoundException("Only existing terms can be use; rule refers to " + in1TermName);
			if (InVars[in2.Name][in2TermName] == null)
				throw new KeyNotFoundException("Only existing terms can be use; rule refers to " + in2TermName);
			#endregion
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in1.Name], in1TermName, op, InVars[in2.Name], in2TermName));
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in2.Name], in2TermName, op, InVars[in1.Name], in1TermName));
		}
		/// <summary>
		/// Creates 4 rules that are permutations of the two given terms per variable; can be used to define that two terms of each input variable contribute to the same term of the ouotput variable
		/// </summary>
		/// <example>if (TileSize too small OR Total TileSize too big) AND (TotalSize too small or TotalSize too big) then TileTotalCombination is useless</example>
		/// <param name="outVar"></param>
		/// <param name="outTermName"></param>
		/// <param name="in1"></param>
		/// <param name="in1TermName1"></param>
		/// <param name="in1TermName2"></param>
		/// <param name="in2"></param>
		/// <param name="in2TermName1"></param>
		/// <param name="in2TermName2"></param>
		public void AddRulePermutations(string outVar, string outTermName, string in1, string in1TermName1, string in1TermName2, string in2, string in2TermName1, string in2TermName2)
		{
			#region check for non-local references
			if (outVar != OutVar.Name)
				throw new KeyNotFoundException("Only the local output variable can be use; rule refers to " + outVar + "; local OutVar.Name = " + OutVar.Name);
			if (InVars[in1] == null)
				throw new KeyNotFoundException("Only the local input variables can be use; rule refers to " + in1 + "; local InVars: " + InVars.Keys.ToArray().ToString());
			if (InVars[in2] == null)
				throw new KeyNotFoundException("Only the local input variables can be use; rule refers to " + in2 + "; local InVars: " + InVars.Keys.ToArray().ToString());
			if (InVars[in1][in1TermName1] == null)
				throw new KeyNotFoundException("Only existing terms can be use; rule refers to " + in1TermName1);
			if (InVars[in2][in2TermName1] == null)
				throw new KeyNotFoundException("Only existing terms can be use; rule refers to " + in2TermName1);
			if (InVars[in1][in1TermName2] == null)
				throw new KeyNotFoundException("Only existing terms can be use; rule refers to " + in1TermName2);
			if (InVars[in2][in2TermName2] == null)
				throw new KeyNotFoundException("Only existing terms can be use; rule refers to " + in2TermName2);
			#endregion
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in1], in1TermName1, FuzzyOperator.AND, InVars[in2], in2TermName1));
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in1], in1TermName1, FuzzyOperator.AND, InVars[in2], in2TermName2));
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in1], in1TermName2, FuzzyOperator.AND, InVars[in2], in2TermName1));
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in1], in1TermName2, FuzzyOperator.AND, InVars[in2], in2TermName2));
		}
		/// <summary>
		/// Creates 4 rules that are permutations of the two given terms per variable; can be used to define that two terms of each input variable contribute to the same term of the ouotput variable
		/// </summary>
		/// <example>if (TileSize too small OR Total TileSize too big) AND (TotalSize too small or TotalSize too big) then TileTotalCombination is useless</example>
		/// <param name="outVar"></param>
		/// <param name="outTermName"></param>
		/// <param name="in1"></param>
		/// <param name="in1TermName1"></param>
		/// <param name="in1TermName2"></param>
		/// <param name="in2"></param>
		/// <param name="in2TermName1"></param>
		/// <param name="in2TermName2"></param>
		public void AddRulePermutations(Linguistic outVar, string outTermName, Linguistic in1, string in1TermName1, string in1TermName2, Linguistic in2, string in2TermName1, string in2TermName2)
		{
			#region check for non-local references
			if (outVar.Name != OutVar.Name)
				throw new KeyNotFoundException("Only the local output variable can be use; rule refers to " + outVar + "; local OutVar.Name = " + OutVar.Name);
			if (InVars[in1.Name] == null)
				throw new KeyNotFoundException("Only the local input variables can be use; rule refers to " + in1 + "; local InVars: " + InVars.Keys.ToArray().ToString());
			if (InVars[in2.Name] == null)
				throw new KeyNotFoundException("Only the local input variables can be use; rule refers to " + in2 + "; local InVars: " + InVars.Keys.ToArray().ToString());
			if (InVars[in1.Name][in1TermName1] == null)
				throw new KeyNotFoundException("Only existing terms can be use; rule refers to " + in1TermName1);
			if (InVars[in2.Name][in2TermName1] == null)
				throw new KeyNotFoundException("Only existing terms can be use; rule refers to " + in2TermName1);
			if (InVars[in1.Name][in1TermName2] == null)
				throw new KeyNotFoundException("Only existing terms can be use; rule refers to " + in1TermName2);
			if (InVars[in2.Name][in2TermName2] == null)
				throw new KeyNotFoundException("Only existing terms can be use; rule refers to " + in2TermName2);
			#endregion
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in1.Name], in1TermName1, FuzzyOperator.AND, InVars[in2.Name], in2TermName1));
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in1.Name], in1TermName1, FuzzyOperator.AND, InVars[in2.Name], in2TermName2));
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in1.Name], in1TermName2, FuzzyOperator.AND, InVars[in2.Name], in2TermName1));
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in1.Name], in1TermName2, FuzzyOperator.AND, InVars[in2.Name], in2TermName2));
		}
		/// <summary>
		/// Creates rule with one condition; behaves like an assignment rather
		/// </summary>
		/// <param name="outVar"></param>
		/// <param name="outTermName"></param>
		/// <param name="in1"></param>
		/// <param name="in1TermName1"></param>
		public void AddRule(string outVar, string outTermName, string in1, string in1TermName)
		{
			#region check for non-local references
			if (outVar != OutVar.Name)
				throw new KeyNotFoundException("Only the local output variable can be use; rule refers to " + outVar + "; local OutVar.Name = " + OutVar.Name);
			if (InVars[in1] == null)
				throw new KeyNotFoundException("Only the local input variables can be use; rule refers to " + in1 + "; local InVars: " + InVars.Keys.ToArray().ToString());
			if (InVars[in1][in1TermName] == null)
				throw new KeyNotFoundException("Only existing terms can be use; rule refers to " + in1TermName);
			#endregion
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in1], in1TermName));
		}
		/// <summary>
		/// Creates rule with one condition; behaves like an assignment rather
		/// </summary>
		/// <param name="outVar"></param>
		/// <param name="outTermName"></param>
		/// <param name="in1"></param>
		/// <param name="in1TermName1"></param>
		public void AddRule(Linguistic outVar, string outTermName, Linguistic in1, string in1TermName)
		{
			#region check for non-local references
			if (outVar.Name != OutVar.Name)
				throw new KeyNotFoundException("Only the local output variable can be use; rule refers to " + outVar + "; local OutVar.Name = " + OutVar.Name);
			if (InVars[in1.Name] == null)
				throw new KeyNotFoundException("Only the local input variables can be use; rule refers to " + in1 + "; local InVars: " + InVars.Keys.ToArray().ToString());
			if (InVars[in1.Name][in1TermName] == null)
				throw new KeyNotFoundException("Only existing terms can be use; rule refers to " + in1TermName);
			#endregion
			Rules.Add(new FuzzyRule(OutVar, outTermName, InVars[in1.Name], in1TermName));
		}
		public Reasoning(Linguistic Out, Linguistic In0, Linguistic In1)
		{
			OutVar = Out;
			InVars = new Dictionary<string, Linguistic>();
			InVars[In0.Name] = In0;
			InVars[In1.Name] = In1;
			Rules = new List<FuzzyRule>();
		}
		/// <summary>
		/// Predefined reasoning for four input variables
		/// </summary>
		/// <param name="Out"></param>
		/// <param name="In0"></param>
		/// <param name="In1"></param>
		/// <param name="In2"></param>
		/// <param name="In3"></param>
		public Reasoning(Linguistic Out, Linguistic In0, Linguistic In1, Linguistic In2, Linguistic In3)
			: this(Out, In0, In1)
		{
			InVars[In2.Name] = In2;
			InVars[In3.Name] = In3;
		}
	}
}
