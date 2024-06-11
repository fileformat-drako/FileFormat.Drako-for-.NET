using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Openize.Draco.Decoder
{
    class PointCloudKdTreeDecoder : PointCloudDecoder
    {
        public PointCloudKdTreeDecoder() : base(EncodedGeometryType.PointCloud)
        {
        }

        protected override bool DecodeGeometryData()
        {
            int num_points;
            if (!Buffer.Decode(out num_points))
                return false;
            if (num_points < 0)
                return false;
            PointCloud.NumPoints = num_points;
            return true;
        }

        protected override bool CreateAttributesDecoder(int attrDecoderId)
        {
            // Always create the basic attribute decoder.
            return SetAttributesDecoder(attrDecoderId, new KdTreeAttributesDecoder());
        }
    }
}
