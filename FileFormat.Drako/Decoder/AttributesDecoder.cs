using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FileFormat.Drako.Utils;

namespace FileFormat.Drako.Decoder
{


    /// <summary>
    /// Base class for decoding one or more attributes that were encoded with a
    /// matching AttributesEncoder. This base class provides only the basic interface
    /// that is used by the PointCloudDecoder. The actual encoding must be
    /// implemented in derived classes using the DecodeAttributes() method.
    /// </summary>
    abstract class AttributesDecoder
    {
        private int[] pointAttributeIds = new int[0];

        // Map between point attribute id and the local id (i.e., the inverse of the
        // |point_attribute_ids_|.
        private IntList point_attribute_to_local_id_map_ = new IntList();
        private PointCloudDecoder pointCloudDecoder;
        private DracoPointCloud pointCloud;


        public virtual bool Initialize(PointCloudDecoder decoder, DracoPointCloud pointCloud)
        {
            this.pointCloud = pointCloud;
            this.pointCloudDecoder = decoder;
            return true;
        }

        public virtual bool DecodeAttributesDecoderData(DecoderBuffer inBuffer)
        {
            // Decode and create attributes.
            int numAttributes;
            if (pointCloudDecoder.BitstreamVersion < 20)
            {
                if (!inBuffer.Decode(out numAttributes))
                    return DracoUtils.Failed();
            }
            else
            {
                uint n;
                if (!Decoding.DecodeVarint(out n, inBuffer))
                    return DracoUtils.Failed();
                numAttributes = (int) n;
            }

            if (numAttributes <= 0)
                return DracoUtils.Failed();

            pointAttributeIds = new int[numAttributes];
            DracoPointCloud pc = pointCloud;
            int version = pointCloudDecoder.BitstreamVersion;
            for (int i = 0; i < numAttributes; ++i)
            {
                // Decode attribute descriptor data.
                byte attType, dataType, componentsCount, normalized;
                if (!inBuffer.Decode(out attType))
                    return DracoUtils.Failed();
                if (!inBuffer.Decode(out dataType))
                    return DracoUtils.Failed();
                if (!inBuffer.Decode(out componentsCount))
                    return DracoUtils.Failed();
                if (!inBuffer.Decode(out normalized))
                    return DracoUtils.Failed();
                DataType dracoDt = (DataType) (dataType);

                // Add the attribute to the point cloud
                PointAttribute ga = new PointAttribute();
                ga.AttributeType = (AttributeType) attType;
                ga.ComponentsCount = componentsCount;
                ga.DataType = dracoDt;
                ga.Normalized = normalized > 0;
                ga.ByteStride = DracoUtils.DataTypeLength(dracoDt) * componentsCount;

                ushort customId;
                if (version < 13)
                {
                    if (!inBuffer.Decode(out customId))
                        return DracoUtils.Failed();
                }
                else
                {
                    Decoding.DecodeVarint(out customId, inBuffer);
                }

                ga.UniqueId = customId;


                int attId = pc.AddAttribute(ga);
                pointAttributeIds[i] = attId;

                // Update the inverse map.
                if (attId >= point_attribute_to_local_id_map_.Count)
                    point_attribute_to_local_id_map_.Resize(attId + 1, -1);
                point_attribute_to_local_id_map_[attId] = i;
            }

            return true;
        }

        public virtual bool DecodeAttributes(DecoderBuffer buffer)
        {
            if (!DecodePortableAttributes(buffer))
                return DracoUtils.Failed();
            if (!DecodeDataNeededByPortableTransforms(buffer))
                return DracoUtils.Failed();
            if (!TransformAttributesToOriginalFormat())
                return DracoUtils.Failed();
            return true;
        }

        protected abstract bool DecodePortableAttributes(DecoderBuffer buffer);

        protected virtual bool DecodeDataNeededByPortableTransforms(DecoderBuffer buffer)
        {
            return true;
        }

        protected virtual bool TransformAttributesToOriginalFormat()
        {
            return true;
        }

        public int GetAttributeId(int i)
        {
            return pointAttributeIds[i];
        }

        public int NumAttributes
        {
            get { return pointAttributeIds.Length; }
        }

        public PointCloudDecoder Decoder
        {
            get { return pointCloudDecoder; }
        }

        public abstract PointAttribute GetPortableAttribute(int attId);

        protected int GetLocalIdForPointAttribute(int point_attribute_id)
        {
            int id_map_size = point_attribute_to_local_id_map_.Count;
            if (point_attribute_id >= id_map_size)
                return -1;
            return point_attribute_to_local_id_map_[point_attribute_id];
        }
    }
}
