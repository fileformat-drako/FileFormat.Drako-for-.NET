using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Openize.Draco.Encoder;
using Openize.Draco.Utils;

namespace Openize.Draco
{
    class AttributeOctahedronTransform : AttributeTransform
    {
        private int quantizationBits;

        public AttributeOctahedronTransform(int quantizationBits)
        {
            this.quantizationBits = quantizationBits;
        }

        public override AttributeTransformType Type()
        {
            return AttributeTransformType.OctahedronTransform;
        }

        public override bool InitFromAttribute(PointAttribute attribute)
        {

            AttributeTransformData transform_data = attribute.AttributeTransformData;
            if (transform_data == null || transform_data.transformType != AttributeTransformType.OctahedronTransform)
                return DracoUtils.Failed(); // Wrong transform type.
            quantizationBits = transform_data.GetInt(0);
            return true;
        }

        public override void CopyToAttributeTransformData(AttributeTransformData outData)
        {
            outData.transformType = AttributeTransformType.OctahedronTransform;
            outData.AppendValue(quantizationBits);
        }

        public bool EncodeParameters(EncoderBuffer encoder_buffer)
        {
            if (quantizationBits != -1)
            {
                encoder_buffer.Encode((byte)quantizationBits);
                return true;
            }
            return DracoUtils.Failed();
        }

        public PointAttribute GeneratePortableAttribute(PointAttribute attribute, int[] point_ids, int num_points)
        {

            // Allocate portable attribute.
            int num_entries = point_ids.Length;
            PointAttribute portable_attribute = InitPortableAttribute(num_entries, 2, num_points, attribute, true);

            //IntArray portable_attribute_data = IntArray.Wrap(portable_attribute.Buffer.GetBuffer(), 0, num_entries * 2 * 4);
            //IntArray portable_attribute_data2 = IntArray.Wrap(portable_attribute.Buffer.GetBuffer(), 0, num_entries * 4);
            Span<byte> buffer = new Span<byte>(portable_attribute.Buffer.GetBuffer(), 0, num_entries * 2 * 4);
            Span<int> portable_attribute_data = MemoryMarshal.Cast<byte, int>(buffer);


            // Quantize all values in the order given by point_ids into portable
            // attribute.
#if NET46
            Span<float> att_val = new float[3];
#else
            Span<float> att_val = stackalloc float[3];
#endif
            int dst_index = 0;
            OctahedronToolBox converter = new OctahedronToolBox();
            if (!converter.SetQuantizationBits(quantizationBits))
                return null;
            for (int i = 0; i < point_ids.Length; ++i)
            {
                int att_val_id = attribute.MappedIndex(point_ids[i]);
                attribute.GetValue(att_val_id, att_val);
                // Encode the vector into a s and t octahedral coordinates.
                int s, t;
                converter.FloatVectorToQuantizedOctahedralCoords(att_val, out s, out t);
                portable_attribute_data[dst_index++] = s;
                portable_attribute_data[dst_index++] = t;
            }
            return portable_attribute;
        }
    }
}
