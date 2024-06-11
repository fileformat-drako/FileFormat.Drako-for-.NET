using Openize.Draco.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Openize.Draco.Decoder
{
    class DracoLoadOptions
    {
        public bool skipAttributeTransform = false;
        public bool flipTextureCoordinateV = true;

    }
    abstract class PointCloudDecoder
    {
        protected DecoderBuffer buffer;
        protected DracoPointCloud pointCloud;  
        protected AttributesDecoder[] attributesDecoders = new AttributesDecoder[0];
        protected EncodedGeometryType geometryType;
        internal DracoLoadOptions options = new DracoLoadOptions();


  // Map between attribute id and decoder id.
        private int[] attributeToDecoderMap;
        public int BitstreamVersion { get; set; }

        protected PointCloudDecoder(EncodedGeometryType geometryType)
        {
            this.geometryType = geometryType;
        }

        public DracoPointCloud PointCloud
        {
            get { return pointCloud; }
        }

        public EncodedGeometryType GeometryType
        {
            get { return geometryType; }
        }

        public virtual bool Decode(DracoHeader header, DecoderBuffer buffer, DracoPointCloud result, bool decodeData)
        {
            this.buffer = buffer;
            this.pointCloud = result;
            BitstreamVersion = header.version;
            if (header.version >= 13 && (header.flags & DracoHeader.MetadataFlagMask) == DracoHeader.MetadataFlagMask)
            {
                if (!DecodeMetadata())
                    return DracoUtils.Failed();
            }
            if (!InitializeDecoder())
                return DracoUtils.Failed();
            if (!DecodeGeometryData())
                return DracoUtils.Failed();
            if (!DecodePointAttributes(decodeData))
                return DracoUtils.Failed();
            return true;
        }

        private bool DecodeMetadata()
        {
            MetadataDecoder decoder = new MetadataDecoder();
            var metadata = decoder.Decode(buffer);
            pointCloud.Metadatas.Add(metadata);
            return DracoUtils.Failed();
        }

        public bool SetAttributesDecoder(int attDecoderId, AttributesDecoder decoder)
        {
            if (attDecoderId < 0)
                return false;
            if (attDecoderId >= attributesDecoders.Length)
            {
                Array.Resize(ref attributesDecoders, attDecoderId + 1);
            }
            attributesDecoders[attDecoderId] = decoder;
            return true;
        }

        public AttributesDecoder[] AttributesDecoders
        {
            get { return attributesDecoders;}
            
        }

        protected virtual bool InitializeDecoder()
        {
            return true;
        }

        protected virtual bool DecodeGeometryData()
        {
            return true;
        }

        protected bool DecodePointAttributes(bool decodeAttributeData)
        {
            byte numAttributesDecoders = 0;
            if (!buffer.Decode(out numAttributesDecoders))
                return DracoUtils.Failed();
            //create attributes decoders
            for (int i = 0; i < numAttributesDecoders; i++)
            {
                if (!CreateAttributesDecoder(i))
                    return DracoUtils.Failed();
            }
            //initialize all decoders
            foreach(AttributesDecoder dec in attributesDecoders)
            {
                if (!dec.Initialize(this, pointCloud))
                    return DracoUtils.Failed();
            }
            //decode data
            foreach (AttributesDecoder dec in attributesDecoders)
            {
                if (!dec.DecodeAttributesDecoderData(buffer))
                    return DracoUtils.Failed();
            }

  // Create map between attribute and decoder ids.
            int maxAttrId = -1;

            for (int i = 0; i < attributesDecoders.Length; ++i)
            {
                int numAttributes = attributesDecoders[i].NumAttributes;
                for (int j = 0; j < numAttributes; ++j)
                {
                    int attId = attributesDecoders[i].GetAttributeId(j);
                    maxAttrId = Math.Max(attId, maxAttrId);
                }
            }
            attributeToDecoderMap = new int[maxAttrId + 1];
            for (int i = 0; i < attributesDecoders.Length; ++i)
            {
                int numAttributes = attributesDecoders[i].NumAttributes;
                for (int j = 0; j < numAttributes; ++j)
                {
                    int attId = attributesDecoders[i].GetAttributeId(j);
                    attributeToDecoderMap[attId] = i;
                }
            }

            //decode attributes
            if (decodeAttributeData)
            {
                if (!DecodeAllAttributes())
                    return DracoUtils.Failed();
            }
                if (!OnAttributesDecoded())
                    return DracoUtils.Failed();
            return true;
        }

        protected abstract bool CreateAttributesDecoder(int attrDecoderId);
        

        protected virtual bool OnAttributesDecoded()
        {
            return true;
        }
        protected virtual bool DecodeAllAttributes()
        {
            foreach (AttributesDecoder dec in attributesDecoders)
            {
                if (!dec.DecodeAttributes(buffer))
                    return DracoUtils.Failed();
            }
            return true;
        }

        public DecoderBuffer Buffer
        {
            get { return buffer; }
            set { buffer = value; }
        }

        public PointAttribute GetPortableAttribute(int attId)
        {
            if (attId < 0 || attId >= pointCloud.NumAttributes)
                return null;
            int parentAttDecoderId = attributeToDecoderMap[attId];
            return attributesDecoders[parentAttDecoderId].GetPortableAttribute(attId);
        }
    }
}
