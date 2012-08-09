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
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using robin.core;
using robin.data;

namespace robin.graph
{
/**
 * Class which actually creates JRobin graphs (does the hard work).
 */
public class RrdGraph {
	internal RrdGraphDef gdef;
	internal ImageParameters im = new ImageParameters();
	DataProcessor dproc;
	internal ImageWorker worker;
	internal Mapper mapper;
	RrdGraphInfo info = new RrdGraphInfo();
	private String signature;

	/**
	 * Creates graph from the corresponding {@link RrdGraphDef} object.
	 *
	 * @param gdef Graph definition
	 * @throws IOException  Thrown in case of I/O error
	 * @throws RrdException Thrown in case of JRobin related error
	 */
	public RrdGraph(RrdGraphDef gdef) {
		this.gdef = gdef;
		signature = gdef.getSignature();
		worker = new ImageWorker(100, 100); // Dummy worker, just to start with something
		try {
			createGraph();
		}
		finally {
			worker.dispose();
			worker = null;
			dproc = null;
		}
	}

	/**
	 * Returns complete graph information in a single object.
	 *
	 * @return Graph information (width, height, filename, image bytes, etc...)
	 */
	public RrdGraphInfo getRrdGraphInfo() {
		return info;
	}

	private void createGraph() {
		bool lazy = lazyCheck();
		if (!lazy || gdef.printStatementCount() != 0) {
			fetchData();
			resolveTextElements();
			if (gdef.shouldPlot() && !lazy) {
				calculatePlotValues();
				findMinMaxValues();
				identifySiUnit();
				expandValueRange();
				removeOutOfRangeRules();
				initializeLimits();
				placeLegends();
				createImageWorker();
				drawBackground();
				drawData();
				drawGrid();
				drawAxis();
				drawText();
				drawLegend();
				drawRules();
				gator();
				drawOverlay();
				saveImage();
			}
		}
		collectInfo();
	}

	private void collectInfo() {
		info.filename = gdef.filename;
		info.width = im.xgif;
		info.height = im.ygif;
		foreach (CommentText comment in gdef.comments) {
			if (comment is PrintText) {
				PrintText pt = (PrintText) comment;
				if (pt.isPrint()) {
					info.addPrintLine(pt.resolvedText);
				}
			}
		}
		if (gdef.imageInfo != null) {
			info.imgInfo = String.Format(gdef.imageInfo, gdef.filename, im.xgif, im.ygif);
		}
	}

	private void saveImage() {
		if (!gdef.filename.Equals("-")) {
			info.bytes = worker.saveImage(gdef.filename, gdef.imageFormat, gdef.imageQuality);
		}
		else {
			info.bytes = worker.getImageBytes(gdef.imageFormat, gdef.imageQuality);
		}
	}

	private void drawOverlay() {
		if (gdef.overlayImage != null) {
			worker.loadImage(gdef.overlayImage);
		}
	}

	private void gator() {
		if (!gdef.onlyGraph && gdef.showSignature) {
			using(Font font = new Font(gdef.getSmallFont().FontFamily,9,FontStyle.Regular))
			using(Brush brush = new SolidBrush(Color.LightGray))
			{
				int x = (int) (im.xgif - 2 - worker.getFontAscent(font));
				int y = 4;
				worker.transform(x, y, (float) (Math.PI/2));
				worker.drawString(signature, 0, 0, font, brush);
				worker.reset();
			}
		}
	}

	private void drawRules() {
		worker.clip(im.xorigin + 1, im.yorigin - gdef.height - 1, gdef.width - 1, gdef.height + 2);
		foreach (PlotElement pe in gdef.plotElements) {
			if (pe is HRule) {
				HRule hr = (HRule) pe;
				if (hr.value >= im.minval && hr.value <= im.maxval) {
					int y = mapper.ytr(hr.value);
					worker.drawLine(im.xorigin, y, im.xorigin + im.xsize, y,new Pen(hr.color,hr.width));
				}
			}
			else if (pe is VRule) {
				VRule vr = (VRule) pe;
				if (vr.timestamp >= im.start && vr.timestamp <= im.end) {
					int x = mapper.xtr(vr.timestamp);
					worker.drawLine(x, im.yorigin, x, im.yorigin - im.ysize,new Pen(vr.color,vr.width));
				}
			}
		}
		worker.reset();
	}

