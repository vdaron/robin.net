/*******************************************************************************
 * Copyright (c) 2001-2005 Sasa Markovic and Ciaran Treanor.
 * Copyright (c) 2011 The OpenNMS Group, Inc.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 *******************************************************************************/

using System;
using System.Globalization;
using System.Threading;
using robin.core;

namespace robin.data
{
	internal class RpnCalculator
	{
		private const int TKN_VAR = 0;
		private const int TKN_NUM = 1;
		private const int TKN_PLUS = 2;
		private const int TKN_MINUS = 3;
		private const int TKN_MULT = 4;
		private const int TKN_DIV = 5;
		private const int TKN_MOD = 6;
		private const int TKN_SIN = 7;
		private const int TKN_COS = 8;
		private const int TKN_LOG = 9;
		private const int TKN_EXP = 10;
		private const int TKN_FLOOR = 11;
		private const int TKN_CEIL = 12;
		private const int TKN_ROUND = 13;
		private const int TKN_POW = 14;
		private const int TKN_ABS = 15;
		private const int TKN_SQRT = 16;
		private const int TKN_RANDOM = 17;
		private const int TKN_LT = 18;
		private const int TKN_LE = 19;
		private const int TKN_GT = 20;
		private const int TKN_GE = 21;
		private const int TKN_EQ = 22;
		private const int TKN_IF = 23;
		private const int TKN_MIN = 24;
		private const int TKN_MAX = 25;
		private const int TKN_LIMIT = 26;
		private const int TKN_DUP = 27;
		private const int TKN_EXC = 28;
		private const int TKN_POP = 29;
		private const int TKN_UN = 30;
		private const int TKN_UNKN = 31;
		private const int TKN_NOW = 32;
		private const int TKN_TIME = 33;
		private const int TKN_PI = 34;
		private const int TKN_E = 35;
		private const int TKN_AND = 36;
		private const int TKN_OR = 37;
		private const int TKN_XOR = 38;
		private const int TKN_PREV = 39;
		private const int TKN_INF = 40;
		private const int TKN_NEGINF = 41;
		private const int TKN_STEP = 42;
		private const int TKN_YEAR = 43;
		private const int TKN_MONTH = 44;
		private const int TKN_DATE = 45;
		private const int TKN_HOUR = 46;
		private const int TKN_MINUTE = 47;
		private const int TKN_SECOND = 48;
		private const int TKN_WEEK = 49;
		private const int TKN_SIGN = 50;
		private const int TKN_RND = 51;
		private readonly double[] calculatedValues;
		private readonly DataProcessor dataProcessor;

		private readonly String rpnExpression;
		private readonly String sourceName;
		private readonly RpnStack stack = new RpnStack();
		private readonly double timeStep;
		private readonly long[] timestamps;
		private readonly Token[] tokens;

		internal RpnCalculator(String rpnExpression, String sourceName, DataProcessor dataProcessor)
		{
			this.rpnExpression = rpnExpression;
			this.sourceName = sourceName;
			this.dataProcessor = dataProcessor;
			timestamps = dataProcessor.GetTimestamps();
			timeStep = timestamps[1] - timestamps[0];
			calculatedValues = new double[timestamps.Length];
			String[] st = rpnExpression.Split(new[] {", "}, StringSplitOptions.None);
			tokens = new Token[st.Length];
			for (int i = 0; i < st.Length; i++)
			{
				tokens[i] = CreateToken(st[i]);
			}
		}

