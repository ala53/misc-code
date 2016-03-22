using Factory.Physics;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Factory.Renderers
{
    public interface IObjectRenderer
    {
        ColladaGeometry AttachedObject {get; set;}
        int DisplayListID { get; set; }
        void ShareDisplayList(int otherDisplayList);
        int BuildDisplayList(bool drawDebug = false);
        void Render();
    }
}
