/*******************************************************************************
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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using robin.core;
using robin.core.jrrd;
using robin.Core.TimeSpec;

namespace robin.tests
{
	
/**
 * Mostly about testing TimeParser; relies on TimeSpec to be working as well (and to some degree exercises it implicitly; 
 * if you break TimeSpec, these tests may fail when doing relative times (e.g start = X, end = start+100 and the like)
 *  
 * NB: if you're adding tests here, you may wish to consider replicating them in RrdGraphControllerTest in OpenNMS as well
 * Or not.  As you see fit :)
 * 
 * @author cmiskell
 *
 */
 [TestClass]
public class TimeParserTest {
	
	private static int ONE_HOUR_IN_MILLIS=60*60*1000;
	private static int ONE_DAY_IN_MILLIS=24 * ONE_HOUR_IN_MILLIS;
	//private final static int ONE_NIGHT_IN_PARIS = 0x7a57e1e55;

	
	[TestInitialize]
	public void setup() {
		//Yeah, this looks weird, but there's method to my madness.  
		// Many of these tests create Calendars based on the current time, then use the 
		// parsing code which will assume something about when 'now' is, often the current day/week
		// If we're running around midnight and the Calendar gets created on one day and the parsing
		// happens on the next, you'll get spurious failures, which would be really annoying and hard
		// to replicate.  Initial tests show that most tests take ~100ms, so if it's within 10 seconds
		// of midnight, wait for 30 seconds (and print a message saing why)
		DateTime now = DateTime.Now;
		if(now.Hour == 23 && now.Minute == 59 && now.Second > 50) {
			Thread.Sleep(30000);
		}
	}
	
	private long[] parseTimes(String startTime, String endTime)
	{
		TimeParser startParser = new TimeParser(startTime);
		TimeParser endParser = new TimeParser(endTime);
		try
		{
			TimeSpec specStart = startParser.Parse();
			TimeSpec specEnd = endParser.Parse();
			return TimeSpec.GetTimestamps(specStart, specEnd);
		}
		catch (RrdException e)
		{
			throw new ArgumentException(
				"Could not parse start '" + startTime + "' and end '" + endTime + "' as valid time specifications", e);
		}
	}

 	/**
	 * Set all fields smaller than an hour to 0.
	 * @param now
	 */
	private DateTime clearMinutesSecondsAndMiliseconds(DateTime cal) {
		return cal.Subtract(new TimeSpan(0,0,cal.Minute,cal.Second,cal.Millisecond));
	}

	/**
	 * Set all fields smaller than a day to 0 (rounding down to midnight effectively)
	 * @param now
	 */
	private DateTime clearTime(DateTime cal) {
		return cal.Subtract(cal.TimeOfDay);
	}

	private DateTime changeMonth(DateTime cal, int month)
	{
		return cal.AddMonths(month - cal.Month);
	}
	private DateTime changeDay(DateTime cal, int day)
	{
		return cal.AddDays(day - cal.Day);
	}
	private DateTime changeYear(DateTime cal, int year)
	{
		return cal.AddYears(year -cal.Year);
	}
	private DateTime changeHour(DateTime cal, int hour)
	{
		return cal.AddHours(hour - cal.Hour);
	}
	private DateTime changeMinute(DateTime cal, int minute)
	{
		return cal.AddMinutes(minute - cal.Minute);
	}
	private DateTime changeDayOfWeek(DateTime cal, DayOfWeek dow)
	{
		return cal.AddDays(dow - cal.DayOfWeek);
	}
	/**
	 * Kinda like the JUnit asserts for doubles, which allows an "epsilon"
	 * But this is for integers, and with a specific description in the assert
	 * just for timestamps.
	 * 
	 * All times are expected in milliseconds
	 * 
	 * @param expected - expected value
	 * @param actual - actual value
	 * @param epsilon - the maximum difference
	 * @param desc - some description of the time.  Usually "start" or "end", could be others
	 */
	private void assertTimestampsEqualWithEpsilon(long expected, long actual, int epsilon, String desc) {
		Assert.IsTrue(Math.Abs(actual - expected) < epsilon, "Expected a "+desc+" time within "+epsilon+"ms of "+ expected
				+ " but got " + actual);
	}
	
