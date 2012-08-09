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

namespace robin.tests
{
[TestClass]
public class RRDFileTest {

	[TestMethod]
	[ExpectedException(typeof(RrdException))]
	public void testTooShortForHeader()
	{
		string tempFile = Path.GetTempFileName();
		try
		{
			File.WriteAllBytes(tempFile, new byte[1]);

			using(RRDFile rrdFile = new RRDFile(tempFile)){}
			
		}
		finally
		{
			File.Delete(tempFile);
		}
	}

	[TestMethod]
	[ExpectedException(typeof(RrdException))]
	public void testNoFloatCookie()
	{
		string tempFile = Path.GetTempFileName();
		try
		{
			using (FileStream outputStream = File.OpenWrite(tempFile))
			{
				this.write32BitHeaderToVersion(outputStream);
				byte[] padding = new byte[24];

				outputStream.Write(padding, 0, padding.Length);
			}
			using(RRDFile rrdFile = new RRDFile(tempFile)){}
			
		}
		finally
		{
			try
			{
				File.Delete(tempFile);
			}
			catch (Exception)
			{
				
			}
			
		}
	}

	[TestMethod]
	public void test32BitLittleEndianness() {
		string tempFile = Path.GetTempFileName();
		try
		{
			using (FileStream outputStream = File.OpenWrite(tempFile))
			{
				write32BitLittleEndianHeaderToFloatCookie(outputStream);
				byte[] padding = new byte[24];
				outputStream.Write(padding, 0, padding.Length);
			}

			using(RRDFile rrdFile = new RRDFile(tempFile))
			{
				Assert.IsFalse(rrdFile.IsBigEndian, "Expected little endian");
				Assert.AreEqual(4, rrdFile.Alignment, "Expected 4-byte alignment");
			}

		}
		finally
		{
			try
			{
				File.Delete(tempFile);
			}
			catch (Exception)
			{

			}
		}
	}

	[TestMethod]
	public void test32BitBigEndianness(){
		string tempFile = Path.GetTempFileName();
		try
		{
			using (FileStream outputStream = File.OpenWrite(tempFile))
			{
				write32BitBigEndianHeaderToFloatCookie(outputStream);
				byte[] padding = new byte[24];
				outputStream.Write(padding, 0, padding.Length);
			}
		
			using(RRDFile rrdFile = new RRDFile(tempFile))
			{
				Assert.IsTrue(rrdFile.IsBigEndian, "Expected big endian");
				Assert.AreEqual(4, rrdFile.Alignment, "Expected 4-byte alignment");
			}

		}
		finally
		{
			try
			{
				File.Delete(tempFile);
			}
			catch (Exception)
			{

			}
		}
	}

	[TestMethod]
	public void test64BitLittleEndianness(){
		string tempFile = Path.GetTempFileName();
		try
		{
			using (FileStream outputStream = File.OpenWrite(tempFile))
			{
				write64BitLittleEndianHeaderToFloatCookie(outputStream);
				byte[] padding = new byte[24];
				outputStream.Write(padding, 0, padding.Length);
			}

			using(RRDFile rrdFile = new RRDFile(tempFile))
			{
				Assert.IsFalse(rrdFile.IsBigEndian, "Expected little endian");
				Assert.AreEqual(8, rrdFile.Alignment, "Expected 8-byte alignment");
			}

		}
		finally
		{
			try
			{
				File.Delete(tempFile);
			}
			catch (Exception)
			{

			}
		}
	}

	[TestMethod]
	public void test64BitBigEndianness(){
		string tempFile = Path.GetTempFileName();
		try
		{
			using (FileStream outputStream = File.OpenWrite(tempFile))
			{
				write64BitBigEndianHeaderToFloatCookie(outputStream);
				byte[] padding = new byte[24];
				outputStream.Write(padding, 0, padding.Length);
			}

			using(RRDFile rrdFile = new RRDFile(tempFile))
			{
				Assert.IsTrue(rrdFile.IsBigEndian, "Expected big endian");
				Assert.AreEqual(8, rrdFile.Alignment, "Expected 8-byte alignment");
			}

		}
		finally
		{
			try
			{
				File.Delete(tempFile);
			}
			catch (Exception)
			{

			}
		}
	}
	
