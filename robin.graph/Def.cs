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
using robin.core;
using robin.data;

namespace robin.graph
{
class Def : Source {
	private String rrdPath, dsName, backend;
	private ConsolidationFunction consolFun;

	public Def(String name, String rrdPath, String dsName, ConsolidationFunction consolFun):this(name, rrdPath, dsName, consolFun, null) {
		
	}

	public Def(String name, String rrdPath, String dsName, ConsolidationFunction consolFun, String backend):base(name) {
		this.rrdPath = rrdPath;
		this.dsName = dsName;
		this.consolFun = consolFun;
		this.backend = backend;
	}

	internal override void requestData(DataProcessor dproc) {
		if (backend == null) {
			dproc.AddDatasource(name, rrdPath, dsName, consolFun);
		}
		else {
			dproc.AddDatasource(name, rrdPath, dsName, consolFun, backend);
		}
	}
}

}