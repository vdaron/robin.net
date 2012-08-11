using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
	public static class DateTimeExtensions
	{
		private static readonly DateTime Epoch = new DateTime(1970, 1, 1);

		public static long GetTimestamp(this DateTime aDateTime)
		{
			return (long)(aDateTime - Epoch).TotalSeconds;
		}

		public static long GetMilisecondsTimestamp(this DateTime aDateTime)
		{
			return (long)(aDateTime - Epoch).TotalMilliseconds;
		}
	}

	public static class LongExtensions
	{
		private static readonly DateTime Epoch = new DateTime(1970, 1, 1);

		public static DateTime ToDateTime(this long aTimeStamp)
		{
			return Epoch.AddSeconds(aTimeStamp);
		}
	}
}
