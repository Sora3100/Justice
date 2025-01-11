using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Justice
{
    public struct EAFHeader
    {
        public byte[] eafMagic; // EAF magic (should always be "#EAF")
        public int eafUnk1; // EAF version
        public Int64 eafSize; // Size of the entire EAF
        public int eafFileNum; // Number of files contained in the EAF
        public int eafUnk2; // Unknown (always 1?)
        //public byte[] eafReserved; // 0x48 bytes of reserved space / padding (actually 0x58?)
    }

    public struct EAFFileInfo
    {
        public byte[] beforeNamePadding;
        public string eafFileName;
        public Int64 eafFileOffset;
        public Int64 eafFileSize;
        //public Int64 eafFileOffset; // Stored File Offset
        //public Int64 eafFileSize; // Stored File Size
        //public Int64 eafFileReserved1; // Reserved (or padding?) 0x8 bytes
        //public Int64 eafFileReserved2; // Reserved (or padding?) 0x8 bytes
        //public string eafFileName; // File name
    }
}
