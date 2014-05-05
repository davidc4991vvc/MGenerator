/*
 * Created by SharpDevelop.
 * User: Rex
 * Date: 1/13/2011
 * Time: 2:55 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace MGenerator.Library
{
	/// <summary>
	/// Description of GenerationInfo.
	/// </summary>
	public class GenerationInfo
	{
		// String FolderPath, String ServerName, String DatabaseName, String LayerNamespace
		public String FolderPath { get; set; } 
		public String ServerName { get; set; } 
		// using BrightGreen
		public String CompanyName { get; set; }  // {0}
		// using BrightGreen.Data
		public String DataNameSpace { get; set; } // {1} 
		
		public GenerationInfo()
		{
		}
		
	}
}
