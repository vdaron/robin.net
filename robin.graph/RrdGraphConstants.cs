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
using System.Drawing;
using robin.core;
using robin.data;

namespace robin.graph
{
/**
 * Class to represent various constants used for graphing. No methods are specified.
 */
public class RrdGraphConstants {
	/**
	 * Default graph starting time
	 */
	internal const string DEFAULT_START = "end-1d";
	/**
	 * Default graph ending time
	 */
	internal const string DEFAULT_END = "now";

	/**
	 * Constant to represent second
	 */
	internal const int SECOND = 1;// Calendar.SECOND;
	/**
	 * Constant to represent minute
	 */
	internal const int MINUTE = 2;// Calendar.MINUTE;
	/**
	 * Constant to represent hour
	 */
	internal const int HOUR =  3;//Calendar.HOUR_OF_DAY;
	/**
	 * Constant to represent day
	 */
	internal const int DAY =  4;//Calendar.DAY_OF_MONTH;
	/**
	 * Constant to represent week
	 */
	internal const int WEEK =  5;//Calendar.WEEK_OF_YEAR;
	/**
	 * Constant to represent month
	 */
	internal const int MONTH =  6;//Calendar.MONTH;
	/**
	 * Constant to represent year
	 */
	internal const int YEAR =  7;//Calendar.YEAR;

	/**
	 * Constant to represent Monday
	 */
	internal const int MONDAY = (int) DayOfWeek.Monday;
	/**
	 * Constant to represent Tuesday
	 */
	internal const int TUESDAY = (int) DayOfWeek.Tuesday;
	/**
	 * Constant to represent Wednesday
	 */
	internal const int WEDNESDAY = (int) DayOfWeek.Wednesday;
	/**
	 * Constant to represent Thursday
	 */
	internal const int THURSDAY = (int) DayOfWeek.Thursday;
	/**
	 * Constant to represent Friday
	 */
	internal const int FRIDAY = (int) DayOfWeek.Friday;
	/**
	 * Constant to represent Saturday
	 */
	internal const int SATURDAY = (int) DayOfWeek.Saturday;
	/**
	 * Constant to represent Sunday
	 */
	internal const int SUNDAY = (int) DayOfWeek.Sunday;

	/**
	 * Index of the canvas color. Used in {@link RrdGraphDef#setColor(internal const int, java.awt.Painternal const int)}
	 */
	internal const int COLOR_CANVAS = 0;
	/**
	 * Index of the background color. Used in {@link RrdGraphDef#setColor(internal const int, java.awt.Painternal const int)}
	 */
	internal const int COLOR_BACK = 1;
	/**
	 * Index of the top-left graph shade color. Used in {@link RrdGraphDef#setColor(internal const int, java.awt.Painternal const int)}
	 */
	internal const int COLOR_SHADEA = 2;
	/**
	 * Index of the bottom-right graph shade color. Used in {@link RrdGraphDef#setColor(internal const int, java.awt.Painternal const int)}
	 */
	internal const int COLOR_SHADEB = 3;
	/**
	 * Index of the minor grid color. Used in {@link RrdGraphDef#setColor(internal const int, java.awt.Painternal const int)}
	 */
	internal const int COLOR_GRID = 4;
	/**
	 * Index of the major grid color. Used in {@link RrdGraphDef#setColor(internal const int, java.awt.Painternal const int)}
	 */
	internal const int COLOR_MGRID = 5;
	/**
	 * Index of the font color. Used in {@link RrdGraphDef#setColor(internal const int, java.awt.Painternal const int)}
	 */
	internal const int COLOR_FONT = 6;
	/**
	 * Index of the frame color. Used in {@link RrdGraphDef#setColor(internal const int, java.awt.Painternal const int)}
	 */
	internal const int COLOR_FRAME = 7;
	/**
	 * Index of the arrow color. Used in {@link RrdGraphDef#setColor(internal const int, java.awt.Painternal const int)}
	 */
	internal const int COLOR_ARROW = 8;

	/**
	 * Allowed color names which can be used in {@link RrdGraphDef#setColor(String, java.awt.Painternal const int)} method
	 */

	public static String[] COLOR_NAMES = {
			"canvas", "back", "shadea", "shadeb", "grid", "mgrid", "font", "frame", "arrow"
	};

	/**
	 * Default first day of the week (obtained from the default locale)
	 */
	internal static DayOfWeek FIRST_DAY_OF_WEEK = System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat.FirstDayOfWeek;

	/**
	 * Default graph canvas color
	 */
	public static Color DEFAULT_CANVAS_COLOR = Color.White;
	/**
	 * Default graph background color
	 */
	public static Color DEFAULT_BACK_COLOR = Color.FromArgb(245, 245, 245);
	/**
	 * Default top-left graph shade color
	 */
	public static Color DEFAULT_SHADEA_COLOR = Color.FromArgb(200, 200, 200);
	/**
	 * Default bottom-right graph shade color
	 */
	public static Color DEFAULT_SHADEB_COLOR = Color.FromArgb(150, 150, 150);
	/**
	 * Default minor grid color
	 */
	public static Color DEFAULT_GRID_COLOR = Color.FromArgb(171, 171, 171, 95);
	// Color DEFAULT_GRID_COLOR = new Color(140, 140, 140);
	/**
	 * Default major grid color
	 */
	public static Color DEFAULT_MGRID_COLOR = Color.FromArgb(255, 91, 91, 95);
	// Color DEFAULT_MGRID_COLOR = new Color(130, 30, 30);
	/**
	 * Default font color
	 */
	public static Color DEFAULT_FONT_COLOR = Color.Black;
	/**
	 * Default frame color
	 */
	public static Color DEFAULT_FRAME_COLOR = Color.Black;
	/**
	 * Default arrow color
	 */
	public static Color DEFAULT_ARROW_COLOR = Color.Red;