	private void drawText() {
		if (!gdef.onlyGraph) {
			if (gdef.title != null) {
				int x = im.xgif / 2 - (int) (worker.getStringWidth(gdef.title, gdef.largeFont) / 2);
				int y = RrdGraphConstants.PADDING_TOP + (int) worker.getFontAscent(gdef.largeFont);
				worker.drawString(gdef.title, x, y, gdef.largeFont,new SolidBrush(gdef.colors[RrdGraphConstants.COLOR_FONT]));
			}
			if (gdef.verticalLabel != null) {
				int x = RrdGraphConstants.PADDING_LEFT;
				int y = im.yorigin - im.ysize / 2 + (int) worker.getStringWidth(gdef.verticalLabel, gdef.getSmallFont()) / 2;
				int ascent = (int) worker.getFontAscent(gdef.smallFont);
				worker.transform(x, y, (float) (-Math.PI / 2));
				worker.drawString(gdef.verticalLabel, 0, ascent, gdef.smallFont,new SolidBrush(gdef.colors[RrdGraphConstants.COLOR_FONT]));
				worker.reset();
			}
		}
	}

	private void drawGrid() {
		if (!gdef.onlyGraph)
		{
			Pen shade1 = new Pen(gdef.colors[RrdGraphConstants.COLOR_SHADEA], 1);
			Pen shade2 = new Pen(gdef.colors[RrdGraphConstants.COLOR_SHADEB],1);
			worker.drawLine(0, 0, im.xgif - 1, 0, shade1);
			worker.drawLine(1, 1, im.xgif - 2, 1, shade1);
			worker.drawLine(0, 0, 0, im.ygif - 1, shade1);
			worker.drawLine(1, 1, 1, im.ygif - 2, shade1);
			worker.drawLine(im.xgif - 1, 0, im.xgif - 1, im.ygif - 1, shade2);
			worker.drawLine(0, im.ygif - 1, im.xgif - 1, im.ygif - 1, shade2);
			worker.drawLine(im.xgif - 2, 1, im.xgif - 2, im.ygif - 2, shade2);
			worker.drawLine(1, im.ygif - 2, im.xgif - 2, im.ygif - 2, shade2);
			if (gdef.drawXGrid) {
				new TimeAxis(this).draw();
			}
			if (gdef.drawYGrid) {
				bool ok;
				if (gdef.altYMrtg) {
					ok = new ValueAxisMrtg(this).draw();
				}
				else if (gdef.logarithmic) {
					ok = new ValueAxisLogarithmic(this).draw();
				}
				else {
					ok = new ValueAxis(this).draw();
				}
				if (!ok) {
					String msg = "No Data Found";
					worker.drawString(msg,
							im.xgif / 2 - (int) worker.getStringWidth(msg, gdef.largeFont) / 2,
							(2 * im.yorigin - im.ysize) / 2,
							gdef.largeFont,new SolidBrush(gdef.colors[RrdGraphConstants.COLOR_FONT]));
				}
			}
		}
	}

	private void drawData() {
		worker.setAntiAliasing(gdef.antiAliasing);
		worker.clip(im.xorigin + 1, im.yorigin - gdef.height - 1, gdef.width - 1, gdef.height + 2);
		double areazero = mapper.ytr((im.minval > 0.0) ? im.minval : (im.maxval < 0.0) ? im.maxval : 0.0);
		double[] x = xtr(dproc.GetTimestamps()), lastY = null;
		// draw line, area and stack
		foreach (PlotElement plotElement in gdef.plotElements) {
			if (plotElement is SourcedPlotElement) {
				SourcedPlotElement source = (SourcedPlotElement) plotElement;
				double[] y = ytr(source.getValues());
				if (source is Line) {
					worker.drawPolyline(x, y, new Pen(source.color,((Line)source).width));
				}
				else if (source is Area) {
					worker.fillPolygon(x, areazero, y, new SolidBrush(source.color));
				}
				else if (source is Stack) {
					Stack stack = (Stack) source;
					float width = stack.getParentLineWidth();
					if (width >= 0F) {
						// line
						worker.drawPolyline(x, y, new Pen(stack.color, width));
					}
					else {
						// area
						worker.fillPolygon(x, lastY, y,new SolidBrush(stack.color));
						worker.drawPolyline(x, lastY, new Pen(stack.getParentColor(), 0));//TODO : vdaron - need tests
					}
				}
				else {
					// should not be here
					throw new RrdException("Unknown plot source: " + source.GetType().Name);
				}
				lastY = y;
			}
		}
		worker.reset();
		worker.setAntiAliasing(false);
	}

