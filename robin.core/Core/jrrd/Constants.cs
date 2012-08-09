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

namespace robin.core.jrrd
{
	internal class Constants
	{
		public const int DS_NAM_SIZE = 20;
		public const int DST_SIZE = 20;
		public const int CF_NAM_SIZE = 20;
		public const int LAST_DS_LEN = 30;
		public const String COOKIE = "RRD";
		public const String VERSION = "0001";
		public const String VERSION3 = "0003";
		public const double FLOAT_COOKIE = 8.642135E130;

		public static byte[] FLOAT_COOKIE_BIG_ENDIAN = {
		                                               	0x5B, 0x1F, 0x2B, 0x43,
		                                               	0xC7, 0xC0, 0x25,
		                                               	0x2F
		                                               };

		public static byte[] FLOAT_COOKIE_LITTLE_ENDIAN = {
		                                                  	0x2F, 0x25, 0xC0,
		                                                  	0xC7, 0x43, 0x2B, 0x1F,
		                                                  	0x5B
		                                                  };
	}
}