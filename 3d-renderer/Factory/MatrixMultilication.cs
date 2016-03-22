using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Factory
{
    static class MatrixMultiplication
    {
        public static Vector3 MultiplyByMatrix4(this Vector3 vector, Matrix4 matrix)
        {
            Vector3 output = new Vector3();
            output.X = (vector.X * matrix.M11)
                + (vector.Y * matrix.M12)
                + (vector.Z * matrix.M13)
                + matrix.M14;
            output.Y = (vector.X * matrix.M21)
                + (vector.Y * matrix.M22)
                + (vector.Z * matrix.M23)
                + matrix.M24;
            output.Z = (vector.X * matrix.M31)
                + (vector.Y * matrix.M32)
                + (vector.Z * matrix.M33)
                + matrix.M34;
            return output;
        }
    }
}