	private void drawAxis() {
		if (!gdef.onlyGraph) {
			Pen gridColor = new Pen(gdef.colors[RrdGraphConstants.COLOR_GRID],1);
			Pen fontColor = new Pen(gdef.colors[RrdGraphConstants.COLOR_FONT],1);
			Pen arrowColor = new Pen(gdef.colors[RrdGraphConstants.COLOR_ARROW],1);
			worker.drawLine(im.xorigin + im.xsize, im.yorigin, im.xorigin + im.xsize, im.yorigin - im.ysize,
					gridColor);
			worker.drawLine(im.xorigin, im.yorigin - im.ysize, im.xorigin + im.xsize, im.yorigin - im.ysize,
					gridColor);
			worker.drawLine(im.xorigin - 4, im.yorigin, im.xorigin + im.xsize + 4, im.yorigin,
					fontColor);
			worker.drawLine(im.xorigin, im.yorigin, im.xorigin, im.yorigin - im.ysize,
					gridColor);
			worker.drawLine(im.xorigin + im.xsize + 4, im.yorigin - 3, im.xorigin + im.xsize + 4, im.yorigin + 3,
					arrowColor);
			worker.drawLine(im.xorigin + im.xsize + 4, im.yorigin - 3, im.xorigin + im.xsize + 9, im.yorigin,
					arrowColor);
			worker.drawLine(im.xorigin + im.xsize + 4, im.yorigin + 3, im.xorigin + im.xsize + 9, im.yorigin,
					arrowColor);
		}
	}

	private void drawBackground(){
		worker.fillRect(0, 0, im.xgif, im.ygif,new SolidBrush(gdef.colors[RrdGraphConstants.COLOR_BACK]));
		if (gdef.backgroundImage != null) {
			worker.loadImage(gdef.backgroundImage);
		}
		worker.fillRect(im.xorigin, im.yorigin - im.ysize, im.xsize, im.ysize,new SolidBrush(gdef.colors[RrdGraphConstants.COLOR_CANVAS]));
	}

	private void createImageWorker() {
		worker.resize(im.xgif, im.ygif);
	}

	private void placeLegends() {
		if (!gdef.noLegend && !gdef.onlyGraph) {
			int border = (int) (getSmallFontCharWidth() * RrdGraphConstants.PADDING_LEGEND);
			LegendComposer lc = new LegendComposer(this, border, im.ygif, im.xgif - 2 * border);
			im.ygif = lc.placeComments() + RrdGraphConstants.PADDING_BOTTOM;
		}
	}

	private void initializeLimits(){
		im.xsize = gdef.width;
		im.ysize = gdef.height;
		im.unitslength = gdef.unitsLength;
		if (gdef.onlyGraph) {
			if (im.ysize > 64) {
				throw new RrdException("Cannot create graph only, height too big");
			}
			im.xorigin = 0;
		}
		else {
			im.xorigin = (int) (RrdGraphConstants.PADDING_LEFT + im.unitslength * getSmallFontCharWidth());
		}
		if (gdef.verticalLabel != null) {
			im.xorigin += (int) getSmallFontHeight();
		}
		if (gdef.onlyGraph) {
			im.yorigin = im.ysize;
		}
		else {
			im.yorigin = RrdGraphConstants.PADDING_TOP + im.ysize;
		}
		mapper = new Mapper(this);
		if (gdef.title != null) {
			im.yorigin += (int) getLargeFontHeight() + RrdGraphConstants.PADDING_TITLE;//TODO: vdaron : check this convertion
		}
		if (gdef.onlyGraph) {
			im.xgif = im.xsize;
			im.ygif = im.yorigin;
		}
		else {
			im.xgif = RrdGraphConstants.PADDING_RIGHT + im.xsize + im.xorigin;
			im.ygif = im.yorigin + (int) (RrdGraphConstants.PADDING_PLOT * getSmallFontHeight());
		}
	}

