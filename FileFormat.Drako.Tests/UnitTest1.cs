using FileFormat.Drako;
using FileFormat.Drako.Utils;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace FileFormat.Drako.Tests;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void DecodeFromDrcFile()
    {
        var cube = File.ReadAllBytes(@"TestData/cube.drc");
        var dm = Draco.Decode(cube);
        var opt = new DracoEncodeOptions();
        var bytes = Draco.Encode(dm, opt);
        var dm2 = Draco.Decode(bytes);
        Assert.IsNotNull(dm2);
        Assert.AreEqual(3, dm2.NumAttributes);
        var attr = dm2.GetNamedAttribute(AttributeType.Position);
        Assert.IsNotNull(attr);
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
                new Vector3( 5, 0, -5.0f),
                new Vector3( 5, 10, -5.0f),
                new Vector3( -5, 10, -5.0f)
        };

        int[] indices = new int[]
        {
                0,1,2, 0, 2, 3, // Front face (Z+)
                1,5,6, 1, 6, 2, // Right side (X+)
                5,4,7, 5, 7, 6, // Back face (Z-)
                4,0,3, 4, 3, 7, // Left side (X-)
                0,4,5, 0, 5, 1, // Bottom face (Y-)
                3,2,6, 3, 6, 7 // Top face (Y+)
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
        var drcBytes = Draco.Encode(mesh, opt);
        var mesh2 = (DracoMesh)Draco.Decode(drcBytes);
        Assert.IsNotNull(mesh2);

    }
}
