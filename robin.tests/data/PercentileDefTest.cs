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
using System.Globalization;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using robin.core;
using robin.core.jrrd;
using robin.data;

namespace robin.tests
{
[TestClass]
public class PercentileDefTest {

    /**
     * Simple source that just accepts some values that are set; used for testing only
     * @author cmiskell
     *
     */
    internal class LocalSource : Source {
    	public LocalSource(String name):base(name) {
        }
    }
    
    [TestMethod]
    public void testNaNWithNoSource() {
        PercentileDef def = new PercentileDef("foo", null, 95);
        Assert.IsNull(def.Values);
        
        def.Timestamps = new long[] { 1 };
        double[] values = def.Values;
        Assert.IsNotNull(values);
        Assert.AreEqual(1, values.Length);
        Assert.AreEqual(Double.NaN, values[0], 0.0f);
    }
    
    private void commonTest(long[] timestamps, double[] inValues, double percentile, double expectedResult) {
        commonTest(timestamps, inValues, percentile, expectedResult, "");
    }
    
    /**
     * All these tests have a bunch of boilerplate code, encapsulated in this method
     * @throws RrdException
     */
    private void commonTest(long[] timestamps, double[] inValues, double percentile, double expectedResult, String comment){
        Source src = new LocalSource("bar");
        src.Timestamps = timestamps;
        src.Values = inValues;

        PercentileDef def = new PercentileDef("foo", src, percentile);
        Assert.IsNull(def.Values);

        def.Timestamps = timestamps;
        def.Calculate(0, timestamps.Length); //Must start from 0 to use the full list of values (otherwise the aggregation uses the last 9 values)
        double[] outValues = def.Values;
        Assert.IsNotNull(outValues);

        //Expect as many output values as we gave timestamps
        Assert.AreEqual(timestamps.Length, outValues.Length);

        foreach (double value in outValues) {
            Assert.AreEqual(expectedResult, value, 0.0,comment);
        }
    }

	 /**
	  * Test with a simple easy to understand input series
	  * @throws RrdException
	  */
	 [TestMethod]
	 public void testValueCalculations(){
	     long[] timestamps = new long[] { 1,2,3,4,5,6,7,8,9,10 };
	     double[] inValues = new double[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0 };
        
	     commonTest(timestamps, inValues, 90, 9.0);
	 }

    
	 /**
	  * Test with a non-so-simple input series (but still pretty simple)
	  * @throws RrdException
	  */
	 [TestMethod]
	 public void testValueCalculations2() {
	     long[] timestamps = new long[] { 1,2,3,4,5,6,7,8,9,10 };
	     double[] inValues = new double[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 8.5, 10.0 };
        
	     commonTest(timestamps, inValues, 90, 8.5);

	 }

	 /**
	  * Test with a non-so-simple input series (but still pretty simple)
	  * @throws RrdException
	  */
	 [TestMethod]
	 public void testValueCalculations3() {
	     long[] timestamps = new long[] { 1,2,3,4,5,6,7,8,9,10 };
	     double[] inValues = new double[] { 10.0, 9.0, 8.0, 7.0, 5.0, 6.0, 1.0, 2.0, 3.0, 4.0 };
        
	     commonTest(timestamps, inValues, 90, 9.0);
	 }
    
	 /**
	  * All the same value; ensure we get that value as the output, no matter what percentile we ask for
	  * @throws RrdException
	  */
	 [TestMethod]
	 public void testOneInputValue() {
	     long[] timestamps = new long[] { 1,2,3,4,5,6,7,8,9,10 };
	     double[] inValues = new double[] { 3.4, 3.4, 3.4, 3.4, 3.4, 3.4, 3.4, 3.4, 3.4, 3.4 };

	     //With 10 input values, asking for a percentile below "10%" should give NaN 
	     //it can't be 0, neither can it be any other number.  NaN is reasonable and correct
	     for(int i=0; i<10; i++) {
	         commonTest(timestamps, inValues, i, Double.NaN, "Percentile: "+i);
	     }
        
	     //However, all the rest of the values, up to 100%, should be the same for the single input value
	     for(int i=10; i<101; i++) {
	         commonTest(timestamps, inValues, i, 3.4, "Percentile: "+i);
	     }
	 }

	 /**
	  * Mostly the same value, one outlier; as for one input value, except that at 100% we should get the outlier
	  * @throws RrdException
	  */
	 [TestMethod]
	 public void testOneOutlier(){
	     long[] timestamps = new long[] { 1,2,3,4,5,6,7,8,9,10 };
	     double[] inValues = new double[] { 3.4, 3.4, 3.4, 7.5, 3.4, 3.4, 3.4, 3.4, 3.4, 3.4 };
        
	     //With 10 input values, asking for a percentile below "10%" should give NaN 
	     //it can't be 0, neither can it be any other number.  NaN is reasonable and correct
	     for(int i=0; i<10; i++) {
	         commonTest(timestamps, inValues, i, Double.NaN, "Percentile: "+i);
	     }
        
	     //However, all the rest of the values, up to 99%, should be the same for the single input value
	     for(int i=10; i<100; i++) {
	         commonTest(timestamps, inValues, i, 3.4, "Percentile: "+i);
	     }
	     commonTest(timestamps, inValues, 100, 7.5, "100%");

	 }

}

}