	private void removeOutOfRangeRules() {
		foreach (PlotElement plotElement in gdef.plotElements) {
			if (plotElement is HRule) {
				((HRule) plotElement).setLegendVisibility(im.minval, im.maxval, gdef.forceRulesLegend);
			}
			else if (plotElement is VRule) {
				((VRule) plotElement).setLegendVisibility(im.start, im.end, gdef.forceRulesLegend);
			}
		}
	}

	private void expandValueRange() {
		im.ygridstep = (gdef.valueAxisSetting != null) ? gdef.valueAxisSetting.gridStep : Double.NaN;
		im.ylabfact = (gdef.valueAxisSetting != null) ? gdef.valueAxisSetting.labelFactor : 0;
		if (!gdef.rigid && !gdef.logarithmic) {
			double[] sensiblevalues = {
					1000.0, 900.0, 800.0, 750.0, 700.0, 600.0, 500.0, 400.0, 300.0, 250.0, 200.0, 125.0, 100.0,
					90.0, 80.0, 75.0, 70.0, 60.0, 50.0, 40.0, 30.0, 25.0, 20.0, 10.0,
					9.0, 8.0, 7.0, 6.0, 5.0, 4.0, 3.5, 3.0, 2.5, 2.0, 1.8, 1.5, 1.2, 1.0,
					0.8, 0.7, 0.6, 0.5, 0.4, 0.3, 0.2, 0.1, 0.0, -1
			};
			double scaled_min, scaled_max, adj;
			if (Double.IsNaN(im.ygridstep)) {
				if (gdef.altYMrtg) { /* mrtg */
					im.decimals = Math.Ceiling(Math.Log10(Math.Max(Math.Abs(im.maxval), Math.Abs(im.minval))));
					im.quadrant = 0;
					if (im.minval < 0) {
						im.quadrant = 2;
						if (im.maxval <= 0) {
							im.quadrant = 4;
						}
					}
					switch (im.quadrant) {
						case 2:
							im.scaledstep = Math.Ceiling(50 * Math.Pow(10, -(im.decimals)) * Math.Max(Math.Abs(im.maxval),
									Math.Abs(im.minval))) * Math.Pow(10, im.decimals - 2);
							scaled_min = -2 * im.scaledstep;
							scaled_max = 2 * im.scaledstep;
							break;
						case 4:
							im.scaledstep = Math.Ceiling(25 * Math.Pow(10,
									-(im.decimals)) * Math.Abs(im.minval)) * Math.Pow(10, im.decimals - 2);
							scaled_min = -4 * im.scaledstep;
							scaled_max = 0;
							break;
						default: /* quadrant 0 */
							im.scaledstep = Math.Ceiling(25 * Math.Pow(10, -(im.decimals)) * im.maxval) *
									Math.Pow(10, im.decimals - 2);
							scaled_min = 0;
							scaled_max = 4 * im.scaledstep;
							break;
					}
					im.minval = scaled_min;
					im.maxval = scaled_max;
				}
				else if (gdef.altAutoscale) {
					/* measure the amplitude of the function. Make sure that
					   graph boundaries are slightly higher then max/min vals
					   so we can see amplitude on the graph */
					double delt, fact;

					delt = im.maxval - im.minval;
					adj = delt * 0.1;
					fact = 2.0 * Math.Pow(10.0,
							Math.Floor(Math.Log10(Math.Max(Math.Abs(im.minval), Math.Abs(im.maxval)))) - 2);
					if (delt < fact) {
						adj = (fact - delt) * 0.55;
					}
					im.minval -= adj;
					im.maxval += adj;
				}
				else if (gdef.altAutoscaleMax) {
					/* measure the amplitude of the function. Make sure that
					   graph boundaries are slightly higher than max vals
					   so we can see amplitude on the graph */
					adj = (im.maxval - im.minval) * 0.1;
					im.maxval += adj;
				}
				else {
					scaled_min = im.minval / im.magfact;
					scaled_max = im.maxval / im.magfact;
					for (int i = 1; sensiblevalues[i] > 0; i++) {
						if (sensiblevalues[i - 1] >= scaled_min && sensiblevalues[i] <= scaled_min) {
							im.minval = sensiblevalues[i] * im.magfact;
						}
						if (-sensiblevalues[i - 1] <= scaled_min && -sensiblevalues[i] >= scaled_min) {
							im.minval = -sensiblevalues[i - 1] * im.magfact;
						}
						if (sensiblevalues[i - 1] >= scaled_max && sensiblevalues[i] <= scaled_max) {
							im.maxval = sensiblevalues[i - 1] * im.magfact;
						}
						if (-sensiblevalues[i - 1] <= scaled_max && -sensiblevalues[i] >= scaled_max) {
							im.maxval = -sensiblevalues[i] * im.magfact;
						}
					}
				}
			}
			else {
				im.minval = (double) im.ylabfact * im.ygridstep *
						Math.Floor(im.minval / ((double) im.ylabfact * im.ygridstep));
				im.maxval = (double) im.ylabfact * im.ygridstep *
						Math.Ceiling(im.maxval / ((double) im.ylabfact * im.ygridstep));
			}

		}
	}

