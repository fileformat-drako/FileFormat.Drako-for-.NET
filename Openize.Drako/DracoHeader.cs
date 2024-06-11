using Openize.Draco.Decoder;
using Openize.Draco.Utils;

namespace Openize.Draco
{
    class DracoHeader
    {
        public const int MetadataFlagMask = 0x8000;
        private static readonly byte[] HEADER = { (byte)'D', (byte)'R', (byte)'A', (byte)'C', (byte)'O' };
        private byte major;
        private byte minor;
        public int version;
        public ushort flags;

        public EncodedGeometryType encoderType;
        public DracoEncodingMethod method;

        public static DracoHeader Parse(DecoderBuffer buffer)
        {
            byte[] header = new byte[5];
            if (!buffer.Decode(header))
                return null;
            if (DracoUtils.Compare(header, HEADER, HEADER.Length) != 0)
                return null;
            DracoHeader ret = new DracoHeader();
            ret.encoderType = EncodedGeometryType.Invalid;
            ret.method = DracoEncodingMethod.Sequential;
            if (!buffer.Decode(out ret.major))
                return null;
            if (!buffer.Decode(out ret.minor))
                return null;
            ret.version = ret.major * 10 + ret.minor;
            byte t;
            if (!buffer.Decode(out t))
                return null;
            ret.encoderType = (EncodedGeometryType)t;
            if (!buffer.Decode(out t))
                return null;
            ret.method = (DracoEncodingMethod)t;
            if (!buffer.Decode(out ret.flags))
                return null;
            return ret;
        }
    }
}
