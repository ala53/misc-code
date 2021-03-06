using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
namespace ColladaDotNet
{
	[System.SerializableAttribute()]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
	[System.Xml.Serialization.XmlRootAttribute(ElementName="cylinder", Namespace="http://www.collada.org/2005/11/COLLADASchema", IsNullable=true)]
	public partial class Collada_Cylinder
	{
		[XmlElement(ElementName = "height")]
		public float Height;		

		[XmlElement(ElementName = "radius")]
		public Collada_Float_Array_String Radius;		

		[XmlElement(ElementName = "extra")]
		public Collada_Extra[] Extra;	
	}
}

