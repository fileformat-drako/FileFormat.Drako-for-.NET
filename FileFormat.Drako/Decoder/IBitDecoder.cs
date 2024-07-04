﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Openize.Drako.Decoder
{
    interface IBitDecoder
    {
        /// <summary>
        /// Sets |sourceBuffer| as the buffer to decode bits from.
        /// Returns false when the data is invalid.
        /// </summary>
        bool StartDecoding(DecoderBuffer sourceBuffer);

        uint DecodeLeastSignificantBits32(int nbits);
        bool DecodeNextBit();
        void EndDecoding();
    }
}
