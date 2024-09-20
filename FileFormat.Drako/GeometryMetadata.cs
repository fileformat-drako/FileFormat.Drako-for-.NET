﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileFormat.Drako
{
    /// <summary>
    /// Draco metadata
    /// </summary>
    public class Metadata
    {
        /// <summary>
        /// Entries of the metadata
        /// </summary>
        public Dictionary<string, byte[]> Entries = new Dictionary<string, byte[]>();
        /// <summary>
        /// Named sub metadata
        /// </summary>
        public Dictionary<string, Metadata> SubMetadata = new Dictionary<string, Metadata>();
    }
    /// <summary>
    /// Metadata for geometries.
    /// </summary>
    public class GeometryMetadata : Metadata
    {
        /// <summary>
        /// Meta data for attributes.
        /// </summary>
        public Dictionary<int, Metadata> AttributeMetadata = new Dictionary<int, Metadata>();
    }
}
