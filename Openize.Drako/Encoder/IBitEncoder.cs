﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Openize.Draco.Encoder
{
    interface IBitEncoder
    {
        void StartEncoding();

        void EncodeBit(bool bit);
        void EncodeLeastSignificantBits32(int nbits, uint value);

        void EndEncoding(EncoderBuffer targetBuffer);
        void Clear();
    }
}
