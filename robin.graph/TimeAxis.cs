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
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading;
using robin.core;
using robin.data;

namespace robin.graph
{
class TimeAxis  {
	private static readonly TimeAxisSetting[] tickSettings = {
			new TimeAxisSetting(0, RrdGraphConstants.SECOND, 30, RrdGraphConstants.MINUTE, 5, RrdGraphConstants.MINUTE, 5, 0, "HH:mm"),
			new TimeAxisSetting(2, RrdGraphConstants.MINUTE, 1, RrdGraphConstants.MINUTE, 5, RrdGraphConstants.MINUTE, 5, 0, "HH:mm"),
			new TimeAxisSetting(5, RrdGraphConstants.MINUTE, 2, RrdGraphConstants.MINUTE, 10, RrdGraphConstants.MINUTE, 10, 0, "HH:mm"),
			new TimeAxisSetting(10, RrdGraphConstants.MINUTE, 5, RrdGraphConstants.MINUTE, 20, RrdGraphConstants.MINUTE, 20, 0, "HH:mm"),
			new TimeAxisSetting(30, RrdGraphConstants.MINUTE, 10, RrdGraphConstants.HOUR, 1, RrdGraphConstants.HOUR, 1, 0, "HH:mm"),
			new TimeAxisSetting(60, RrdGraphConstants.MINUTE, 30, RrdGraphConstants.HOUR, 2, RrdGraphConstants.HOUR, 2, 0, "HH:mm"),
			new TimeAxisSetting(180, RrdGraphConstants.HOUR, 1, RrdGraphConstants.HOUR, 6, RrdGraphConstants.HOUR, 6, 0, "HH:mm"),
			new TimeAxisSetting(600, RrdGraphConstants.HOUR, 6, RrdGraphConstants.DAY, 1, RrdGraphConstants.DAY, 1, 24 * 3600, "dddd"),
			new TimeAxisSetting(1800, RrdGraphConstants.HOUR, 12, RrdGraphConstants.DAY, 1, RrdGraphConstants.DAY, 2, 24 * 3600, "dddd"),
			new TimeAxisSetting(3600, RrdGraphConstants.DAY, 1, RrdGraphConstants.WEEK, 1, RrdGraphConstants.WEEK, 1, 7 * 24 * 3600, "'Week 'w"),
			new TimeAxisSetting(3 * 3600, RrdGraphConstants.WEEK, 1, RrdGraphConstants.MONTH, 1, RrdGraphConstants.WEEK, 2, 7 * 24 * 3600, "'Week 'w"),
			new TimeAxisSetting(6 * 3600, RrdGraphConstants.MONTH, 1, RrdGraphConstants.MONTH, 1, RrdGraphConstants.MONTH, 1, 30 * 24 * 3600, "MMM"),
			new TimeAxisSetting(48 * 3600, RrdGraphConstants.MONTH, 1, RrdGraphConstants.MONTH, 3, RrdGraphConstants.MONTH, 3, 30 * 24 * 3600, "MMM"),
			new TimeAxisSetting(10 * 24 * 3600, RrdGraphConstants.YEAR, 1, RrdGraphConstants.YEAR, 1, RrdGraphConstants.YEAR, 1, 365 * 24 * 3600, "yyyy"),
			new TimeAxisSetting(-1, RrdGraphConstants.MONTH, 0, RrdGraphConstants.MONTH, 0, RrdGraphConstants.MONTH, 0, 0, "")
	};

	private TimeAxisSetting tickSetting;
	private RrdGraph rrdGraph;
	private double secPerPix;
	private DateTime calendar;

	public TimeAxis(RrdGraph rrdGraph) {
		this.rrdGraph = rrdGraph;
		if (rrdGraph.im.xsize > 0) {
			this.secPerPix = (rrdGraph.im.end - rrdGraph.im.start) / ((Double)(rrdGraph.im.xsize));
		}
		this.calendar = DateTime.Now;
	}

	internal void draw() {
		chooseTickSettings();
		if (tickSetting == null) {
			return;
		}
		drawMinor();
		drawMajor();
		drawLabels();
	}

	private void drawMinor() {
		if (!rrdGraph.gdef.noMinorGrid) {
			adjustStartingTime(tickSetting.minorUnit, tickSetting.minorUnitCount);
			Color color = rrdGraph.gdef.colors[RrdGraphConstants.COLOR_GRID];
			int y0 = rrdGraph.im.yorigin, y1 = y0 - rrdGraph.im.ysize;
			for (int status = getTimeShift(); status <= 0; status = getTimeShift()) {
				if (status == 0) {
					long time = calendar.GetTimestamp();
					int x = rrdGraph.mapper.xtr(time);
					rrdGraph.worker.drawLine(x, y0 - 1, x, y0 + 1,new Pen(color,RrdGraphConstants.TICK_STROKE));
					rrdGraph.worker.drawLine(x, y0, x, y1, new Pen(color, RrdGraphConstants.GRID_STROKE));
				}
				findNextTime(tickSetting.minorUnit, tickSetting.minorUnitCount);
			}
		}
	}

	private void drawMajor() {
		adjustStartingTime(tickSetting.majorUnit, tickSetting.majorUnitCount);
		Color color = rrdGraph.gdef.colors[RrdGraphConstants.COLOR_MGRID];
		int y0 = rrdGraph.im.yorigin, y1 = y0 - rrdGraph.im.ysize;
		for (int status = getTimeShift(); status <= 0; status = getTimeShift()) {
			if (status == 0) {
				long time = calendar.GetTimestamp();
				int x = rrdGraph.mapper.xtr(time);
				rrdGraph.worker.drawLine(x, y0 - 2, x, y0 + 2, new Pen(color, RrdGraphConstants.TICK_STROKE));
				rrdGraph.worker.drawLine(x, y0, x, y1, new Pen(color, RrdGraphConstants.GRID_STROKE));
			}
			findNextTime(tickSetting.majorUnit, tickSetting.majorUnitCount);
		}
	}