		private Token CreateToken(String parsedText)
		{
			var token = new Token();
			if (Util.IsDouble(parsedText))
			{
				token.Id = TKN_NUM;
				token.Number = Util.ParseDouble(parsedText);
			}
			else if (parsedText.Equals("+"))
			{
				token.Id = TKN_PLUS;
			}
			else if (parsedText.Equals("-"))
			{
				token.Id = TKN_MINUS;
			}
			else if (parsedText.Equals("*"))
			{
				token.Id = TKN_MULT;
			}
			else if (parsedText.Equals("/"))
			{
				token.Id = TKN_DIV;
			}
			else if (parsedText.Equals("%"))
			{
				token.Id = TKN_MOD;
			}
			else if (parsedText.Equals("SIN"))
			{
				token.Id = TKN_SIN;
			}
			else if (parsedText.Equals("COS"))
			{
				token.Id = TKN_COS;
			}
			else if (parsedText.Equals("LOG"))
			{
				token.Id = TKN_LOG;
			}
			else if (parsedText.Equals("EXP"))
			{
				token.Id = TKN_EXP;
			}
			else if (parsedText.Equals("FLOOR"))
			{
				token.Id = TKN_FLOOR;
			}
			else if (parsedText.Equals("CEIL"))
			{
				token.Id = TKN_CEIL;
			}
			else if (parsedText.Equals("ROUND"))
			{
				token.Id = TKN_ROUND;
			}
			else if (parsedText.Equals("POW"))
			{
				token.Id = TKN_POW;
			}
			else if (parsedText.Equals("ABS"))
			{
				token.Id = TKN_ABS;
			}
			else if (parsedText.Equals("SQRT"))
			{
				token.Id = TKN_SQRT;
			}
			else if (parsedText.Equals("RANDOM"))
			{
				token.Id = TKN_RANDOM;
			}
			else if (parsedText.Equals("LT"))
			{
				token.Id = TKN_LT;
			}
			else if (parsedText.Equals("LE"))
			{
				token.Id = TKN_LE;
			}
			else if (parsedText.Equals("GT"))
			{
				token.Id = TKN_GT;
			}
			else if (parsedText.Equals("GE"))
			{
				token.Id = TKN_GE;
			}
			else if (parsedText.Equals("EQ"))
			{
				token.Id = TKN_EQ;
			}
			else if (parsedText.Equals("IF"))
			{
				token.Id = TKN_IF;
			}
			else if (parsedText.Equals("MIN"))
			{
				token.Id = TKN_MIN;
			}
			else if (parsedText.Equals("MAX"))
			{
				token.Id = TKN_MAX;
			}
			else if (parsedText.Equals("LIMIT"))
			{
				token.Id = TKN_LIMIT;
			}
			else if (parsedText.Equals("DUP"))
			{
				token.Id = TKN_DUP;
			}
			else if (parsedText.Equals("EXC"))
			{
				token.Id = TKN_EXC;
			}
			else if (parsedText.Equals("POP"))
			{
				token.Id = TKN_POP;
			}
			else if (parsedText.Equals("UN"))
			{
				token.Id = TKN_UN;
			}
			else if (parsedText.Equals("UNKN"))
			{
				token.Id = TKN_UNKN;
			}
			else if (parsedText.Equals("NOW"))
			{
				token.Id = TKN_NOW;
			}
			else if (parsedText.Equals("TIME"))
			{
				token.Id = TKN_TIME;
			}
			else if (parsedText.Equals("PI"))
			{
				token.Id = TKN_PI;
			}
			else if (parsedText.Equals("E"))
			{
				token.Id = TKN_E;
			}
			else if (parsedText.Equals("AND"))
			{
				token.Id = TKN_AND;
			}
			else if (parsedText.Equals("OR"))
			{
				token.Id = TKN_OR;
			}
			else if (parsedText.Equals("XOR"))
			{
				token.Id = TKN_XOR;
			}
			else if (parsedText.Equals("PREV"))
			{
				token.Id = TKN_PREV;
				token.Variable = sourceName;
				token.Values = calculatedValues;
			}
			else if (parsedText.StartsWith("PREV(") && parsedText.EndsWith(")"))
			{
				token.Id = TKN_PREV;
				token.Variable = parsedText.Substring(5, parsedText.Length - 1 - 5);
				token.Values = dataProcessor.GetValues(token.Variable);
			}
			else if (parsedText.Equals("INF"))
			{
				token.Id = TKN_INF;
			}
			else if (parsedText.Equals("NEGINF"))
			{
				token.Id = TKN_NEGINF;
			}
			else if (parsedText.Equals("STEP"))
			{
				token.Id = TKN_STEP;
			}
			else if (parsedText.Equals("YEAR"))
			{
				token.Id = TKN_YEAR;
			}
			else if (parsedText.Equals("MONTH"))
			{
				token.Id = TKN_MONTH;
			}
			else if (parsedText.Equals("DATE"))
			{
				token.Id = TKN_DATE;
			}
			else if (parsedText.Equals("HOUR"))
			{
				token.Id = TKN_HOUR;
			}
			else if (parsedText.Equals("MINUTE"))
			{
				token.Id = TKN_MINUTE;
			}
			else if (parsedText.Equals("SECOND"))
			{
				token.Id = TKN_SECOND;
			}
			else if (parsedText.Equals("WEEK"))
			{
				token.Id = TKN_WEEK;
			}
			else if (parsedText.Equals("SIGN"))
			{
				token.Id = TKN_SIGN;
			}
			else if (parsedText.Equals("RND"))
			{
				token.Id = TKN_RND;
			}
			else
			{
				token.Id = TKN_VAR;
				token.Variable = parsedText;
				token.Values = dataProcessor.GetValues(token.Variable);
			}
			return token;
		}

