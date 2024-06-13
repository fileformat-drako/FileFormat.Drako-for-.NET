using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Openize.Draco.Decoder;
using Openize.Draco.Utils;

namespace Openize.Draco.Encoder
{

    /// <summary>
    /// Attribute encoder that quantizes floating point attribute values. The
    /// quantized values can be optionally compressed using an entropy coding.
    /// </summary>
    class SequentialQuantizationAttributeEncoder : SequentialIntegerAttributeEncoder
    {
        private AttributeQuantizationTransform attribute_quantization_transform_ = new AttributeQuantizationTransform();

        public override SequentialAttributeEncoderType GetUniqueId()
        {
            return SequentialAttributeEncoderType.Quantization;
        }

        public override bool IsLossyEncoder()
        {
            return true;
        }

        public override bool Initialize(
            PointCloudEncoder encoder, int attributeId)
        {
            if (!base.Initialize(encoder, attributeId))
                return false;
            // This encoder currently works only for floating point attributes.
            PointAttribute attribute = Encoder.PointCloud.Attribute(attributeId);
            if (attribute.DataType != DataType.FLOAT32)
                return false;

            // Initialize AttributeQuantizationTransform.
            int quantization_bits = encoder.Options.GetQuantizationBits(attribute);
            if (quantization_bits < 1)
                return false;
            /*
            if (encoder.Options.IsAttributeOptionSet(attribute_id,
                    "quantization_origin") &&
                encoder.options().IsAttributeOptionSet(attribute_id,
                    "quantization_range"))
            {
                // Quantization settings are explicitly specified in the provided options.
                std::vector<float> quantization_origin(attribute.num_components());
                encoder.options().GetAttributeVector(attribute_id, "quantization_origin",
                    attribute.num_components(),
                    &quantization_origin[0]);
                const float range = encoder.options().GetAttributeFloat(
                    attribute_id, "quantization_range", 1.f);
                attribute_quantization_transform_.SetParameters(
                    quantization_bits, quantization_origin.data(),
                    attribute.ComponentsCount, range);
            }
            else
            */
            {
                // Compute quantization settings from the attribute values.
                attribute_quantization_transform_.ComputeParameters(attribute,
                    quantization_bits);
            }

            return true;
        }

        public override bool EncodeDataNeededByPortableTransform(EncoderBuffer out_buffer)
        {
            return attribute_quantization_transform_.EncodeParameters(out_buffer);
        }

        protected override bool PrepareValues(int[] pointIds, int numPoints)
        {
            var portable_attribute = attribute_quantization_transform_.InitTransformedAttribute(
                    attribute, pointIds.Length);
            if (!attribute_quantization_transform_.TransformAttribute(
                    attribute, pointIds, portable_attribute))
            {
                return false;
            }
            this.portableAttribute = portable_attribute;

            return true;
        }
    }
}
