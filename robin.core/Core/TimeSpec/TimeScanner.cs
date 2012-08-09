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
using System.Diagnostics;
using System.Text;

namespace robin.Core.TimeSpec
{
	internal class TimeScanner
	{
		private static readonly TimeToken[] WORDS = {
		                                            	new TimeToken("midnight", TimeToken.MIDNIGHT),
		                                            	/* 00:00:00 of today or tomorrow */
		                                            	new TimeToken("noon", TimeToken.NOON),
		                                            	/* 12:00:00 of today or tomorrow */
		                                            	new TimeToken("teatime", TimeToken.TEATIME),
		                                            	/* 16:00:00 of today or tomorrow */
		                                            	new TimeToken("am", TimeToken.AM), /* morning times for 0-12 clock */
		                                            	new TimeToken("pm", TimeToken.PM), /* evening times for 0-12 clock */
		                                            	new TimeToken("tomorrow", TimeToken.TOMORROW),
		                                            	new TimeToken("yesterday", TimeToken.YESTERDAY),
		                                            	new TimeToken("today", TimeToken.TODAY),
		                                            	new TimeToken("now", TimeToken.NOW),
		                                            	new TimeToken("n", TimeToken.NOW),
		                                            	new TimeToken("start", TimeToken.START),
		                                            	new TimeToken("s", TimeToken.START),
		                                            	new TimeToken("end", TimeToken.END),
		                                            	new TimeToken("e", TimeToken.END),
		                                            	new TimeToken("jan", TimeToken.JAN),
		                                            	new TimeToken("feb", TimeToken.FEB),
		                                            	new TimeToken("mar", TimeToken.MAR),
		                                            	new TimeToken("apr", TimeToken.APR),
		                                            	new TimeToken("may", TimeToken.MAY),
		                                            	new TimeToken("jun", TimeToken.JUN),
		                                            	new TimeToken("jul", TimeToken.JUL),
		                                            	new TimeToken("aug", TimeToken.AUG),
		                                            	new TimeToken("sep", TimeToken.SEP),
		                                            	new TimeToken("oct", TimeToken.OCT),
		                                            	new TimeToken("nov", TimeToken.NOV),
		                                            	new TimeToken("dec", TimeToken.DEC),
		                                            	new TimeToken("january", TimeToken.JAN),
		                                            	new TimeToken("february", TimeToken.FEB),
		                                            	new TimeToken("march", TimeToken.MAR),
		                                            	new TimeToken("april", TimeToken.APR),
		                                            	new TimeToken("may", TimeToken.MAY),
		                                            	new TimeToken("june", TimeToken.JUN),
		                                            	new TimeToken("july", TimeToken.JUL),
		                                            	new TimeToken("august", TimeToken.AUG),
		                                            	new TimeToken("september", TimeToken.SEP),
		                                            	new TimeToken("october", TimeToken.OCT),
		                                            	new TimeToken("november", TimeToken.NOV),
		                                            	new TimeToken("december", TimeToken.DEC),
		                                            	new TimeToken("sunday", TimeToken.SUN),
		                                            	new TimeToken("sun", TimeToken.SUN),
		                                            	new TimeToken("monday", TimeToken.MON),
		                                            	new TimeToken("mon", TimeToken.MON),
		                                            	new TimeToken("tuesday", TimeToken.TUE),
		                                            	new TimeToken("tue", TimeToken.TUE),
		                                            	new TimeToken("wednesday", TimeToken.WED),
		                                            	new TimeToken("wed", TimeToken.WED),
		                                            	new TimeToken("thursday", TimeToken.THU),
		                                            	new TimeToken("thu", TimeToken.THU),
		                                            	new TimeToken("friday", TimeToken.FRI),
		                                            	new TimeToken("fri", TimeToken.FRI),
		                                            	new TimeToken("saturday", TimeToken.SAT),
		                                            	new TimeToken("sat", TimeToken.SAT),
		                                            	new TimeToken(null, 0) /*** SENTINEL ***/
		                                            };

