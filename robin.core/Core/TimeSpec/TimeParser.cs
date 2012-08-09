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

/*
 * C# port of the Java port of Tobi's original parsetime.c routine
 */

using System;
using robin.core;

namespace robin.Core.TimeSpec
{
	/// <summary>
	/// Class which parses at-style time specification (described in detail on the rrdfetch man page),
	/// used in all RRDTool commands. This code is in most parts just a java port of Tobi's parsetime.c
	/// code.
	/// </summary>
	internal class TimeParser
	{
		private const int PREVIOUS_OP = -1;

		private readonly TimeScanner scanner;
		private readonly TimeSpec spec;

		private int op = TimeToken.PLUS;
		private int prevMultiplier = -1;
		private TimeToken token;

		/// <summary>
		/// Constructs TimeParser instance from the given input string.
		/// </summary>
		/// <param name="dateString">time specification string as described in detail on the rrdfetch man page</param>
		public TimeParser(String dateString)
		{
			scanner = new TimeScanner(dateString);
			spec = new TimeSpec(dateString);
		}

		private void ExpectToken(int desired, String errorMessage)
		{
			token = scanner.NextToken();
			if (token.Id != desired)
			{
				throw new RrdException(errorMessage);
			}
		}

		private void PlusMinus(int doop)
		{
			if (doop >= 0)
			{
				op = doop;
				ExpectToken(TimeToken.NUMBER, "There should be number after " +
				                              (op == TimeToken.PLUS ? '+' : '-'));
				prevMultiplier = -1; /* reset months-minutes guessing mechanics */
			}
			int delta = int.Parse(token.Value);
			token = scanner.NextToken();
			if (token.Id == TimeToken.MONTHS_MINUTES)
			{
				/* hard job to guess what does that -5m means: -5mon or -5min? */
				switch (prevMultiplier)
				{
					case TimeToken.DAYS:
					case TimeToken.WEEKS:
					case TimeToken.MONTHS:
					case TimeToken.YEARS:
						token = scanner.ResolveMonthsMinutes(TimeToken.MONTHS);
						break;
					case TimeToken.SECONDS:
					case TimeToken.MINUTES:
					case TimeToken.HOURS:
						token = scanner.ResolveMonthsMinutes(TimeToken.MINUTES);
						break;
					default:
						token = scanner.ResolveMonthsMinutes(delta < 6 ? TimeToken.MONTHS : TimeToken.MINUTES);
						break;
				}
			}
			prevMultiplier = token.Id;
			delta *= (op == TimeToken.PLUS) ? +1 : -1;
			switch (token.Id)
			{
				case TimeToken.YEARS:
					spec.DeltaYear += delta;
					break;
				case TimeToken.MONTHS:
					spec.DeltaMonth += delta;
					break;
				case TimeToken.WEEKS:
					delta *= 7;
					spec.DeltaDay += delta;
					break;
				case TimeToken.DAYS:
					spec.DeltaDay += delta;
					break;
				case TimeToken.HOURS:
					spec.DeltaHour += delta;
					break;
				case TimeToken.MINUTES:
					spec.DeltaMinute += delta;
					break;
				case TimeToken.SECONDS:
				default: // default is 'seconds'
					spec.DeltaSecond += delta;
					break;
			}
			// unreachable statement
			// throw new RrdException("Well-known time unit expected after " + delta);
		}

		/// <summary>
		/// Try and read a "timeofday" specification.  This method will be called
		/// when we see a plain number at the start of a time, which means we could be
		/// reading a time, or a day.  If it turns out to be a date, then this method restores
		/// the scanner state to what it was at entry, and returns without setting anything.
		/// </summary>
		private void TimeOfDay()
		{
			int minute = 0;
			/* save token status in case we must abort */
			scanner.SaveState();
			/* first pick out the time of day - we assume a HH (COLON|DOT) MM time */
			if (token.Value.Length > 2)
			{
				//Definitely not an hour specification; probably a date or something.  Give up now
				return;
			}
			int hour = int.Parse(token.Value);
			token = scanner.NextToken();
			if (token.Id == TimeToken.SLASH)
			{
				/* guess we are looking at a date */
				token = scanner.RestoreState();
				return;
			}
			if (token.Id == TimeToken.COLON || token.Id == TimeToken.DOT)
			{
				ExpectToken(TimeToken.NUMBER, "Parsing HH:MM or HH.MM syntax, expecting MM as number, got none");
				minute = int.Parse(token.Value);
				if (minute > 59)
				{
					throw new RrdException("Parsing HH:MM or HH.MM syntax, got MM = " +
					                       minute + " (>59!)");
				}
				token = scanner.NextToken();
				if (token.Id == TimeToken.DOT)
				{
					//Oh look, another dot; must have actually been a date in DD.MM.YYYY format.  Give up and return
					token = scanner.RestoreState();
					return;
				}
			}
			/* check if an AM or PM specifier was given */
			if (token.Id == TimeToken.AM || token.Id == TimeToken.PM)
			{
				if (hour > 12)
				{
					throw new RrdException("There cannot be more than 12 AM or PM hours");
				}
				if (token.Id == TimeToken.PM)
				{
					if (hour != 12)
					{
						/* 12:xx PM is 12:xx, not 24:xx */
						hour += 12;
					}
				}
				else
				{
					if (hour == 12)
					{
						/* 12:xx AM is 00:xx, not 12:xx */
						hour = 0;
					}
				}
				token = scanner.NextToken();
			}
			else if (hour > 23)
			{
				/* guess it was not a time then, probably a date ... */
				token = scanner.RestoreState();
				return;
			}

			spec.Hour = hour;
			spec.Min = minute;
			spec.Sec = 0;
			if (spec.Hour == 24)
			{
				spec.Hour = 0;
				spec.Day++;
			}
		}

