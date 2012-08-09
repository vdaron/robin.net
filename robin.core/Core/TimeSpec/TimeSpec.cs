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
using robin.core;

namespace robin.Core.TimeSpec
{
	enum TimeSpecType
	{
		Absolute,Start,End
	}
	/// <summary>
	/// Simple class to represent time obtained by parsing at-style date specification (described
	/// in detail on the rrdfetch man page. <see cref="TimeParser"/> for more info
	/// for more information.
	/// </summary>
	internal class TimeSpec
	{
		private readonly String dateString;

		private TimeSpec context;

		internal int Year, Month, Day, Hour, Min, Sec;
		internal int DeltaYear, DeltaMonth, DeltaDay, DeltaHour, DeltaMinute, DeltaSecond;
		internal TimeSpecType Type = TimeSpecType.Absolute;
		internal int Wday;

		internal TimeSpec(String dateString)
		{
			this.dateString = dateString;
		}

		internal void Localtime(long timestamp)
		{
			DateTime date = Util.GetDateTime(timestamp);
			Year = date.Year - 1900;
			Month = date.Month;
			Day = date.Day;
			Hour = date.Hour;
			Min = date.Minute;
			Sec = date.Second;
			Wday = date.DayOfWeek - DayOfWeek.Sunday;
		}

		internal DateTime GetTime()
		{
			DateTime gc;
			// absoulte time, this is easy
			if (Type == TimeSpecType.Absolute)
			{
				gc = new DateTime(Year + 1900, Month, Day, Hour, Min, Sec);
			}
				// relative time, we need a context to evaluate it
			else if (context != null && context.Type == TimeSpecType.Absolute)
			{
				gc = context.GetTime();
			}
				// how would I guess what time it was?
			else
			{
				throw new RrdException("Relative times like '" +
				                       dateString + "' require proper absolute context to be evaluated");
			}
			gc = gc.AddYears(DeltaYear);
			gc = gc.AddMonths(DeltaMonth);
			gc = gc.AddDays(DeltaDay);
			gc = gc.AddHours(DeltaHour);
			gc = gc.AddMinutes(DeltaMinute);
			gc = gc.AddSeconds(DeltaSecond);
			return gc;
		}

		/// <summary>
		/// Returns the corresponding timestamp (seconds since Epoch).
		/// <code>
		/// TimeParser p = new TimeParser("now-1day");
		/// TimeSpec ts = p.Parse();
		/// Console.WriteLine("Timestamp was: " + ts.GetTimestamp();
		/// </code>
		/// </summary>
		/// <returns>Timestamp (in seconds, no milliseconds)</returns>
		public long GetTimestamp()
		{
			return Util.GetTimestamp(GetTime());
		}

		public override string ToString()
		{
			return (Type == TimeSpecType.Absolute ? "ABSTIME" : Type == TimeSpecType.Start ? "START" : "END") +
			       ": " + Year + "/" + Month + "/" + Day +
			       "/" + Hour + "/" + Min + "/" + Sec + " (" +
			       DeltaYear + "/" + DeltaMonth + "/" + DeltaDay +
			       "/" + DeltaHour + "/" + DeltaMinute + "/" + DeltaSecond + ")";
		}

		/// <summary>
		/// Use this static method to resolve relative time references and obtain the corresponding
		/// DateTime objects. Example:
		/// <code>
		/// TimeParser pStart = new TimeParser("now-1month"); // starting time
		/// TimeParser pEnd = new TimeParser("start+1week");  // ending time
		/// TimeSpec specStart = pStart.Parse();
		/// TimeSpec specEnd = pEnd.Parse();
		/// DateTime[] gc = TimeSpec.GetTimes(specStart, specEnd);
		/// </code>
		/// </summary>
		/// <param name="spec1">Starting time specification</param>
		/// <param name="spec2">Ending time specification</param>
		/// <returns>Two element array containing DateTime objects</returns>
		public static DateTime[] GetTimes(TimeSpec spec1, TimeSpec spec2)
		{
			if (spec1.Type == TimeSpecType.Start || spec2.Type == TimeSpecType.End)
			{
				throw new RrdException("Recursive time specifications not allowed");
			}
			spec1.context = spec2;
			spec2.context = spec1;
			return new[]
			       	{
			       		spec1.GetTime(),
			       		spec2.GetTime()
			       	};
		}

		/// <summary>
		/// Use this static method to resolve relative time references and obtain the corresponding
		/// timestamps (seconds since epoch). Example:
		/// <pre>
		/// TimeParser pStart = new TimeParser("now-1month"); // starting time
		/// TimeParser pEnd = new TimeParser("start+1week");  // ending time
		/// TimeSpec specStart = pStart.Parse();
		/// TimeSpec specEnd = pEnd.Parse();
		/// long[] ts = TimeSpec.GetTimestamps(specStart, specEnd);
		/// </pre>
		/// </summary>
		/// <param name="spec1">Starting time specification</param>
		/// <param name="spec2">Ending time specification</param>
		/// <returns>array containing two timestamps (in seconds since epoch)</returns>
		public static long[] GetTimestamps(TimeSpec spec1, TimeSpec spec2)
		{
			DateTime[] gcs = GetTimes(spec1, spec2);
			return new[]
			       	{
			       		Util.GetTimestamp(gcs[0]), Util.GetTimestamp(gcs[1])
			       	};
		}
	}
}