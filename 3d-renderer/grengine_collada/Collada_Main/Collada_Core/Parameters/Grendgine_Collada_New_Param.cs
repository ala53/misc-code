using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
namespace ColladaDotNet
{
	[System.SerializableAttribute()]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
	public partial class Collada_New_Param
	{
		[XmlAttribute("sid")]
		public string sID;
		
		[XmlElement(ElementName = "semantic")]
		public string Semantic;				
		
		[XmlElement(ElementName = "modifier")]
		public string Modifier;				
		
		[XmlElement("annotate")]
		public Collada_Annotate[] Annotate;
	
		/// <summary>
		/// The element is the type and the element text is the value or space delimited list of values
		/// </summary>
		[XmlAnyElement]
		public XmlElement[] Data;	
	}
}