	[TestMethod]
	public void testReadInt32BitLittleEndian(){
	string tempFile = Path.GetTempFileName();
		try
		{
			using (FileStream outputStream = File.OpenWrite(tempFile))
			{
				write32BitLittleEndianHeaderToFloatCookie(outputStream);
				//Write out 3 integers (the rest of the normal header), each 32 bits, in little endian format.
				byte[] int1 = {0x12, 0x34, 0x56, 0x78}; //Gives the integer 0x78563412 in little endian
				byte[] int2 = {0x78, 0x56, 0x34, 0x12}; //Gives the integer 0x12345678 in little endian
				byte[] int3 = {0x34, 0x12, 0x78, 0x56}; //Gives the integer 0x56781234 in little endian

				outputStream.Write(int1,0,4);
				outputStream.Write(int2,0,4);
				outputStream.Write(int3,0,4);

			}

			using(RRDFile rrdFile = new RRDFile(tempFile))
			{
				rrdFile.SkipBytes(20); //Skip the string cookie, version, padding, and float cookie
				Assert.AreEqual(0x78563412, rrdFile.ReadInt());
				Assert.AreEqual(0x12345678, rrdFile.ReadInt());
				Assert.AreEqual(0x56781234, rrdFile.ReadInt());
			}

		}
		finally
		{
			try
			{
				File.Delete(tempFile);
			}
			catch (Exception)
			{

			}
		}
	}
	
	[TestMethod]
	public void testReadInt32BitBigEndian(){
	string tempFile = Path.GetTempFileName();
		try
		{
			using (FileStream outputStream = File.OpenWrite(tempFile))
			{
				write32BitBigEndianHeaderToFloatCookie(outputStream);
				//Write out 3 integers (the rest of the normal header), each 32 bits, in little endian format.
				byte[] int1 = {0x12, 0x34, 0x56, 0x78}; //Gives the integer 0x78563412 in little endian
				byte[] int2 = {0x78, 0x56, 0x34, 0x12}; //Gives the integer 0x12345678 in little endian
				byte[] int3 = {0x34, 0x12, 0x78, 0x56}; //Gives the integer 0x56781234 in little endian

				outputStream.Write(int1,0,4);
				outputStream.Write(int2,0,4);
				outputStream.Write(int3,0,4);

			}

			using(RRDFile rrdFile = new RRDFile(tempFile))
			{
				rrdFile.SkipBytes(20); //Skip the string cookie, version, padding, and float cookie
				Assert.AreEqual(0x12345678, rrdFile.ReadInt());
				Assert.AreEqual(0x78563412, rrdFile.ReadInt());
				Assert.AreEqual(0x34127856, rrdFile.ReadInt());
			}
		}
		finally
		{
			try
			{
				File.Delete(tempFile);
			}
			catch (Exception)
			{

			}
		}
	}
	
	[TestMethod]
	public void testReadInt64BitLittleEndian(){
	string tempFile = Path.GetTempFileName();
		try
		{
			using (FileStream outputStream = File.OpenWrite(tempFile))
			{
				write64BitLittleEndianHeaderToFloatCookie(outputStream);
				//Write out 3 integers (the rest of the normal header), each 32 bits, in little endian format.
				byte[] int1 = {0x12, 0x34, 0x56, 0x78, 0x77, 0x66, 0x55, 0x44}; //Gives the integer 0x78563412 in little endian
				byte[] int2 = {0x78, 0x56, 0x34, 0x12, 0x77, 0x66, 0x55, 0x44}; //Gives the integer 0x12345678 in little endian
				byte[] int3 = {0x34, 0x12, 0x78, 0x56, 0x77, 0x66, 0x55, 0x44}; //Gives the integer 0x56781234 in little endian

				outputStream.Write(int1,0,8);
				outputStream.Write(int2,0,8);
				outputStream.Write(int3,0,8);

			}

			using(RRDFile rrdFile = new RRDFile(tempFile))
			{
				rrdFile.SkipBytes(24); //Skip the string cookie, version, padding, and float cookie
				Assert.AreEqual(0x78563412, rrdFile.ReadInt());
				Assert.AreEqual(0x12345678, rrdFile.ReadInt());
				Assert.AreEqual(0x56781234, rrdFile.ReadInt());
			}

		}
		finally
		{
			try
			{
				File.Delete(tempFile);
			}
			catch (Exception)
			{

			}
		}
	}
	
	
	[TestMethod]
	public void testReadInt64BitBigEndian(){
	string tempFile = Path.GetTempFileName();
		try
		{
			using (FileStream outputStream = File.OpenWrite(tempFile))
			{
				write64BitBigEndianHeaderToFloatCookie(outputStream);
				//Write out 3 integers (the rest of the normal header), each 64 bits, in little endian format.
				//However, we're expecting only an int (32-bits) back, so the value we're expecting from a 
				// big-endian file is the *last* four bytes only.  The first four bytes are ignored.
				//We write them with real possibly mis-interpretable numbers though, to double check 
				//that it's reading correctly
				byte[] int1 = { 0x77, 0x66, 0x55, 0x44, 0x78, 0x56, 0x34, 0x12};
				byte[] int2 = { 0x77, 0x66, 0x55, 0x44, 0x12, 0x34, 0x56, 0x78};
				byte[] int3 = { 0x77, 0x66, 0x55, 0x44, 0x78, 0x12, 0x56, 0x34};

				outputStream.Write(int1,0,8);
				outputStream.Write(int2,0,8);
				outputStream.Write(int3,0,8);

			}

			using(RRDFile rrdFile = new RRDFile(tempFile))
			{
				rrdFile.SkipBytes(24); //Skip the string cookie, version, padding, and float cookie
				Assert.AreEqual(0x78563412, rrdFile.ReadInt());
				Assert.AreEqual(0x12345678, rrdFile.ReadInt());
				Assert.AreEqual(0x78125634, rrdFile.ReadInt());
			}

		}
		finally
		{
			try
			{
				File.Delete(tempFile);
			}
			catch (Exception)
			{

			}
		}
	}
	
