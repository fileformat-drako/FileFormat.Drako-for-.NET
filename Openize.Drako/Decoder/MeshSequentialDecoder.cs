using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Openize.Draco.Utils;

namespace Openize.Draco.Decoder
{
    class MeshSequentialDecoder : MeshDecoder
    {
        protected override bool DecodeConnectivity()
        {
            uint numFaces;
            uint numPoints;
            if (BitstreamVersion < 22)
            {
                if (!Buffer.Decode(out numFaces))
                    return DracoUtils.Failed();
                if (!Buffer.Decode(out numPoints))
                    return DracoUtils.Failed();
            }
            else
            {
                if (!Decoding.DecodeVarint(out numFaces, buffer))
                    return false;
                if (!Decoding.DecodeVarint(out numPoints, buffer))
                    return false;
            }

            byte connectivityMethod;
            if (!Buffer.Decode(out connectivityMethod))
                return DracoUtils.Failed();
            if (connectivityMethod == 0)
            {
                if (!DecodeAndDecompressIndices((int)numFaces))
                    return DracoUtils.Failed();
            }
            else
            {
                if (numPoints < 256)
                {
                    // Decode indices as uint8T.
                    int[] face = new int[3];
                    for (int i = 0; i < numFaces; ++i)
                    {
                        for (int j = 0; j < 3; ++j)
                        {
                            byte val;
                            if (!Buffer.Decode(out val))
                                return DracoUtils.Failed();
                            face[j] = val;
                        }
                        Mesh.AddFace(face);
                    }
                }
                else if (numPoints < (1 << 16))
                {
                    // Decode indices as uint16T.
                    int[] face = new int[3];
                    for (int i = 0; i < numFaces; ++i)
                    {
                        for (int j = 0; j < 3; ++j)
                        {
                            ushort val;
                            if (!Buffer.Decode(out val))
                                return DracoUtils.Failed();
                            face[j] = val;
                        }
                        Mesh.AddFace(face);
                    }
                }
                else if (Mesh.NumPoints < (1 << 21) && BitstreamVersion >= 22)
                {
                    // Decode indices as uint32_t.
                    int[] face = new int[3];
                    for (int i = 0; i < numFaces; ++i)
                    {
                        for (int j = 0; j < 3; ++j)
                        {
                            uint val;
                            if (!Decoding.DecodeVarint(out val, buffer))
                                return false;
                            face[j] = (int)val;
                        }
                        Mesh.AddFace(face);
                    }
                }
                else
                {
                    // Decode faces as uint (default).
                    int[] face = new int[3];
                    for (int i = 0; i < numFaces; ++i)
                    {
                        for (int j = 0; j < 3; ++j)
                        {
                            int val;
                            if (!Buffer.Decode(out val))
                                return DracoUtils.Failed();
                            face[j] = val;
                        }
                        Mesh.AddFace(face);
                    }
                }
            }
            PointCloud.NumPoints = (int)numPoints;
            return true;
        }

        protected override bool CreateAttributesDecoder(int attrDecoderId)
        {

            // Always create the basic attribute decoder.
            SetAttributesDecoder(
                attrDecoderId,
                new SequentialAttributeDecodersController(
                    new LinearSequencer(PointCloud.NumPoints)));
            return true;
        }

        /// <summary>
        /// Decodes face indices that were compressed with an entropy code.
        /// Returns false on error.
        /// </summary>
        bool DecodeAndDecompressIndices(int numFaces)
        {

            // Get decoded indices differences that were encoded with an entropy code.
            Span<int> indicesBuffer = stackalloc int[numFaces * 3];
            if (!Decoding.DecodeSymbols(numFaces * 3, 1, Buffer, indicesBuffer))
                return DracoUtils.Failed();
            // Reconstruct the indices from the differences.
            // See MeshSequentialEncoder::CompressAndEncodeIndices() for more details.
            int lastIndexValue = 0;
            int vertexIndex = 0;
            int[] face = new int[3];
            for (int i = 0; i < numFaces; ++i)
            {
                for (int j = 0; j < 3; ++j)
                {
                    int encodedVal = indicesBuffer[vertexIndex++];
                    int indexDiff = (encodedVal >> 1);
                    if ((encodedVal & 1) != 0)
                        indexDiff = -indexDiff;
                    int indexValue = indexDiff + lastIndexValue;
                    face[j] = indexValue;
                    lastIndexValue = indexValue;
                }
                Mesh.AddFace(face);
            }
            return true;
        }

    }
}
