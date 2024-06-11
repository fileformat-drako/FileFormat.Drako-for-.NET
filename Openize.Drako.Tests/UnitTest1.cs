using Openize.Draco;
using Openize.Draco.Utils;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Draco.NET.Tests;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void DecodeFromDrcFile()
    {
        var cube = File.ReadAllBytes(@"TestData/cube.drc");
        var dm = Openize.Draco.Draco.Decode(cube);
        var opt = new DracoEncodeOptions();
        var bytes = Openize.Draco.Draco.Encode(dm, opt);
        var dm2 = Openize.Draco.Draco.Decode(bytes);
        Assert.IsNotNull(dm2);
        Assert.AreEqual(3, dm2.NumAttributes);
        var attr = dm2.GetNamedAttribute(AttributeType.Position);
        Assert.IsNotNull(attr);

        unsafe
        {
            fixed(byte*p = attr.Buffer.GetBuffer())
            {
                var span = new Span<Vector3>(p, attr.Buffer.Length / sizeof(Vector3));
                for (var i = 0; i < span.Length; i++)
                {
                    Console.WriteLine(span[i]);
                }
            }
        }
        attr.Buffer.GetBuffer();


    }


    [TestMethod]
    public void EncodeMeshToDrc()
    {

        Vector3[] controlPoints = new Vector3[]
        {
                new Vector3( -5, 0, 5.0f),
                new Vector3( 5, 0, 5.0f),
                new Vector3( 5, 10, 5.0f),
                new Vector3( -5, 10, 5.0f),
                new Vector3( -5, 0, -5.0f),
                new Vector3( 50, 0, -5.0f),
                new Vector3( 5, 10, -5.0f),
                new Vector3( -5, 10, -5.0f)
        };

        int[] indices = new int[]
        {
                0,1,2,3, // Front face (Z+)
                1,5,6,2, // Right side (X+)
                5,4,7,6, // Back face (Z-)
                4,0,3,7, // Left side (X-)
                0,4,5,1, // Bottom face (Y-)
                3,2,6,7 // Top face (Y+)
        };


        var mesh = new DracoMesh();
        //construct an attribute for position, with type float[3], 
        var attrPos = PointAttribute.Wrap(AttributeType.Position, controlPoints);
        mesh.AddAttribute(attrPos);
        //add triangle indices
        mesh.Indices.AddRange(indices);
        //number of the control points, it's required for the encoder to produce correct result.
        mesh.NumPoints = 8;
        //You can also use following methods to deduplicate the attributes to reduce the file size
        //mesh.DeduplicateAttributeValues();
        //mesh.DeduplicatePointIds();

        var opt = new DracoEncodeOptions();
        var drcBytes = Openize.Draco.Draco.Encode(mesh, opt);
        var mesh2 = (DracoMesh)Openize.Draco.Draco.Decode(drcBytes);
        Assert.IsNotNull(mesh2);

    }
}
