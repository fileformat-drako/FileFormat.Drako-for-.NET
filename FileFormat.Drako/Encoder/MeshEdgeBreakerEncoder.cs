using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Openize.Drako.Encoder
{
    /// <summary>
    /// Class implements the edge breaker geometry compression method as described
    /// in "3D Compression Made Simple: Edgebreaker on a Corner-Table" by Rossignac
    /// at al.'01. http://www.cc.gatech.edu/~jarek/papers/CornerTableSMI.pdf
    /// </summary>
    class MeshEdgeBreakerEncoder : MeshEncoder
    {
        private IMeshEdgeBreakerEncoder impl;

        public override CornerTable CornerTable
        {
            get { return impl.CornerTable; }
        }

        public override MeshAttributeCornerTable GetAttributeCornerTable(int attId)
        {
            return impl.GetAttributeCornerTable(attId);
        }

        public override MeshAttributeIndicesEncodingData GetAttributeEncodingData(int attId)
        {
            return impl.GetAttributeEncodingData(attId);
        }

        public override DracoEncodingMethod GetEncodingMethod()
        {
            return DracoEncodingMethod.EdgeBreaker;
        }

        protected override bool InitializeEncoder()
        {
            impl = null;
            if (options.CompressionLevel == DracoCompressionLevel.Optimal)
            {
                Buffer.Encode((byte) (1));
                impl = new MeshEdgeBreakerEncoderImpl(new MeshEdgeBreakerTraversalPredictiveEncoder());
            }
            else
            {
                Buffer.Encode((byte) (0));
                impl = new MeshEdgeBreakerEncoderImpl(new MeshEdgeBreakerTraversalEncoder());
            }
            if (!impl.Init(this))
                return false;
            return true;
        }

        protected override bool EncodeConnectivity()
        {
            return impl.EncodeConnectivity();
        }

        protected override bool GenerateAttributesEncoder(int attId)
        {

            if (!impl.GenerateAttributesEncoder(attId))
                return false;
            return true;
        }

        protected override bool EncodeAttributesEncoderIdentifier(int attEncoderId)
        {
            return impl.EncodeAttributesEncoderIdentifier(attEncoderId);

        }

    }
}