	private void drawLabels() {
		// escape strftime like format string
		String labelFormat = Regex.Replace(tickSetting.format,"([^%]|^)%([^%t])", "$1%t$2");
		Font font = rrdGraph.gdef.smallFont;
		Color color = rrdGraph.gdef.colors[RrdGraphConstants.COLOR_FONT];
		adjustStartingTime(tickSetting.labelUnit, tickSetting.labelUnitCount);
		int y = rrdGraph.im.yorigin + (int) rrdGraph.worker.getFontHeight(font) + 2;
		for (int status = getTimeShift(); status <= 0; status = getTimeShift()) {
			String label = formatLabel(labelFormat, DateTime.Now);
			long time = calendar.GetTimestamp();
			int x1 = rrdGraph.mapper.xtr(time);
			int x2 = rrdGraph.mapper.xtr(time + tickSetting.labelSpan);
			int labelWidth = (int) rrdGraph.worker.getStringWidth(label, font);
			int x = x1 + (x2 - x1 - labelWidth) / 2;
			if (x >= rrdGraph.im.xorigin && x + labelWidth <= rrdGraph.im.xorigin + rrdGraph.im.xsize) {
				rrdGraph.worker.drawString(label, x, y, font,new SolidBrush(color));
			}
			findNextTime(tickSetting.labelUnit, tickSetting.labelUnitCount);
		}
	}

	private static String formatLabel(String format, DateTime date)
	{
		return date.ToString(format);
		//if (format.Contains("%")) {
		//   // strftime like format string
		//   return String.Format(format, date);
		//}
		//else {
		//   // simple date format
		//   return new SimpleDateFormat(format).format(date);
		//}
	}

	private void findNextTime(int timeUnit, int timeUnitCount) {
		switch (timeUnit) {
			case RrdGraphConstants.SECOND:
				calendar = calendar.AddSeconds(timeUnitCount);
				break;
			case RrdGraphConstants.MINUTE:
				calendar = calendar.AddMinutes(timeUnitCount);
				break;
			case RrdGraphConstants.HOUR:
				calendar = calendar.AddHours(timeUnitCount);
				break;
			case RrdGraphConstants.DAY:
				calendar = calendar.AddDays(timeUnitCount);
				break;
			case RrdGraphConstants.WEEK:
				calendar = calendar.AddDays(7 * timeUnitCount);
				break;
			case RrdGraphConstants.MONTH:
				calendar = calendar.AddMonths(timeUnitCount);
				break;
			case RrdGraphConstants.YEAR:
				calendar = calendar.AddYears(timeUnitCount);
				break;
		}
	}

	private int getTimeShift() {
		long time = calendar.GetTimestamp();
		return (time < rrdGraph.im.start) ? -1 : (time > rrdGraph.im.end) ? +1 : 0;
	}

	private void adjustStartingTime(int timeUnit, int timeUnitCount) {
		calendar = Util.GetDateTime(rrdGraph.im.start);
		switch (timeUnit) {
			case RrdGraphConstants.SECOND:
				calendar = calendar.AddSeconds(-(calendar.Second % timeUnitCount));
				break;
			case RrdGraphConstants.MINUTE:
				calendar = calendar.AddSeconds(-calendar.Second);
				calendar = calendar.AddMinutes(-(calendar.Minute % timeUnitCount));
				break;
			case RrdGraphConstants.HOUR:
				calendar = calendar.AddSeconds(-calendar.Second);
				calendar = calendar.AddMinutes(-calendar.Minute);
				calendar = calendar.AddHours(-(calendar.Hour % timeUnitCount));
				break;
			case RrdGraphConstants.DAY:
				calendar = calendar.AddSeconds(-calendar.Second);
				calendar = calendar.AddMinutes(-calendar.Minute);
				calendar = calendar.AddHours(-calendar.Hour);
				break;
			case RrdGraphConstants.WEEK:
				calendar = calendar.AddSeconds(-calendar.Second);
				calendar = calendar.AddMinutes(-calendar.Minute);
				calendar = calendar.AddHours(-calendar.Hour);
				int diffDays = calendar.DayOfWeek - Thread.CurrentThread.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
				if (diffDays < 0) {
					diffDays += 7;
				}
				calendar = calendar.AddDays(-diffDays);
				break;
			case RrdGraphConstants.MONTH:
				calendar = calendar.AddSeconds(-calendar.Second);
				calendar = calendar.AddMinutes(-calendar.Minute);
				calendar = calendar.AddHours(-calendar.Hour);
				calendar = calendar.AddDays(-calendar.Day);
				calendar = calendar.AddMonths(-(calendar.Month % timeUnitCount));
				break;
			case RrdGraphConstants.YEAR:
				calendar = calendar.AddSeconds(-calendar.Second);
				calendar = calendar.AddMinutes(-calendar.Minute);
				calendar = calendar.AddHours(-calendar.Hour);
				calendar = calendar.AddDays(-calendar.Day);
				calendar = calendar.AddMonths(-calendar.Month);
				calendar = calendar.AddYears(-(calendar.Year % timeUnitCount));
				break;
		}
	}


	private void chooseTickSettings() {
		if (rrdGraph.gdef.timeAxisSetting != null) {
			tickSetting = new TimeAxisSetting(rrdGraph.gdef.timeAxisSetting);
		}
		else {
			for (int i = 0; tickSettings[i].secPerPix >= 0 && secPerPix > tickSettings[i].secPerPix; i++) {
				tickSetting = tickSettings[i];
			}
		}
	}

}

}