		internal double[] CalculateValues()
		{
			for (int slot = 0; slot < timestamps.Length; slot++)
			{
				ResetStack();
				foreach (Token token in tokens)
				{
					double x1, x2, x3;
					switch (token.Id)
					{
						case TKN_NUM:
							Push(token.Number);
							break;
						case TKN_VAR:
							Push(token.Values[slot]);
							break;
						case TKN_PLUS:
							Push(Pop() + Pop());
							break;
						case TKN_MINUS:
							x2 = Pop();
							x1 = Pop();
							Push(x1 - x2);
							break;
						case TKN_MULT:
							Push(Pop()*Pop());
							break;
						case TKN_DIV:
							x2 = Pop();
							x1 = Pop();
							Push(x1/x2);
							break;
						case TKN_MOD:
							x2 = Pop();
							x1 = Pop();
							Push(x1%x2);
							break;
						case TKN_SIN:
							Push(Math.Sin(Pop()));
							break;
						case TKN_COS:
							Push(Math.Cos(Pop()));
							break;
						case TKN_LOG:
							Push(Math.Log(Pop()));
							break;
						case TKN_EXP:
							Push(Math.Exp(Pop()));
							break;
						case TKN_FLOOR:
							Push(Math.Floor(Pop()));
							break;
						case TKN_CEIL:
							Push(Math.Ceiling(Pop()));
							break;
						case TKN_ROUND:
							Push(Math.Round(Pop()));
							break;
						case TKN_POW:
							x2 = Pop();
							x1 = Pop();
							Push(Math.Pow(x1, x2));
							break;
						case TKN_ABS:
							Push(Math.Abs(Pop()));
							break;
						case TKN_SQRT:
							Push(Math.Sqrt(Pop()));
							break;
						case TKN_RANDOM:
							var random = new Random();
							Push(random.Next());
							break;
						case TKN_LT:
							x2 = Pop();
							x1 = Pop();
							Push(x1 < x2 ? 1 : 0);
							break;
						case TKN_LE:
							x2 = Pop();
							x1 = Pop();
							Push(x1 <= x2 ? 1 : 0);
							break;
						case TKN_GT:
							x2 = Pop();
							x1 = Pop();
							Push(x1 > x2 ? 1 : 0);
							break;
						case TKN_GE:
							x2 = Pop();
							x1 = Pop();
							Push(x1 >= x2 ? 1 : 0);
							break;
						case TKN_EQ:
							x2 = Pop();
							x1 = Pop();
							Push(x1 == x2 ? 1 : 0);
							break;
						case TKN_IF:
							x3 = Pop();
							x2 = Pop();
							x1 = Pop();
							Push(x1 != 0 ? x2 : x3);
							break;
						case TKN_MIN:
							Push(Math.Min(Pop(), Pop()));
							break;
						case TKN_MAX:
							Push(Math.Max(Pop(), Pop()));
							break;
						case TKN_LIMIT:
							x3 = Pop();
							x2 = Pop();
							x1 = Pop();
							Push(x1 < x2 || x1 > x3 ? Double.NaN : x1);
							break;
						case TKN_DUP:
							Push(Peek());
							break;
						case TKN_EXC:
							x2 = Pop();
							x1 = Pop();
							Push(x2);
							Push(x1);
							break;
						case TKN_POP:
							Pop();
							break;
						case TKN_UN:
							Push(Double.IsNaN(Pop()) ? 1 : 0);
							break;
						case TKN_UNKN:
							Push(Double.NaN);
							break;
						case TKN_NOW:
							Push(Util.GetCurrentTime());
							break;
						case TKN_TIME:
							Push((long) Math.Round((double) timestamps[slot]));
							break;
						case TKN_PI:
							Push(Math.PI);
							break;
						case TKN_E:
							Push(Math.E);
							break;
						case TKN_AND:
							x2 = Pop();
							x1 = Pop();
							Push((x1 != 0 && x2 != 0) ? 1 : 0);
							break;
						case TKN_OR:
							x2 = Pop();
							x1 = Pop();
							Push((x1 != 0 || x2 != 0) ? 1 : 0);
							break;
						case TKN_XOR:
							x2 = Pop();
							x1 = Pop();
							Push(((x1 != 0 && x2 == 0) || (x1 == 0 && x2 != 0)) ? 1 : 0);
							break;
						case TKN_PREV:
							Push((slot == 0) ? Double.NaN : token.Values[slot - 1]);
							break;
						case TKN_INF:
							Push(Double.PositiveInfinity);
							break;
						case TKN_NEGINF:
							Push(Double.NegativeInfinity);
							break;
						case TKN_STEP:
							Push(timeStep);
							break;
						case TKN_YEAR:
							Push(GetDateField(Pop()).Year);
							break;
						case TKN_MONTH:
							Push(GetDateField(Pop()).Month);
							break;
						case TKN_DATE:
							Push(GetDateField(Pop()).Day);
							break;
						case TKN_HOUR:
							Push(GetDateField(Pop()).Hour);
							break;
						case TKN_MINUTE:
							Push(GetDateField(Pop()).Minute);
							break;
						case TKN_SECOND:
							Push(GetDateField(Pop()).Second);
							break;
						case TKN_WEEK:
							Push(Util.GetWeekNumber(GetDateField(Pop())));
							break;
						case TKN_SIGN:
							x1 = Pop();
							Push(Double.IsNaN(x1) ? Double.NaN : x1 > 0 ? +1 : x1 < 0 ? -1 : 0);
							break;
						case TKN_RND:
							var r = new Random();
							Push(Math.Floor(Pop()*r.Next()));
							break;
						default:
							throw new RrdException("Unexpected RPN token encountered, token.id=" + token.Id);
					}
				}
				calculatedValues[slot] = Pop();
				// check if stack is empty only on the first try
				if (slot == 0 && !IsStackEmpty)
				{
					throw new RrdException("Stack not empty at the end of calculation. " +
					                       "Probably bad RPN expression [" + rpnExpression + "]");
				}
			}
			return calculatedValues;
		}

