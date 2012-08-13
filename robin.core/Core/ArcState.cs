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
using System.Xml;

namespace robin.core
{
	/// <summary>
	/// Class to represent internal RRD archive state for a single datasource. Objects of this
	/// class are never manipulated directly, it's up to JRobin framework to manage
	/// internal arcihve states.
	/// 
	/// @author <a href="mailto:saxon@jrobin.org">Sasa Markovic</a>
	/// </summary>
	internal class ArcState : IRrdUpdater
	{
		private readonly RrdDouble accumumatedValue;
		private readonly RrdLong nanSteps;
		private readonly Archive parentArc;

		internal ArcState(Archive parentArc, bool shouldInitialize)
		{
			this.parentArc = parentArc;
			accumumatedValue = new RrdDouble(this);
			nanSteps = new RrdLong(this);
			if (shouldInitialize)
			{
				Header header = parentArc.ParentDb.Header;
				long step = header.Step;
				long lastUpdateTime = header.LastUpdateTime;
				long arcStep = parentArc.TimeStep;
				long initNanSteps = (Util.Normalize(lastUpdateTime, step) -
				                     Util.Normalize(lastUpdateTime, arcStep))/step;
				accumumatedValue.Set(Double.NaN);
				nanSteps.Set(initNanSteps);
			}
		}

		/// <summary>
		/// Number of currently accumulated NaN steps.
		/// </summary>
		/// <value></value>
		public long NanSteps
		{
			set { nanSteps.Set(value); }
			get { return nanSteps.Get(); }
		}

		/// <summary>
		/// Returns the value accumulated so far.
		/// </summary>
		/// <value></value>
		public double AccumulatedValue
		{
			set { accumumatedValue.Set(value); }
			get { return accumumatedValue.Get(); }
		}

		#region IRrdUpdater Members

		/// <summary>
		/// Copies object's internal state to another ArcState object.
		/// </summary>
		/// <param name="other"> New ArcState object to copy state to</param>
		public void CopyStateTo(IRrdUpdater other)
		{
			if (!(other is ArcState))
			{
				throw new RrdException(
					"Cannot copy ArcState object to " + other.GetType().Name);
			}
			var arcState = (ArcState) other;
			arcState.accumumatedValue.Set(accumumatedValue.Get());
			arcState.nanSteps.Set(nanSteps.Get());
		}

		/// <summary>
		/// Returns the underlying storage (backend) object which actually performs all
		/// I/O operations.
		/// </summary>
		/// <returns></returns>
		public RrdBackend GetRrdBackend()
		{
			return parentArc.GetRrdBackend();
		}

		/// <summary>
		/// Required to implement RrdUpdater interface. You should never call this method directly.
		/// </summary>
		/// <returns></returns>
		public RrdAllocator GetRrdAllocator()
		{
			return parentArc.GetRrdAllocator();
		}

		#endregion

		internal String Dump()
		{
			return "accumValue:" + accumumatedValue.Get() + " nanSteps:" + nanSteps.Get() + "\n";
		}

		/// <summary>
		/// Returns the Archive object to which this ArcState object belongs.
		/// </summary>
		/// <returns></returns>
		public Archive GetParent()
		{
			return parentArc;
		}

		internal void AppendXml(XmlWriter writer)
		{
			writer.WriteStartElement("ds");
			writer.WriteElementString("value", Util.FormatDouble(accumumatedValue.Get()));
			writer.WriteElementString("unknown_datapoints", nanSteps.Get().ToString());
			writer.WriteEndElement(); // ds
		}

		public override String ToString()
		{
			return "ArcState@" + GetHashCode().ToString("X") + "[parentArc=" + parentArc + ",accumValue=" + accumumatedValue +
			       ",nanSteps=" +
			       nanSteps + "]";
		}
	}
}