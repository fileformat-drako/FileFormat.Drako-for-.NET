using Aspose.ThreeD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileFormat.Drako.Decoder
{
    class MetadataDecoder
    {
        class MetadataTuple
        {
            public Metadata parent;
            public Metadata decoded;
            public int level;
            public MetadataTuple(Metadata parent, Metadata decoded, int level)
            {
                this.parent = parent;
                this.decoded = decoded;
                this.level = level;
            }
        }

        const int kMaxSubmetadataLevel = 1000;

        public GeometryMetadata Decode(DecoderBuffer buffer)
        {
            uint numAttrMetadata = 0;
            if (!Decoding.DecodeVarint(out numAttrMetadata, buffer))
                return null;
            var metadata = new GeometryMetadata();
            for(int i = 0; i < numAttrMetadata; i++)
            {
                uint attUniqueId;
                if (!Decoding.DecodeVarint(out attUniqueId, buffer))
                    return null;
                var attMetadata = new Metadata();
                if (!DecodeMetadata(buffer, attMetadata))
                    return null;
                metadata.AttributeMetadata[(int)attUniqueId] = attMetadata;
            }

            DecodeMetadata(buffer, metadata);
            return metadata;
        }
        private bool DecodeMetadata(DecoderBuffer buffer, Metadata metadata)
        {
            var stack = new List<MetadataTuple>();
            stack.Add(new MetadataTuple(null, metadata, 0));
            while(stack.Count > 0)
            {
                var mp = stack[stack.Count - 1];
                stack.RemoveAt(stack.Count - 1);
                metadata = mp.decoded;
                if(mp.parent != null)
                {
                    if (mp.level > kMaxSubmetadataLevel)
                        return false;
                    var subMetadataName = DecodeName(buffer);
                    if (subMetadataName == null)
                        return false;
                    var subMetadata = new Metadata();
                    metadata = subMetadata;
                    mp.parent.SubMetadata[subMetadataName] = subMetadata;
                }
                if (metadata == null)
                    return false;
                uint numEntries = 0;
                if (!Decoding.DecodeVarint(out numEntries, buffer))
                    return false;
                for(int i = 0; i < numEntries; i++)
                {
                    if (!DecodeEntry(buffer, metadata))
                        return false;
                }
                uint numSubMetadata = 0;
                if (!Decoding.DecodeVarint(out numSubMetadata, buffer))
                    return false;
                if (numSubMetadata > buffer.RemainingSize)
                    return false;
                for(var i = 0; i < numSubMetadata; i++)
                {
                    stack.Add(new MetadataTuple(metadata, null, mp.parent != null ? mp.level + 1 : mp.level));
                }
            }

            return true;
        }
        private bool DecodeEntry(DecoderBuffer buffer, Metadata metadata)
        {
            var entryName = DecodeName(buffer);
            if (entryName == null)
                return false;
            uint dataSize = 0;
            if (!Decoding.DecodeVarint(out dataSize, buffer))
                return false;
            if (dataSize == 0 || dataSize > buffer.RemainingSize)
                return false;
            var entryValue = new byte[dataSize];
            if (!buffer.Decode(entryValue, (int)dataSize))
                return false;
            metadata.Entries[entryName] = entryValue;
            return true;
        }
        private string DecodeName(DecoderBuffer buffer)
        {
            uint nameLen = 0;
            if (!Decoding.DecodeVarint(out nameLen, buffer))
                return null;
            var bytes = new byte[nameLen];
            if (!buffer.Decode(bytes, bytes.Length))
                return null;
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