		private void AssignDate(long mday, long mon, long year)
		{
			if (year > 138)
			{
				if (year > 1970)
				{
					year -= 1900;
				}
				else
				{
					throw new RrdException("Invalid year " + year +
					                       " (should be either 00-99 or >1900)");
				}
			}
			else if (year >= 0 && year < 38)
			{
				year += 100; /* Allow year 2000-2037 to be specified as   */
			} /* 00-37 until the problem of 2038 year will */
			/* arise for unices with 32-bit time_t     */
			if (year < 70)
			{
				throw new RrdException("Won't handle dates before epoch (01/01/1970), sorry");
			}
			spec.Year = (int) year;
			spec.Month = (int) mon;
			spec.Day = (int) mday;
		}

		private void Day()
		{
			long mday = 0, wday, mon, year = spec.Year;
			switch (token.Id)
			{
				case TimeToken.YESTERDAY:
					spec.DeltaDay--; // bug in java version : spec.day--; (crash first day of the month)
					token = scanner.NextToken();
					break;
				case TimeToken.TODAY: /* force ourselves to stay in today - no further processing */
					token = scanner.NextToken();
					break;
				case TimeToken.TOMORROW:
					spec.DeltaDay++; // bug in java version : spec.day++; (crash last day of the month)
					token = scanner.NextToken();
					break;
				case TimeToken.JAN:
				case TimeToken.FEB:
				case TimeToken.MAR:
				case TimeToken.APR:
				case TimeToken.MAY:
				case TimeToken.JUN:
				case TimeToken.JUL:
				case TimeToken.AUG:
				case TimeToken.SEP:
				case TimeToken.OCT:
				case TimeToken.NOV:
				case TimeToken.DEC:
					/* do month mday [year] */
					//Info: In C# Month are not 0 based, Add +1 here.
					//mon = (token.id - TimeToken.JAN);
					mon = (token.Id - TimeToken.JAN) + 1;
					ExpectToken(TimeToken.NUMBER, "the day of the month should follow month name");
					mday = long.Parse(token.Value);
					token = scanner.NextToken();
					if (token.Id == TimeToken.NUMBER)
					{
						year = long.Parse(token.Value);
						token = scanner.NextToken();
					}
					else
					{
						year = spec.Year;
					}
					AssignDate(mday, mon, year);
					break;
				case TimeToken.SUN:
				case TimeToken.MON:
				case TimeToken.TUE:
				case TimeToken.WED:
				case TimeToken.THU:
				case TimeToken.FRI:
				case TimeToken.SAT:
					/* do a particular day of the week */
					wday = (token.Id - TimeToken.SUN);
					spec.Day += (int) (wday - spec.Wday);
					token = scanner.NextToken();
					break;
				case TimeToken.NUMBER:
					/* get numeric <sec since 1970>, MM/DD/[YY]YY, or DD.MM.[YY]YY */
					// int tlen = token.value.length();
					mon = long.Parse(token.Value);
					if (mon > 10L*365L*24L*60L*60L)
					{
						spec.Localtime(mon);
						token = scanner.NextToken();
						break;
					}
					if (mon > 19700101 && mon < 24000101)
					{
						/*works between 1900 and 2400 */
						year = mon/10000;
						mday = mon%100;
						mon = (mon/100)%100;
						token = scanner.NextToken();
					}
					else
					{
						token = scanner.NextToken();
						if (mon <= 31 && (token.Id == TimeToken.SLASH || token.Id == TimeToken.DOT))
						{
							int sep = token.Id;
							ExpectToken(TimeToken.NUMBER, "there should be " +
							                              (sep == TimeToken.DOT ? "month" : "day") +
							                              " number after " +
							                              (sep == TimeToken.DOT ? '.' : '/'));
							mday = long.Parse(token.Value);
							token = scanner.NextToken();
							if (token.Id == sep)
							{
								ExpectToken(TimeToken.NUMBER, "there should be year number after " +
								                              (sep == TimeToken.DOT ? '.' : '/'));
								year = long.Parse(token.Value);
								token = scanner.NextToken();
							}
							/* flip months and days for European timing */
							if (sep == TimeToken.DOT)
							{
								long x = mday;
								mday = mon;
								mon = x;
							}
						}
					}
					//Info: In C# Month are not 0 based
					//mon--;
					//if (mon < 0 || mon > 11) {
					if (mon < 1 || mon > 12)
					{
						throw new RrdException("Did you really mean month " + (mon + 1));
					}
					if (mday < 1 || mday > 31)
					{
						throw new RrdException("I'm afraid that " + mday +
						                       " is not a valid day of the month");
					}
					AssignDate(mday, mon, year);
					break;
			}
		}

