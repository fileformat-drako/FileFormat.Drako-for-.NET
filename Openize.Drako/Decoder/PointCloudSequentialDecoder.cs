using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Openize.Draco.Decoder
{
    class PointCloudSequentialDecoder : PointCloudDecoder
    {
        public PointCloudSequentialDecoder()
            : base(EncodedGeometryType.PointCloud)
        {

        }

        protected override bool DecodeGeometryData()
        {
            int num_points;
            if (!buffer.Decode(out num_points))
                return false;
            PointCloud.NumPoints = num_points;
            return true;
        }

        protected override bool CreateAttributesDecoder(int attrDecoderId)
        {
            // Always create the basic attribute decoder.
            return SetAttributesDecoder(
                attrDecoderId,
                new SequentialAttributeDecodersController(
                    new LinearSequencer(PointCloud.NumPoints)));
        }
    }
}
