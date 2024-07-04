using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FileFormat.Drako.Compression;
using FileFormat.Drako.Utils;

namespace FileFormat.Drako.Decoder
{

    /// <summary>
    /// A base class for decoding attribute values encoded by the
    /// SequentialAttributeEncoder.
    /// </summary>
    class SequentialAttributeDecoder
    {
        protected PointCloudDecoder decoder;
        protected PointAttribute attribute;
        PointAttribute portableAttribute;
        private int attributeId = -1;

        public virtual bool Initialize(PointCloudDecoder decoder, int attributeId)
        {
            this.decoder = decoder;
            this.attribute = decoder.PointCloud.Attribute(attributeId);
            this.attributeId = attributeId;
            return true;
        }

        /// <summary>
        /// Intialization for a specific attribute. This can be used mostly for
        /// standalone decoding of an attribute without an PointCloudDecoder.
        /// </summary>
        public virtual bool InitializeStandalone(PointAttribute attribute)
        {
            this.attribute = attribute;
            this.attributeId = -1;
            return true;
        }

        public virtual bool Decode(int[] pointIds, DecoderBuffer inBuffer)
        {
            attribute.Reset(pointIds.Length);
            if (!DecodeValues(pointIds, inBuffer))
                return DracoUtils.Failed();
            return true;
        }

        public PointAttribute Attribute
        {
            get { return attribute; }
        }

        public int AttributeId
        {
            get { return attributeId; }
        }

        public PointCloudDecoder Decoder
        {
            get { return decoder; }
        }

        /// <summary>
        /// Should be used to initialize newly created prediction scheme.
        /// Returns false when the initialization failed (in which case the scheme
        /// cannot be used).
        /// </summary>
        protected virtual bool InitPredictionScheme(PredictionScheme ps)
        {
            for (int i = 0; i < ps.NumParentAttributes; ++i)
            {
                int attId = decoder.PointCloud.GetNamedAttributeId(ps.GetParentAttributeType(i));
                if (attId == -1)
                    return DracoUtils.Failed(); // Requested attribute does not exist.
                PointAttribute parentAttribute;
                if (decoder.BitstreamVersion < 20)
                {
                    parentAttribute = decoder.PointCloud.Attribute(attId);
                }
                else
                {
                    parentAttribute = decoder.GetPortableAttribute(attId);
                }
                if (parentAttribute == null || !ps.SetParentAttribute(parentAttribute))
                    return DracoUtils.Failed();
            }
            return true;
        }

        /// <summary>
        /// The actual implementation of the attribute decoding. Should be overriden
        /// for specialized decoders.
        /// </summary>
        protected virtual bool DecodeValues(int[] pointIds, DecoderBuffer inBuffer)
        {
            int numValues = pointIds.Length;
            int entrySize = (int) attribute.ByteStride;
            byte[] valueData = new byte[entrySize];
            int outBytePos = 0;
            // Decode raw attribute values in their original format.
            for (int i = 0; i < numValues; ++i)
            {
                if (!inBuffer.Decode(valueData, entrySize))
                    return DracoUtils.Failed();
                attribute.Buffer.Write(outBytePos, valueData, entrySize);
                outBytePos += entrySize;
            }
            return true;
        }

        public virtual bool DecodePortableAttribute(int[] pointIds, DecoderBuffer in_buffer)
        {
            if (attribute.ComponentsCount <= 0 || !attribute.Reset(pointIds.Length))
                return DracoUtils.Failed();
            if (!DecodeValues(pointIds, in_buffer))
                return DracoUtils.Failed();
            return true;
        }

        public virtual bool DecodeDataNeededByPortableTransform(int[] pointIds, DecoderBuffer in_buffer)
        {
            // Default implementation does not apply any transform.
            return true;
        }

        public virtual bool TransformAttributeToOriginalFormat(int[] pointIds)
        {
            // Default implementation does not apply any transform.
            return true;
        }

        public PointAttribute PortableAttribute
        {
            get
            {
                // If needed, copy point to attribute value index mapping from the final
                // attribute to the portable attribute.
                if (!attribute.IdentityMapping && portableAttribute != null &&
                    portableAttribute.IdentityMapping)
                {
                    int indiceMapSize = attribute.IndicesMap.Length;
                    portableAttribute.SetExplicitMapping(indiceMapSize);
                    for (int i = 0; i < indiceMapSize; ++i)
                    {
                        portableAttribute.SetPointMapEntry(i, attribute.MappedIndex(i));
                    }
                }

                return portableAttribute;
            }
            set
            {
                portableAttribute = value;
            }
        }


    }
}
