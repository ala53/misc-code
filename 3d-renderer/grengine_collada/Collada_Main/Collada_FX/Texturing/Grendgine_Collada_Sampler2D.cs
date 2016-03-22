using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
namespace ColladaDotNet
{
	[System.SerializableAttribute()]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
	[System.Xml.Serialization.XmlRootAttribute(ElementName="sampler2D", Namespace="http://www.collada.org/2005/11/COLLADASchema", IsNullable=true)]
	public partial class Collada_Sampler2D : Collada_FX_Sampler_Common
	{

	}
}