	/**
	 * Test the specification of just an "hour" (assumed today) for
	 * both start and end, using the 24hour clock
	 */
	[TestMethod]
	public void test24HourClockHourTodayStartEndTime()
	{
		DateTime startDate = DateTime.Now;
		startDate = clearTime(startDate);
		startDate = startDate.AddHours(8);//8am

		DateTime endDate = DateTime.Now;
		endDate = clearTime(endDate);
		endDate = endDate.AddHours(16);//4pm
		
		long[] result = this.parseTimes("08", "16");
		long start = result[0];
		long end = result[1];

		Assert.AreEqual(startDate.GetTimestamp(), start);
		Assert.AreEqual(endDate.GetTimestamp(), end);
	}
	
	//**
	// * Test the specification of just an "hour" (assumed today) start using "midnight", end = now
	// */
	[TestMethod]
	public void testMidnightToday() {
		DateTime startDate = DateTime.Now;
		startDate = clearTime(startDate);

	   long[] result = this.parseTimes("midnight", "16");
	   long start = result[0];

	   Assert.AreEqual(startDate.GetTimestamp(), start);

	}

	//**
	// * Test the specification of just an "hour" (assumed today) start using "noon", end = now
	// */
	[TestMethod]
	public void testNoonToday() {
		DateTime startDate = DateTime.Now;
		startDate = clearTime(startDate);
		startDate = startDate.AddHours(12); // noon
		
	   long[] result = this.parseTimes("noon", "16");
	   long start = result[0];

	   Assert.AreEqual(startDate.GetTimestamp(), start);

	}

	//**
	// * Test the specification of just an "hour" (assumed today) for
	// * both start and end, using am/pm designators
	// */
	[TestMethod]
	public void testAMPMClockHourTodayStartEndTime() {
		DateTime startDate = DateTime.Now;
		startDate = clearTime(startDate);
		startDate = startDate.AddHours(8);//8am

		DateTime endDate = DateTime.Now;
		endDate = clearTime(endDate);
		endDate = endDate.AddHours(16);//4pm
		
	   long[] result = this.parseTimes("8am", "4pm");
	   long start = result[0];
	   long end = result[1];

		Assert.AreEqual(startDate.GetTimestamp(), start);
		Assert.AreEqual(endDate.GetTimestamp(), end);

	}

	//**
	// * Test that we can explicitly use the term "today", e.g. "today 9am"
	// * NB: This was failing in 1.5.12, so well worth testing :)
	// */
	[TestMethod]
	public void testTodayTime() {
		DateTime startDate = DateTime.Now;
		startDate = clearTime(startDate);
		startDate = startDate.AddHours(9);//9am

		DateTime endDate = DateTime.Now;
		endDate = clearTime(endDate);
		endDate = endDate.AddHours(17);//5pm
		
	   long[] result = this.parseTimes("9am today", "5pm today");
	   long start = result[0];
	   long end = result[1];

		Assert.AreEqual(startDate.GetTimestamp(), start);
		Assert.AreEqual(endDate.GetTimestamp(), end);

	}

	//**
	// * Test that we can explicitly use the term "yesterday", e.g. "yesterday 9am"
	// * NB: This was failing in 1.5.12, so well worth testing :)
	// */
	//TODO: This test is failing the first day of the month
	[TestMethod]
	public void testYesterdayTime() {
		DateTime startDate = DateTime.Now;
		startDate = clearTime(startDate);
		startDate = startDate.AddHours(9);//9am
		startDate = startDate.AddDays(-1);

		DateTime endDate = DateTime.Now;
		endDate = clearTime(endDate);
		endDate = endDate.AddHours(17);//5pm
		endDate = endDate.AddDays(-1);
		
	   long[] result = parseTimes("9am yesterday", "5pm yesterday");
	   long start = result[0];
	   long end = result[1];

		Assert.AreEqual(startDate.GetTimestamp(), start);
		Assert.AreEqual(endDate.GetTimestamp(), end);
	}


