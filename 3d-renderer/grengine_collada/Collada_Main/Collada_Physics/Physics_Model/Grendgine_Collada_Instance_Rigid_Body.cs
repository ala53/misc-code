using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
namespace ColladaDotNet
{
	[System.SerializableAttribute()]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
	[System.Xml.Serialization.XmlRootAttribute(ElementName="instance_rigid_body", Namespace="http://www.collada.org/2005/11/COLLADASchema", IsNullable=true)]
	public partial class Collada_Instance_Rigid_Body
	{
		[XmlAttribute("sid")]
		public string sID;

		[XmlAttribute("name")]
		public string Name;

		[XmlAttribute("body")]
		public string Body;		

		[XmlAttribute("target")]
		public string Target;		
		
		[XmlElement(ElementName = "technique_common")]
		public Collada_Technique_Common_Instance_Rigid_Body Technique_Common;
	    
		[XmlElement(ElementName = "technique")]
		public Collada_Technique[] Technique;		
		
	    [XmlElement(ElementName = "extra")]
		public Collada_Extra[] Extra;	
	}
}

