﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Openize.Drako.Decoder
{
    interface IMeshEdgeBreakerDecoderImpl
    {

        bool Init(MeshEdgeBreakerDecoder decoder);

        MeshAttributeCornerTable GetAttributeCornerTable(int attId);
        MeshAttributeIndicesEncodingData GetAttributeEncodingData(int attId);
        bool CreateAttributesDecoder(int attDecoderId);
        bool DecodeConnectivity();
        bool OnAttributesDecoded();

        MeshEdgeBreakerDecoder GetDecoder();
        CornerTable CornerTable { get; }
    }
}