	//**
	// * Test that we can explicitly use the term "tomorrow", e.g. "tomorrow 9am"
	// * NB: This was failing in 1.5.12, so well worth testing :)
	// */
	[TestMethod]
	public void testTomorrowTime() {
		DateTime startDate = DateTime.Now;
		startDate = clearTime(startDate);
		startDate = startDate.AddHours(9);//9am
		startDate = startDate.AddDays(1);

		DateTime endDate = DateTime.Now;
		endDate = clearTime(endDate);
		endDate = endDate.AddHours(17);//5pm
		endDate = endDate.AddDays(1);
		
	   long[] result = this.parseTimes("9am tomorrow", "5pm tomorrow");
	   long start = result[0];
	   long end = result[1];

		Assert.AreEqual(startDate.GetTimestamp(), start);
		Assert.AreEqual(endDate.GetTimestamp(), end);
	}

	//**
	// * Tests a simple negative hour offset
	// * 
	// * Test the simple start=-1h example from rrdfetch man page.  
	// * End is "now" (implied if missing in most user interfaces), 
	// * start should be 1 hour prior to now 
	// */
	[TestMethod]
	public void testSimpleNegativeOffset()
	{
		DateTime startDate = DateTime.Now;
		startDate = startDate.AddHours(-1);

		DateTime endDate = DateTime.Now;

		long[] result = this.parseTimes("-1h", "now");
		long start = result[0]*1000;
		long end = result[1]*1000;

		assertTimestampsEqualWithEpsilon(endDate.GetMilisecondsTimestamp(), end, 1000, "end");
		assertTimestampsEqualWithEpsilon(startDate.GetMilisecondsTimestamp(), start, 1000, "start");
	}

 	//**
	// * Test a start relative to an end that isn't now
	// */
	[TestMethod]
	public void testRelativeStartOffsetEnd() {
		DateTime startDate = DateTime.Now;
		startDate = startDate.AddHours(-3);

		DateTime endDate = DateTime.Now;
		endDate = endDate.AddHours(-1);

	   //End is 1 hour ago; start is 2 hours before that
	   long[] result = this.parseTimes("end-2h", "-1h");
	   long start = result[0] * 1000;
	   long end = result[1] * 1000;

		assertTimestampsEqualWithEpsilon(endDate.GetMilisecondsTimestamp(), end, 1000, "end");
		assertTimestampsEqualWithEpsilon(startDate.GetMilisecondsTimestamp(), start, 1000, "start");

	}

	//**
	// * Test a start relative to an end that isn't now
	// */
	[TestMethod]
	public void testRelativeStartOffsetEndAbbreviatedEnd() {
		DateTime startDate = DateTime.Now;
		startDate = startDate.AddHours(-3);

		DateTime endDate = DateTime.Now;
		endDate = endDate.AddHours(-1);


	   //End is 1 hour ago; start is 2 hours before that
	   long[] result = this.parseTimes("e-2h", "-1h");
	   long start = result[0] * 1000;
	   long end = result[1] * 1000;

		assertTimestampsEqualWithEpsilon(endDate.GetMilisecondsTimestamp(), end, 1000, "end");
		assertTimestampsEqualWithEpsilon(startDate.GetMilisecondsTimestamp(), start, 1000, "start");
	}

	//**
	// * Test an end relative to a start that isn't now
	// */
	[TestMethod]
	public void testRelativeEndOffsetStart() {
		DateTime startDate = DateTime.Now;
		startDate = startDate.AddHours(-4);

		DateTime endDate = DateTime.Now;
		endDate = endDate.AddHours(-2);

	   long[] result = this.parseTimes("-4h", "start+2h");
	   long start = result[0] * 1000;
	   long end = result[1] * 1000;

		assertTimestampsEqualWithEpsilon(endDate.GetMilisecondsTimestamp(), end, 1000, "end");
		assertTimestampsEqualWithEpsilon(startDate.GetMilisecondsTimestamp(), start, 1000, "start");

	}

