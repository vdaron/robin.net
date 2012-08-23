using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace System
{
	public static class ExtensionMethods
	{
		private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		/// <summary>
		/// Safely get the UTC <see cref="DateTime"/> for a given input.
		/// </summary>
		/// <remarks>
		/// Unlike <see cref="DateTime.ToUniversalTime"/>, this method will consider <see cref="DateTimeKind.Unspecified"/> source dates to
		/// originate in the universal time zone.
		/// (c) Mindtouch
		/// </remarks>
		/// <param name="date">Source date</param>
		/// <returns>Date in the UTC timezone.</returns>
		private static DateTime ToSafeUniversalTime(DateTime date)
		{
			if (date != DateTime.MinValue && date != DateTime.MaxValue)
			{
				switch (date.Kind)
				{
					case DateTimeKind.Unspecified:
						date = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, DateTimeKind.Utc);
						break;
					case DateTimeKind.Local:
						date = date.ToUniversalTime();
						break;
				}
			}
			return date;
		}

		public static long GetTimestamp(this DateTime aDateTime)
		{
			return (long)ToSafeUniversalTime(aDateTime).Subtract(Epoch).TotalSeconds;
		}

		public static long GetMilisecondsTimestamp(this DateTime aDateTime)
		{
			return (long)ToSafeUniversalTime(aDateTime).Subtract(Epoch).TotalMilliseconds;
		}

		public static DateTime ToDateTime(this long aTimeStamp)
		{
			return Epoch.AddSeconds(aTimeStamp);
		}

	}
}