		private static readonly TimeToken[] MULTIPLIERS = {
		                                                  	new TimeToken("second", TimeToken.SECONDS),
		                                                  	/* seconds multiplier */
		                                                  	new TimeToken("seconds", TimeToken.SECONDS), /* (pluralized) */
		                                                  	new TimeToken("sec", TimeToken.SECONDS), /* (generic) */
		                                                  	new TimeToken("s", TimeToken.SECONDS), /* (short generic) */
		                                                  	new TimeToken("minute", TimeToken.MINUTES),
		                                                  	/* minutes multiplier */
		                                                  	new TimeToken("minutes", TimeToken.MINUTES), /* (pluralized) */
		                                                  	new TimeToken("min", TimeToken.MINUTES), /* (generic) */
		                                                  	new TimeToken("m", TimeToken.MONTHS_MINUTES), /* (short generic) */
		                                                  	new TimeToken("hour", TimeToken.HOURS), /* hours ... */
		                                                  	new TimeToken("hours", TimeToken.HOURS), /* (pluralized) */
		                                                  	new TimeToken("hr", TimeToken.HOURS), /* (generic) */
		                                                  	new TimeToken("h", TimeToken.HOURS), /* (short generic) */
		                                                  	new TimeToken("day", TimeToken.DAYS), /* days ... */
		                                                  	new TimeToken("days", TimeToken.DAYS), /* (pluralized) */
		                                                  	new TimeToken("d", TimeToken.DAYS), /* (short generic) */
		                                                  	new TimeToken("week", TimeToken.WEEKS), /* week ... */
		                                                  	new TimeToken("weeks", TimeToken.WEEKS), /* (pluralized) */
		                                                  	new TimeToken("wk", TimeToken.WEEKS), /* (generic) */
		                                                  	new TimeToken("w", TimeToken.WEEKS), /* (short generic) */
		                                                  	new TimeToken("month", TimeToken.MONTHS), /* week ... */
		                                                  	new TimeToken("months", TimeToken.MONTHS), /* (pluralized) */
		                                                  	new TimeToken("mon", TimeToken.MONTHS), /* (generic) */
		                                                  	new TimeToken("year", TimeToken.YEARS), /* year ... */
		                                                  	new TimeToken("years", TimeToken.YEARS), /* (pluralized) */
		                                                  	new TimeToken("yr", TimeToken.YEARS), /* (generic) */
		                                                  	new TimeToken("y", TimeToken.YEARS), /* (short generic) */
		                                                  	new TimeToken(null, 0) /*** SENTINEL ***/
		                                                  };

		private readonly String dateString;

		private int pos, pos_save;
		private TimeToken[] specials = WORDS;
		private TimeToken token, token_save;

		public TimeScanner(String dateString)
		{
			this.dateString = dateString;
		}

		public void SetContext(bool parsingWords)
		{
			specials = parsingWords ? WORDS : MULTIPLIERS;
		}

		public TimeToken NextToken()
		{
			var buffer = new StringBuilder("");
			while (pos < dateString.Length)
			{
				char c = dateString[pos++];
				if (char.IsWhiteSpace(c) || c == '_' || c == ',')
				{
					continue;
				}
				buffer.Append(c);
				if (char.IsDigit(c))
				{
					// pick as many digits as possible
					while (pos < dateString.Length)
					{
						char next = dateString[pos];
						if (char.IsDigit(next))
						{
							buffer.Append(next);
							pos++;
						}
						else
						{
							break;
						}
					}
					String value = buffer.ToString();
					return token = new TimeToken(value, TimeToken.NUMBER);
				}
				if (char.IsLetter(c))
				{
					// pick as many letters as possible
					while (pos < dateString.Length)
					{
						char next = dateString[pos];
						if (char.IsLetter(next))
						{
							buffer.Append(next);
							pos++;
						}
						else
						{
							break;
						}
					}
					String value = buffer.ToString();
					return token = new TimeToken(value, ParseToken(value));
				}
				switch (c)
				{
					case ':':
						return token = new TimeToken(":", TimeToken.COLON);
					case '.':
						return token = new TimeToken(".", TimeToken.DOT);
					case '+':
						return token = new TimeToken("+", TimeToken.PLUS);
					case '-':
						return token = new TimeToken("-", TimeToken.MINUS);
					case '/':
						return token = new TimeToken("/", TimeToken.SLASH);
					default:
						pos--;
						return token = new TimeToken(null, TimeToken.EOF);
				}
			}
			return token = new TimeToken(null, TimeToken.EOF);
		}

		internal TimeToken ResolveMonthsMinutes(int newId)
		{
			Debug.Assert(token.Id == TimeToken.MONTHS_MINUTES);
			Debug.Assert(newId == TimeToken.MONTHS || newId == TimeToken.MINUTES);

			return token = new TimeToken(token.Value, newId);
		}

		internal void SaveState()
		{
			token_save = token;
			pos_save = pos;
		}

		internal TimeToken RestoreState()
		{
			pos = pos_save;
			return token = token_save;
		}

		private int ParseToken(String arg)
		{
			for (int i = 0; specials[i].Value != null; i++)
			{
				if (specials[i].Value.Equals(arg, StringComparison.InvariantCultureIgnoreCase))
				{
					return specials[i].Id;
				}
			}
			return TimeToken.ID;
		}
	}
}