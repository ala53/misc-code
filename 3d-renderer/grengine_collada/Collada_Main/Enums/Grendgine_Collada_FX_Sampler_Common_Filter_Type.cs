using System;
namespace ColladaDotNet
{
	[System.SerializableAttribute()]
	[System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.collada.org/2005/11/COLLADASchema" )]
	public enum Collada_FX_Sampler_Common_Filter_Type
	{
		NONE,
		NEAREST,
		LINEAR,
		ANISOTROPIC
		
	}
}