		private static DateTime GetDateField(double timestamp)
		{
			return Util.GetDateTime((long) (timestamp*1000));
		}

		private void Push(double x)
		{
			stack.Push(x);
		}

		private double Pop()
		{
			return stack.Pop();
		}

		private double Peek()
		{
			return stack.Peek();
		}

		private void ResetStack()
		{
			stack.Reset();
		}

		private bool IsStackEmpty
		{
			get { return stack.IsEmpty; }
		}

		#region Nested type: RpnStack

		private class RpnStack
		{
			private static readonly int MAX_STACK_SIZE = 1000;
			private readonly double[] stack = new double[MAX_STACK_SIZE];
			private int pos;

			internal void Push(double x)
			{
				if (pos >= MAX_STACK_SIZE)
				{
					throw new RrdException("PUSH failed, RPN stack full [" + MAX_STACK_SIZE + "]");
				}
				stack[pos++] = x;
			}

			internal double Pop()
			{
				if (pos <= 0)
				{
					throw new RrdException("POP failed, RPN stack is empty ");
				}
				return stack[--pos];
			}

			internal double Peek()
			{
				if (pos <= 0)
				{
					throw new RrdException("PEEK failed, RPN stack is empty ");
				}
				return stack[pos - 1];
			}

			internal void Reset()
			{
				pos = 0;
			}

			internal bool IsEmpty
			{
				get { return pos <= 0; }
			}
		}

		#endregion

		#region Nested type: Token

		private class Token
		{
			public int Id = -1;
			public double Number = Double.NaN;
			public double[] Values;
			public String Variable;
		}

		#endregion
	}
}