		/// <summary>
		/// Parses the input string specified in the constructor.
		/// </summary>
		/// <returns></returns>
		public TimeSpec Parse()
		{
			long now = Util.GetCurrentTime();
			int hr = 0;
			/* this MUST be initialized to zero for midnight/noon/teatime */
			/* establish the default time reference */
			spec.Localtime(now);
			token = scanner.NextToken();
			switch (token.Id)
			{
				case TimeToken.PLUS:
				case TimeToken.MINUS:
					break; /* jump to OFFSET-SPEC part */
				case TimeToken.START:
					spec.Type = TimeSpecType.Start;
					spec.Year = spec.Month = spec.Day = spec.Hour = spec.Min = spec.Sec = 0;
					token = scanner.NextToken();
					if (token.Id != TimeToken.PLUS && token.Id != TimeToken.MINUS)
					{
						throw new RrdException("Words 'start' or 'end' MUST be followed by +|- offset");
					}
					break;
				case TimeToken.END:
					spec.Type = TimeSpecType.End;
					spec.Year = spec.Month = spec.Day = spec.Hour = spec.Min = spec.Sec = 0;
					token = scanner.NextToken();
					if (token.Id != TimeToken.PLUS && token.Id != TimeToken.MINUS)
					{
						throw new RrdException("Words 'start' or 'end' MUST be followed by +|- offset");
					}
					break;
				case TimeToken.NOW:
					token = scanner.NextToken();
					if (token.Id != TimeToken.PLUS && token.Id != TimeToken.MINUS && token.Id != TimeToken.EOF)
					{
						throw new RrdException("If 'now' is followed by a token it must be +|- offset");
					}
					break;
					/* Only absolute time specifications below */
				case TimeToken.NUMBER:
					TimeOfDay();
					Day();
					if (token.Id != TimeToken.NUMBER)
					{
						break;
					}
					//Allows (but does not require) the time to be specified after the day.  This extends the rrdfetch specifiation
					TimeOfDay();
					break;
				case TimeToken.JAN:
				case TimeToken.FEB:
				case TimeToken.MAR:
				case TimeToken.APR:
				case TimeToken.MAY:
				case TimeToken.JUN:
				case TimeToken.JUL:
				case TimeToken.AUG:
				case TimeToken.SEP:
				case TimeToken.OCT:
				case TimeToken.NOV:
				case TimeToken.DEC:
				case TimeToken.TODAY:
				case TimeToken.YESTERDAY:
				case TimeToken.TOMORROW:
					Day();
					if (token.Id != TimeToken.NUMBER)
					{
						break;
					}
					//Allows (but does not require) the time to be specified after the day.  This extends the rrdfetch specifiation
					TimeOfDay();
					break;
				case TimeToken.TEATIME:
					spec.Hour = 16;
					spec.Min = 0;
					spec.Sec = 0;
					token = scanner.NextToken();
					Day();
					break;
				case TimeToken.NOON:
					spec.Hour = 12;
					spec.Min = 0;
					spec.Sec = 0;
					token = scanner.NextToken();
					Day();
					break;
				case TimeToken.MIDNIGHT:
					spec.Hour = 0;
					spec.Min = 0;
					spec.Sec = 0;
					token = scanner.NextToken();
					Day();
					break;
				default:
					throw new RrdException("Unparsable time: " + token.Value);
			}

			/*
		 * the OFFSET-SPEC part
		 *
		 * (NOTE, the sc_tokid was prefetched for us by the previous code)
		 */
			if (token.Id == TimeToken.PLUS || token.Id == TimeToken.MINUS)
			{
				scanner.SetContext(false);
				while (token.Id == TimeToken.PLUS || token.Id == TimeToken.MINUS ||
				       token.Id == TimeToken.NUMBER)
				{
					if (token.Id == TimeToken.NUMBER)
					{
						PlusMinus(PREVIOUS_OP);
					}
					else
					{
						PlusMinus(token.Id);
					}
					token = scanner.NextToken();
					/* We will get EOF eventually but that's OK, since
				token() will return us as many EOFs as needed */
				}
			}
			/* now we should be at EOF */
			if (token.Id != TimeToken.EOF)
			{
				throw new RrdException("Unparsable trailing text: " + token.Value);
			}
			return spec;
		}
	}
}