	private void identifySiUnit() {
		im.unitsexponent = gdef.unitsExponent;
		im.base_renamed = gdef.base_renamed;
		if (!gdef.logarithmic) {
			char[] symbol = {'a', 'f', 'p', 'n', 'u', 'm', ' ', 'k', 'M', 'G', 'T', 'P', 'E'};
			int symbcenter = 6;
			double digits;
			if (im.unitsexponent != int.MaxValue) {
				digits = Math.Floor(im.unitsexponent / 3.0);
			}
			else {
				digits = Math.Floor(Math.Log(Math.Max(Math.Abs(im.minval), Math.Abs(im.maxval))) / Math.Log(im.base_renamed));
			}
			im.magfact = Math.Pow(im.base_renamed, digits);
			if (((digits + symbcenter) < symbol.Length) && ((digits + symbcenter) >= 0)) {
				im.symbol = symbol[(int) digits + symbcenter];
			}
			else {
				im.symbol = '?';
			}
		}
	}

	private void findMinMaxValues() {
		double minval = Double.NaN, maxval = Double.NaN;
		foreach (PlotElement pe in gdef.plotElements) {
			if (pe is SourcedPlotElement) {
				minval = Util.Min(((SourcedPlotElement) pe).getMinValue(), minval);
				maxval = Util.Max(((SourcedPlotElement) pe).getMaxValue(), maxval);
			}
		}
		if (Double.IsNaN(minval)) {
			minval = 0D;
		}
		if (Double.IsNaN(maxval)) {
			maxval = 1D;
		}
		im.minval = gdef.minValue;
		im.maxval = gdef.maxValue;
		/* adjust min and max values */
		if (Double.IsNaN(im.minval) || ((!gdef.logarithmic && !gdef.rigid) && im.minval > minval)) {
			im.minval = minval;
		}
		if (Double.IsNaN(im.maxval) || (!gdef.rigid && im.maxval < maxval)) {
			if (gdef.logarithmic) {
				im.maxval = maxval * 1.1;
			}
			else {
				im.maxval = maxval;
			}
		}
		/* make sure min is smaller than max */
		if (im.minval > im.maxval) {
			im.minval = 0.99 * im.maxval;
		}
		/* make sure min and max are not equal */
		if (Math.Abs(im.minval - im.maxval) < .0000001) {
			im.maxval *= 1.01;
			if (!gdef.logarithmic) {
				im.minval *= 0.99;
			}
			/* make sure min and max are not both zero */
			if (im.maxval == 0.0) {
				im.maxval = 1.0;
			}
		}
	}

	private void calculatePlotValues(){
		foreach (PlotElement pe in gdef.plotElements) {
			if (pe is SourcedPlotElement) {
				((SourcedPlotElement) pe).assignValues(dproc);
			}
		}
	}

	private void resolveTextElements() {
		ValueScaler valueScaler = new ValueScaler(gdef.base_renamed);
		foreach (CommentText comment in gdef.comments) {
			comment.resolveText(dproc, valueScaler);
		}
	}

	private void fetchData() {
		dproc = new DataProcessor(gdef.startTime, gdef.endTime);
		dproc.IsPoolUsed = gdef.poolUsed;
		if (gdef.step > 0) {
			dproc.Step = gdef.step;
		}
		foreach (Source src in gdef.sources) {
			src.requestData(dproc);
		}
		dproc.ProcessData();
		//long[] t = dproc.getTimestamps();
		//im.start = t[0];
		//im.end = t[t.length - 1];
		im.start = gdef.startTime;
		im.end = gdef.endTime;
	}