	///*
	// * No need to test readDouble specifically; it's been tested by all the other test writing the
	// * float cookies and reading them back in the RRDFile constructor.
	// * If there's a problem, it'll show up there
	// */
	
	[TestMethod]
	public void testReadString() {
		string tempFile = Path.GetTempFileName();
		try
		{
			using (FileStream outputStream = File.OpenWrite(tempFile))
			{
				write64BitLittleEndianHeaderToFloatCookie(outputStream);
			}

			//The first 4 bytes of the file must be null terminated string "RRD" (Constants.COOKIE)
			//That's a good enough test
			using(RRDFile rrdFile = new RRDFile(tempFile))
			{
				String cookie = rrdFile.ReadString(4);
				Assert.AreEqual(Constants.COOKIE, cookie);
			}
		}
		finally
		{
			try
			{
				File.Delete(tempFile);
			}
			catch (Exception)
			{

			}
		}
	}
	
	/*Writes the header, up to the float cookie, for a 32 bit little endian file */
	private void write32BitLittleEndianHeaderToFloatCookie(FileStream outputStream){
	   write32BitHeaderToVersion(outputStream);
		outputStream.Write(Constants.FLOAT_COOKIE_LITTLE_ENDIAN, 0, Constants.FLOAT_COOKIE_LITTLE_ENDIAN.Length);
	}
	
	/*Writes the header, up to the float cookie, for a 32 bit little endian file */
	private void write32BitBigEndianHeaderToFloatCookie(FileStream outputStream){
	   this.write32BitHeaderToVersion(outputStream);
		outputStream.Write(Constants.FLOAT_COOKIE_BIG_ENDIAN, 0, Constants.FLOAT_COOKIE_BIG_ENDIAN.Length);
	}

	//The same for little or big endian (byte by byte text basically)
	//But, padding varies for 32 vs 64 bit
	private void write32BitHeaderToVersion(FileStream outputStream)
	{
		outputStream.Write(Encoding.ASCII.GetBytes(Constants.COOKIE),0,Constants.COOKIE.Length);
		outputStream.WriteByte(0); //Null terminate the string
		outputStream.Write(Encoding.ASCII.GetBytes(Constants.VERSION3), 0, Constants.VERSION3.Length);
		for (int i = 0; i < 4; i++)
		{
			outputStream.WriteByte(0); //Null terminate the string and add 3 null bytes to pad to 32-bits
		}
	}

	/*Writes the header, up to the float cookie, for a 32 bit little endian file */
	private void write64BitLittleEndianHeaderToFloatCookie(FileStream outputStream){
		
	   this.write64BitHeaderToVersion(outputStream);
		outputStream.Write(Constants.FLOAT_COOKIE_LITTLE_ENDIAN, 0, Constants.FLOAT_COOKIE_LITTLE_ENDIAN.Length);
	}

	/*Writes the header, up to the float cookie, for a 32 bit little endian file */
	private void write64BitBigEndianHeaderToFloatCookie(FileStream outputStream) {
		
	   this.write64BitHeaderToVersion(outputStream);
		outputStream.Write(Constants.FLOAT_COOKIE_BIG_ENDIAN, 0, Constants.FLOAT_COOKIE_BIG_ENDIAN.Length);
	}

	//The same for little or big endian (byte by byte text basically)
	//But, padding varies for 32 vs 64 bit
	private void write64BitHeaderToVersion(FileStream outputStream){
	   outputStream.Write(Encoding.ASCII.GetBytes(Constants.COOKIE),0,Constants.COOKIE.Length);
	   outputStream.WriteByte(0); //Null terminate the string
	   outputStream.Write(Encoding.ASCII.GetBytes(Constants.VERSION3),0,Constants.VERSION3.Length);
	   for(int i=0; i<8; i++) {
	      outputStream.WriteByte(0); //Null terminate the string and add 7 null bytes to pad to 64-bits
	   }
	}
}
}
