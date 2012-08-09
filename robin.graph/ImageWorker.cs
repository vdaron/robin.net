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
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using robin.core;
using robin.data;

namespace robin.graph
{
class ImageWorker {
	private const String DUMMY_TEXT = "Dummy";

	private Image img;
	private Graphics gd;
	private int imgWidth, imgHeight;
	private Matrix aftInitial;

	internal ImageWorker(int width, int height) {
		resize(width, height);
	}

	internal void resize(int width, int height)
	{
		if (gd != null) {
			gd.Dispose();
		}
		this.imgWidth = width;
		this.imgHeight = height;
		this.img = new Bitmap(width,height);
		this.gd = Graphics.FromImage(img);
		this.aftInitial = gd.Transform;
		this.setAntiAliasing(false);
	}

	internal void clip(int x, int y, int width, int height)
	{
		gd.SetClip(new Rectangle(x, y, width, height));
	}

	internal void transform(int x, int y, float angle)
	{
		gd.TranslateTransform(x,y);
		gd.RotateTransform(angle);
	}

	internal void reset()
	{
		gd.Transform = aftInitial;
		gd.SetClip(new Rectangle(0, 0, imgWidth, imgHeight));
	}

	internal void fillRect(int x, int y, int width, int height, Brush paint)
	{
		gd.FillRectangle(paint, x, y, width, height);
	}

	internal void fillPolygon(int[] x, int[] y, Brush paint)
	{
		Point[] points = GetPoints(x, y);
		gd.FillPolygon(paint, points);
	}
	internal void drawPolygon(int[] x, int[] y, Pen paint)
	{
		Point[] points = GetPoints(x, y);
		gd.DrawPolygon(paint, points);
	}

	private Point[] GetPoints(int[] x, int[] y)
	{
		Point[] points = new Point[x.Length];
		for (int i = 0; i < x.Length; i++)
		{
			points[i] = new Point(x[i],y[i]);
		}
		return points;
	}

	internal void fillPolygon(double[] x, double yBottom, double[] yTop, Brush paint)
	{
		PathIterator path = new PathIterator(yTop);
		for (int[] pos = path.getNextPath(); pos != null; pos = path.getNextPath()) {
			int start = pos[0], end = pos[1], n = end - start;
			int[] xDev = new int[n + 2], yDev = new int[n + 2];
			for (int i = start; i < end; i++) {
				xDev[i - start] = (int) x[i];
				yDev[i - start] = (int) yTop[i];
			}
			xDev[n] = xDev[n - 1];
			xDev[n + 1] = xDev[0];
			yDev[n] = yDev[n + 1] = (int) yBottom;
			fillPolygon(xDev, yDev, paint);
			drawPolygon(xDev, yDev, new Pen(paint));
		}
	}

	internal void fillPolygon(double[] x, double[] yBottom, double[] yTop, Brush paint)
	{
		PathIterator path = new PathIterator(yTop);
		for (int[] pos = path.getNextPath(); pos != null; pos = path.getNextPath()) {
			int start = pos[0], end = pos[1], n = end - start;
			int[] xDev = new int[n * 2], yDev = new int[n * 2];
			for (int i = start; i < end; i++) {
				int ix1 = i - start, ix2 = n * 2 - 1 - i + start;
				xDev[ix1] = xDev[ix2] = (int) x[i];
				yDev[ix1] = (int) yTop[i];
				yDev[ix2] = (int) yBottom[i];
			}
			fillPolygon(xDev, yDev, paint);
			drawPolygon(xDev, yDev, new Pen(paint));
		}
	}


	internal void drawLine(int x1, int y1, int x2, int y2, Pen paint)
	{
		gd.DrawLine(paint, x1, y1, x2, y2);
	}

	internal void drawPolyline(int[] x, int[] y, Pen paint)
	{
		gd.DrawLines(paint,GetPoints(x,y));
	}

	internal void drawPolyline(double[] x, double[] y, Pen paint)
	{
		PathIterator path = new PathIterator(y);
		for (int[] pos = path.getNextPath(); pos != null; pos = path.getNextPath()) {
			int start = pos[0], end = pos[1];
			int[] xDev = new int[end - start], yDev = new int[end - start];
			for (int i = start; i < end; i++) {
				xDev[i - start] = (int) x[i];
				yDev[i - start] = (int) y[i];
			}
			drawPolyline(xDev, yDev,paint);
		}
	}

	internal void drawString(String text, int x, int y, Font font, Brush paint)
	{
		gd.DrawString(text,font,paint, x, y);
	}

	internal double getFontAscent(Font font)
	{
		//Does not eixsts in C#... use GetHeigth instead
		return font.GetHeight();
		

		//LineMetrics lm = font.getLineMetrics(DUMMY_TEXT, gd.getFontRenderContext());
		//return lm.getAscent();
	}

	internal double getFontHeight(Font font)
	{
		return font.GetHeight();
	}

	internal double getStringWidth(String text, Font font)
	{
		return gd.MeasureString(text,font).Width;
	}

	internal void setAntiAliasing(bool enable)
	{
		gd.SmoothingMode = enable ? SmoothingMode.AntiAlias : SmoothingMode.HighSpeed;
	}

	internal void dispose() {
		gd.Dispose();
	}

	internal void saveImage(Stream stream, String type, float quality)
	{
		if(type.Equals("png",StringComparison.InvariantCultureIgnoreCase)) {
			 img.Save(stream,ImageFormat.Png);
		}
		else if (type.Equals("gif",StringComparison.InvariantCultureIgnoreCase)) {
			img.Save(stream,ImageFormat.Gif);
		}
		else if ((type.Equals("jpg",StringComparison.InvariantCultureIgnoreCase))||
		         (type.Equals("jpg",StringComparison.InvariantCultureIgnoreCase))) {
			img.Save(stream,ImageFormat.Jpeg);
		}
		else {
			throw new IOException("Unsupported image format: " + type);
		}
		stream.Flush();
	}

	internal byte[] saveImage(String path, String type, float quality)
	{
		using(FileStream ms = File.Create(path))
		{
			saveImage(ms, type, quality);
		}
		return File.ReadAllBytes(path);
	}

	internal byte[] getImageBytes(String type, float quality){
		using(MemoryStream ms = new MemoryStream())
		{
			saveImage(ms, type, quality);

			ms.Seek(0, SeekOrigin.Begin);
			byte[] buffer = new byte[ms.Length];
			ms.Read(buffer, 0, buffer.Length);
			return buffer;
		}

	}

	internal void loadImage(String imageFile)
	{
		Image img = Image.FromFile(imageFile);
		gd.DrawImage(img,img.Height,img.Width);
	}
}

}