	//**
	// * Test an end relative to a start that isn't now - abbreviated start (s)
	// */
	[TestMethod]
	public void testRelativeEndOffsetStartAbbreviatedStart() {
		DateTime startDate = DateTime.Now;
		startDate = startDate.AddHours(-4);

		DateTime endDate = DateTime.Now;
		endDate = endDate.AddHours(-2);

	   long[] result = this.parseTimes("-4h", "s+2h");
	   long start = result[0] * 1000;
	   long end = result[1] * 1000;

		assertTimestampsEqualWithEpsilon(endDate.GetMilisecondsTimestamp(), end, 1000, "end");
		assertTimestampsEqualWithEpsilon(startDate.GetMilisecondsTimestamp(), start, 1000, "start");
	}

	//**
	// * Test hour:min, and hour.min syntaxes
	// */
	[TestMethod]
	public void testHourMinuteSyntax() {
		
		DateTime startDate = DateTime.Now;
		int seconds = startDate.Second;
		startDate = clearTime(startDate);
		startDate = startDate.AddHours(8);
		startDate = startDate.AddMinutes(30);

		DateTime endDate = DateTime.Now;
		endDate = clearTime(endDate);
		endDate = endDate.AddHours(16);
		endDate = endDate.AddMinutes(45);

	   //Mixed syntaxes FTW; two tests in one
	   //This also exercises the test of the order of parsing (time then day).  If
	   // that order is wrong, 8.30 could be (and was at one point) interpreted as a day.month 
	   long[] result = this.parseTimes("8.30", "16:45");
	   long start = result[0];
	   long end = result[1];

	   Assert.AreEqual(startDate.GetTimestamp(), start);
		Assert.AreEqual(endDate.GetTimestamp(), end);

	}
	
	/**
	 * Test a plain date specified as DD.MM.YYYY
	 */
	[TestMethod]
	public void testDateWithDots() {
		DateTime startDate = new DateTime(1980,1,1);
		DateTime endDate = new DateTime(1980,12,15);
		
	   //Start is a simple one; end ensures we have our days/months around the right way.
	   long[] result = this.parseTimes("00:00 01.01.1980", "00:00 15.12.1980");
	   long start = result[0];
	   long end = result[1];

		Assert.AreEqual(startDate.GetTimestamp(), start);
		Assert.AreEqual(endDate.GetTimestamp(), end);
	}

	/**
	 * Test a plain date specified as DD/MM/YYYY
	 */
	[TestMethod]
	public void testDateWithSlashes()
	{
		DateTime startDate = new DateTime(1980, 1, 1);
		DateTime endDate = new DateTime(1980, 12, 15);

		//Start is a simple one; end ensures we have our days/months around the right way.
		long[] result = this.parseTimes("00:00 01/01/1980", "00:00 12/15/1980");
		long start = result[0];
		long end = result[1];

		Assert.AreEqual(startDate.GetTimestamp(), start);
		Assert.AreEqual(endDate.GetTimestamp(), end);
	}

 	/**
	 * Test a plain date specified as YYYYMMDD
	 */
	[TestMethod]
	public void testDateWithNoDelimiters()
	{
		DateTime startDate = new DateTime(1980, 1, 1);
		DateTime endDate = new DateTime(1980, 12, 15);

		//Start is a simple one; end ensures we have our days/months around the right way.
		long[] result = this.parseTimes("00:00 19800101", "00:00 19801215");
		long start = result[0];
		long end = result[1];

		Assert.AreEqual(startDate.GetTimestamp(), start);
		Assert.AreEqual(endDate.GetTimestamp(), end);
	}

