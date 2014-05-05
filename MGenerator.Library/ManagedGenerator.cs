/*
 * Created by SharpDevelop.
 * User: Rex
 * Date: 1/13/2011
 * Time: 2:46 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using System.CodeDom;
using System.CodeDom.Compiler;

namespace MGenerator.Library
{
    /// <summary>
    /// 
    /// </summary>
    public class DomainInfo
    {
        public String CompanyName { get; set; }
        public String FrameworkName { get; set; }
        public String DomainName { get; set; }
        public GenerationInfo GenerationOption { get; set; } 


        #region [ Contructors ] 
        /// <summary>
        /// [0] Constructor 
        /// </summary>
        public DomainInfo()
        {
        }
        /// <summary>
        /// [1] Constructor 
        /// </summary>
        /// <param name="CompanyName"></param>
        /// <param name="FrameworkName"></param>
        /// <param name="DomainName"></param>
        public DomainInfo(String CompanyName, String FrameworkName, String DomainName)
        {

        }
        #endregion 
    }

	/// <summary>
	/// Description of ManagedGenerator.
	/// </summary>
	public class ManagedGenerator
	{
		public ManagedGenerator()
		{
			
		}
	}
}
