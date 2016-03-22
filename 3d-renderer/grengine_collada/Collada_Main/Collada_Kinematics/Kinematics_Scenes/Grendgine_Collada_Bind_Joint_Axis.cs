using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
namespace ColladaDotNet
{
	[System.SerializableAttribute()]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
	[System.Xml.Serialization.XmlRootAttribute(ElementName="bind_joint_axis", Namespace="http://www.collada.org/2005/11/COLLADASchema", IsNullable=true)]
	public partial class Collada_Bind_Joint_Axis
	{
		[XmlAttribute("target")]
		public string Target;


		[XmlElement(ElementName = "axis")]
		public Collada_Common_SIDREF_Or_Param_Type Axis;	
		
		[XmlElement(ElementName = "value")]
		public Collada_Common_Float_Or_Param_Type Value;	
		

	}
}