 	/**
	 * Test named month dates with no year
	 * 
	 * NB: Seems silly to test all, just test that an arbitrary one works, in both short and long form  
	 * 
	 * If we find actual problems with specific months, we can add more tests
	 */
	[TestMethod]
	public void testNamedMonthsNoYear()
	{
		DateTime startDate = DateTime.Now;
		startDate = clearTime(startDate);
		startDate = changeMonth(startDate, 3);
		startDate = changeDay(startDate,1);

		DateTime endDate = DateTime.Now;
		endDate = clearTime(endDate);
		endDate = changeMonth(endDate, 11);
		endDate = changeDay(endDate, 15);


	   //one short, one long month name
	   long[] result = this.parseTimes("00:00 Mar 1 ", "00:00 November 15");
	   long start = result[0];
	   long end = result[1];

		Assert.AreEqual(startDate.GetTimestamp(), start);
		Assert.AreEqual(endDate.GetTimestamp(), end);
	}

	/**
	 * Test named month dates with 2 digit years
	 * 
	 * NB: Seems silly to test all, just test that an arbitrary one works, in both short and long form
	 * If we find actual problems with specific months, we can add more tests
	 */
	[TestMethod]
	public void testNamedMonthsTwoDigitYear() {
		DateTime startDate = DateTime.Now;
		startDate = clearTime(startDate);
		startDate = changeMonth(startDate, 2);
		startDate = changeDay(startDate, 2);
		startDate = changeYear(startDate, 1980);

		DateTime endDate = DateTime.Now;
		endDate = clearTime(endDate);
		endDate = changeMonth(endDate, 10);
		endDate = changeDay(endDate, 16);
		endDate = changeYear(endDate, 1980);

	   long[] result = this.parseTimes("00:00 Feb 2 80", "00:00 October 16 80");
		long start = result[0];
		long end = result[1];

		Assert.AreEqual(startDate.GetTimestamp(), start);
		Assert.AreEqual(endDate.GetTimestamp(), end);

	}
	
	/**
	 * Test named month dates with 4 digit years
	 * 
	 * NB: Seems silly to test all, just test that an arbitrary one works, in both short and long form
	 * 
	 * If we find actual problems with specific months, we can add more tests
	 */
	[TestMethod]
	public void testNamedMonthsFourDigitYear()
	{
		DateTime startDate = DateTime.Now;
		startDate = clearTime(startDate);
		startDate = changeMonth(startDate, 4);
		startDate = changeDay(startDate, 6);
		startDate = changeYear(startDate, 1980);

		DateTime endDate = DateTime.Now;
		endDate = clearTime(endDate);
		endDate = changeMonth(endDate, 9);
		endDate = changeDay(endDate, 17);
		endDate = changeYear(endDate, 1980);

		long[] result = this.parseTimes("00:00 Apr 6 1980", "00:00 September 17 1980");
		long start = result[0];
		long end = result[1];

		Assert.AreEqual(startDate.GetTimestamp(), start);
		Assert.AreEqual(endDate.GetTimestamp(), end);
	}

 	/**
	 * Test day of week specification.  The expected behaviour is annoyingly murky; if these tests start failing
	 * give serious consideration to fixing the tests rather than the underlying code.
	 * 
	 */
	[TestMethod]
	public void testDayOfWeekTimeSpec() {
		DateTime startDate = DateTime.Now;
		startDate = clearTime(startDate);
		startDate = changeHour(startDate,12);
		startDate = changeDayOfWeek(startDate, DayOfWeek.Thursday);

		DateTime endDate = DateTime.Now;
		endDate = clearTime(endDate);
		endDate = changeHour(endDate, 18);
		endDate = changeDayOfWeek(endDate, DayOfWeek.Friday);
		
	   long[] result = this.parseTimes("noon Thursday", "6pm Friday");
		long start = result[0];
		long end = result[1];

		Assert.AreEqual(startDate.GetTimestamp(), start);
		Assert.AreEqual(endDate.GetTimestamp(), end);
	}
	
