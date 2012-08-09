/*******************************************************************************
 * Copyright (c) 2011 Craig Miskell
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
using robin.core;
using robin.data;

namespace robin.graph
{
public class PercentileDef : Source {
    private String m_sourceName;

    private double m_percentile;

	public PercentileDef(String name, String sourceName, double percentile):base(name) {
        m_sourceName = sourceName;
        m_percentile = percentile;
    }

	internal override void requestData(DataProcessor dproc)
	 {
        dproc.AddDatasource(name, m_sourceName, m_percentile);
    }

}
}