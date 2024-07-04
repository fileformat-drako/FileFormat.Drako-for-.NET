using Openize.Drako.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Openize.Drako.Decoder
{

    /// <summary>
    /// Class that reconstructs a 3D mesh from input data that was encoded by
    /// MeshEncoder.
    /// </summary>
    abstract class MeshDecoder : PointCloudDecoder
    {
        private DracoMesh mesh;

        public MeshDecoder()
            :base(EncodedGeometryType.TriangularMesh)
        {
            
        }

        public DracoMesh Mesh
        {
            get { return mesh; }
        }

        /// <summary>
        /// The main entry point for mesh decoding.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="inBuffer"></param>
        /// <param name="outMesh"></param>
        /// <param name="decodeData"></param>
        /// <returns></returns>
        public override bool Decode(DracoHeader header, DecoderBuffer inBuffer, DracoPointCloud outMesh, bool decodeData)
        {
            this.mesh = (DracoMesh)outMesh;
            return base.Decode(header, inBuffer, outMesh, decodeData);
        }

        /// <summary>
        /// Returns the base connectivity of the decoded mesh (or nullptr if it is not
        /// initialized).
        /// </summary>
        /// <returns></returns>
        public virtual CornerTable GetCornerTable()
        {
            return null;
        }

        /// <summary>
        /// Returns the attribute connectivity data or nullptr if it does not exist.
        /// </summary>
        /// <param name="attId"></param>
        /// <returns></returns>
        public virtual MeshAttributeCornerTable GetAttributeCornerTable(int attId)
        {
            return null;
        }

        /// <summary>
        /// Returns the decoding data for a given attribute or nullptr when the data
        /// does not exist.
        /// </summary>
        /// <param name="attId"></param>
        /// <returns></returns>
        public virtual MeshAttributeIndicesEncodingData GetAttributeEncodingData(int attId)
        {
            return null;
        }

        protected override bool DecodeGeometryData()
        {
            if (mesh == null)
                return DracoUtils.Failed();
            if (!DecodeConnectivity())
                return DracoUtils.Failed();
            return base.DecodeGeometryData();
        }

        protected abstract bool DecodeConnectivity();
    }
}