	/**
	 * Constant to represent left alignment marker
	 */
	internal const string ALIGN_LEFT_MARKER = "\\l";
	/**
	 * Constant to represent centered alignment marker
	 */
	internal const string ALIGN_CENTER_MARKER = "\\c";
	/**
	 * Constant to represent right alignment marker
	 */
	internal const string ALIGN_RIGHT_MARKER = "\\r";
	/**
	 * Constant to represent justified alignment marker
	 */
	internal const string ALIGN_JUSTIFIED_MARKER = "\\j";
	/**
	 * Constant to represent "glue" marker
	 */
	internal const string GLUE_MARKER = "\\g";
	/**
	 * Constant to represent vertical spacing marker
	 */
	internal const string VERTICAL_SPACING_MARKER = "\\s";
	/**
	 * Constant to represent no justification markers
	 */
	internal const string NO_JUSTIFICATION_MARKER = "\\J";
	/**
	 * Used internal const internally
	 */

	internal static String[] MARKERS = {
			ALIGN_LEFT_MARKER, ALIGN_CENTER_MARKER, ALIGN_RIGHT_MARKER,
			ALIGN_JUSTIFIED_MARKER, GLUE_MARKER, VERTICAL_SPACING_MARKER, NO_JUSTIFICATION_MARKER
	};

	/**
	 * Constant to represent in-memory image name
	 */
	internal const string IN_MEMORY_IMAGE = "-";

	/**
	 * Default units length
	 */
	internal const int DEFAULT_UNITS_LENGTH = 9;
	/**
	 * Default graph width
	 */
	internal const int DEFAULT_WIDTH = 400;
	/**
	 * Default graph height
	 */
	internal const int DEFAULT_HEIGHT = 100;
	/**
	 * Default image format
	 */
	internal const string DEFAULT_IMAGE_FORMAT = "gif";
	/**
	 * Default image quality, used only for jpeg graphs
	 */
	public static float DEFAULT_IMAGE_QUALITY = 0.8F; // only for jpegs, not used for png/gif
	/**
	 * Default value base
	 */
	public static double DEFAULT_BASE = 1000;

	/**
	 * Default font name, determined based on the current operating system
	 */
	internal static string DEFAULT_FONT_NAME = Environment.OSVersion.Platform == PlatformID.Unix ?
			"Monospaced" : "Lucida Sans Typewriter";
	
	/**
	 * Default graph small font
	 */
	internal const string DEFAULT_MONOSPACE_FONT_FILE = "DejaVuSansMono.ttf";

	/**
	 * Default graph large font
	 */
	internal const string DEFAULT_PROPORTIONAL_FONT_FILE = "DejaVuSans-Bold.ttf";

	/**
	 * Used internal const internally
	 */
	internal static double LEGEND_LEADING = 1.2; // chars
	/**
	 * Used internal const internally
	 */
	internal static double LEGEND_LEADING_SMALL = 0.7; // chars
	/**
	 * Used internal const internally
	 */
	internal static double LEGEND_BOX_SPACE = 1.2; // chars
	/**
	 * Used internal const internally
	 */
	public static double LEGEND_BOX = 0.9; // chars
	/**
	 * Used internal const internally
	 */
	public static int LEGEND_INTERSPACING = 2; // chars
	/**
	 * Used internal const internally
	 */
	internal const int PADDING_LEFT = 10; // pix
	/**
	 * Used internal const internally
	 */
	internal const int PADDING_TOP = 12; // pix
	/**
	 * Used internal const internally
	 */
	internal const int PADDING_TITLE = 6; // pix
	/**
	 * Used internal const internally
	 */
	internal const int PADDING_RIGHT = 16; // pix
	/**
	 * Used internal const internally
	 */
	internal const int PADDING_PLOT = 2; //chars
	/**
	 * Used internal const internally
	 */
	internal const int PADDING_LEGEND = 2; // chars
	/**
	 * Used internal const internally
	 */
	internal const int PADDING_BOTTOM = 15; // pix
	/**
	 * Used internal const internally
	 */
	internal const int PADDING_VLABEL = 7; // pix

	/**
	 * Stroke used to draw grid
	 */
	// solid line
	public static float GRID_STROKE = 1F;
	
	// dotted line
	// Stroke GRID_STROKE = new BasicStroke(1, BasicStroke.CAP_BUTT, BasicStroke.JOIN_MITER, 1, new float[] {1, 1}, 0);
	/**
	 * Stroke used to draw ticks
	 */
	public static float TICK_STROKE = 1F;
}

}