	/**
	 * Test some basic time offsets
	 */
	[TestMethod]
	public void testTimeOffsets1() {
		DateTime startDate = DateTime.Now;
		startDate = startDate.AddMinutes(-1);
		DateTime endDate= DateTime.Now;
		endDate = endDate.AddSeconds(-10);
		
	   long[] result = this.parseTimes("now - 1minute", "now-10 seconds");
		long start = result[0];
		long end = result[1];

		Assert.AreEqual(startDate.GetTimestamp(), start);
		Assert.AreEqual(endDate.GetTimestamp(), end);

	}
	
	/**
	 * Test some basic time offsets
	 * NB: Due to it's use of a "day" offset, this may fail around daylight savings time.
	 * Maybe (Depends how the parsing code constructs it's dates)
	 */
	[TestMethod]
	public void testTimeOffsets2() {
		DateTime startDate = DateTime.Now;
		startDate = startDate.AddDays(-1);
		DateTime endDate = DateTime.Now;
		endDate = endDate.AddHours(-3);

		
	   long[] result = this.parseTimes("now - 1 day", "now-3 hours");
		long start = result[0];
		long end = result[1];

		Assert.AreEqual(startDate.GetTimestamp(), start);
		Assert.AreEqual(endDate.GetTimestamp(), end);
	}

	/**
	 * Test some basic time offsets
	 */
	[TestMethod]
	public void testTimeOffsets3() {
		DateTime startDate = DateTime.Now;
		startDate = changeMonth(startDate,6);
		startDate = changeDay(startDate, 12);

		DateTime endDate = DateTime.Now;
		endDate = changeMonth(endDate, 7);
		endDate = changeDay(endDate, 12);
		endDate = endDate.AddDays(-21);

		
	   long[] result = this.parseTimes("Jul 12 - 1 month", "Jul 12 - 3 weeks");
		long start = result[0];
		long end = result[1];

		Assert.AreEqual(startDate.GetTimestamp(), start);
		Assert.AreEqual(endDate.GetTimestamp(), end);

	}

	/**
	 * Test another basic time offset
	 */
	[TestMethod]
	public void testTimeOffsets4() {

		DateTime endDate = DateTime.Now;
		endDate = changeYear(endDate, 1980);
		endDate = changeMonth(endDate, 7);
		endDate = changeDay(endDate, 12);
		endDate = clearTime(endDate);

		DateTime startDate = endDate.AddYears(-1);

	   long[] result = this.parseTimes("end - 1 year", "00:00 12.07.1980");
		long start = result[0];
		long end = result[1];

		Assert.AreEqual(startDate.GetTimestamp(), start);
		Assert.AreEqual(endDate.GetTimestamp(), end);
	}
	
	/**
	 * Test some complex offset examples (per the rrdfetch man page)
	 */
	[TestMethod]
	public void complexTest1() {

		DateTime startDate = DateTime.Now;
		startDate = startDate.AddDays(-1);
		startDate = changeHour(startDate, 9);
		startDate = clearMinutesSecondsAndMiliseconds(startDate);
		
	   long[] result = this.parseTimes("noon yesterday -3hours", "now");
	   long start = result[0];

		Assert.AreEqual(startDate.GetTimestamp(), start); 
	}
	
	/**
	 * Test some more complex offset examples
	 */
	[TestMethod]
	public void complexTest2() {

		DateTime startDate = DateTime.Now;
		startDate = startDate.AddHours(-5);
		startDate = startDate.AddMinutes(-45);

	   long[] result = this.parseTimes("-5h45min", "now");
	   long start = result[0];

		Assert.AreEqual(startDate.GetTimestamp(), start); 
	}

	/**
	 * Test some more complex offset examples
	 */
	[TestMethod]
	public void complexTest3() {

		DateTime startDate = DateTime.Now;
		startDate = startDate.AddMonths(-5);
		startDate = startDate.AddDays(-7-2);

	   long[] result = this.parseTimes("-5mon1w2d", "now");
		long start = result[0];

		Assert.AreEqual(startDate.GetTimestamp(), start); 
	}

	
}

}