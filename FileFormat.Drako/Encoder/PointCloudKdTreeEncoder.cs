using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileFormat.Drako.Encoder
{
    class PointCloudKdTreeEncoder : PointCloudEncoder
    {
        public override DracoEncodingMethod GetEncodingMethod()
        {
            return DracoEncodingMethod.KdTree;
        }

        protected override bool GenerateAttributesEncoder(int attId)
        {

            if (NumAttributesEncoders == 0)
            {
                // Create a new attribute encoder only for the first attribute.
                AddAttributesEncoder(new KdTreeAttributesEncoder(attId));
                return true;
            }

            // Add a new attribute to the attribute encoder.
            AttributesEncoder(0).AddAttributeId(attId);
            return true;


        }

        protected override bool EncodeGeometryData()
        {
            int num_points = PointCloud.NumPoints;
            Buffer.Encode(num_points);
            return true;
        }

    }
}