	private bool lazyCheck() {
		// redraw if lazy option is not set or file does not exist
		if (!gdef.lazy || !Util.FileExists(gdef.filename)) {
			return false; // 'false' means 'redraw'
		}
		// redraw if not enough time has passed
		long secPerPixel = (gdef.endTime - gdef.startTime) / gdef.width;
		long elapsed =(long) (DateTime.Now - File.GetLastWriteTime(gdef.filename)).TotalSeconds;
		return elapsed <= secPerPixel;
	}

	private void drawLegend() {
		if (!gdef.onlyGraph && !gdef.noLegend) {
			int ascent = (int) worker.getFontAscent(gdef.smallFont);
			int box = (int) getBox(), boxSpace = (int) (getBoxSpace());
			foreach (CommentText c in gdef.comments) {
				if (c.isValidGraphElement()) {
					int x = c.x, y = c.y + ascent;
					if (c is LegendText) {
						// draw with BOX
						worker.fillRect(x, y - box, box, box, new SolidBrush(gdef.colors[RrdGraphConstants.COLOR_FRAME]));
						worker.fillRect(x + 1, y - box + 1, box - 2, box - 2, new SolidBrush(((LegendText) c).legendColor));
						worker.drawString(c.resolvedText, x + boxSpace, y, gdef.smallFont,new SolidBrush(gdef.colors[RrdGraphConstants.COLOR_FONT]));
					}
					else {
						worker.drawString(c.resolvedText, x, y, gdef.smallFont,new SolidBrush(gdef.colors[RrdGraphConstants.COLOR_FONT]));
					}
				}
			}
		}
	}

	// helper methods

	internal double getSmallFontHeight() {
		return worker.getFontHeight(gdef.smallFont);
	}

	private double getLargeFontHeight() {
		return worker.getFontHeight(gdef.largeFont);
	}

	private double getSmallFontCharWidth() {
		return worker.getStringWidth("a", gdef.smallFont);
	}

	internal double getInterlegendSpace() {
		return getSmallFontCharWidth() * RrdGraphConstants.LEGEND_INTERSPACING;
	}

	internal double getLeading() {
		return getSmallFontHeight() * RrdGraphConstants.LEGEND_LEADING;
	}

	internal double getSmallLeading() {
		return getSmallFontHeight() * RrdGraphConstants.LEGEND_LEADING_SMALL;
	}

	internal double getBoxSpace() {
		return Math.Ceiling(getSmallFontHeight() * RrdGraphConstants.LEGEND_BOX_SPACE);
	}

	private double getBox() {
		return getSmallFontHeight() * RrdGraphConstants.LEGEND_BOX;
	}

	double[] xtr(long[] timestamps) {
		/*
		double[] timestampsDev = new double[timestamps.length];
		for (int i = 0; i < timestamps.length; i++) {
			timestampsDev[i] = mapper.xtr(timestamps[i]);
		}
		return timestampsDev;
		*/
		double[] timestampsDev = new double[2 * timestamps.Length - 1];
		for (int i = 0, j = 0; i < timestamps.Length; i += 1, j += 2) {
			timestampsDev[j] = mapper.xtr(timestamps[i]);
			if (i < timestamps.Length - 1) {
				timestampsDev[j + 1] = timestampsDev[j];
			}
		}
		return timestampsDev;
	}

	double[] ytr(double[] values) {
		/*
		double[] valuesDev = new double[values.length];
		for (int i = 0; i < values.length; i++) {
			if (Double.isNaN(values[i])) {
				valuesDev[i] = Double.NaN;
			}
			else {
				valuesDev[i] = mapper.ytr(values[i]);
			}
		}
		return valuesDev;
		*/
		double[] valuesDev = new double[2 * values.Length - 1];
		for (int i = 0, j = 0; i < values.Length; i += 1, j += 2) {
			if (Double.IsNaN(values[i])) {
				valuesDev[j] = Double.NaN;
			}
			else {
				valuesDev[j] = mapper.ytr(values[i]);
			}
			if (j > 0) {
				valuesDev[j - 1] = valuesDev[j];
			}
		}
		return valuesDev;
	}

	/**
	 * Renders this graph onto graphing device
	 *
	 * @param g Graphics handle
	 */
	//public void render(Graphics g) {
	//   byte[] imageData = getRrdGraphInfo().getBytes();
	//   ImageIcon image = new ImageIcon(imageData);
	//   image.paintIcon(null, g, 0, 0);
	//}
}

}