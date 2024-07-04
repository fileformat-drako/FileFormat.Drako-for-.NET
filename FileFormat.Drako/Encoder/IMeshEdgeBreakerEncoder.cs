using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileFormat.Drako.Encoder
{
    interface IMeshEdgeBreakerEncoder
    {

        bool Init(MeshEdgeBreakerEncoder encoder);

        MeshAttributeCornerTable GetAttributeCornerTable(int attId);
        MeshAttributeIndicesEncodingData GetAttributeEncodingData(int attId);
        bool GenerateAttributesEncoder(int attId);
        bool EncodeAttributesEncoderIdentifier(int attEncoderId);
        bool EncodeConnectivity();

        /// <summary>
        /// Returns corner table of the encoded mesh.
        /// </summary>
        CornerTable CornerTable { get; }
        MeshEdgeBreakerEncoder Encoder { get; }
    }
}
