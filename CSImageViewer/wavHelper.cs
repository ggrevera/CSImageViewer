/**
    \file   wavHelper.cs
    \brief  Contains wavHelper class definition.
    \author George J. Grevera, Ph.D., ggrevera@sju.edu

    Copyright (C) 2010, George J. Grevera

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307
    USA or from http://www.gnu.org/licenses/gpl.txt.

    This General Public License does not permit incorporating this
    code into proprietary programs.  (So a hypothetical company such
    as GH (Generally Hectic) should NOT incorporate this code into
    their proprietary programs.)
 */
using System;
using System.IO;
using System.Diagnostics;
//----------------------------------------------------------------------
#pragma warning disable IDE1006
#pragma warning disable CS0219

namespace CSImageViewer {

    /** \brief  This class contains methods that read and write wav 
     * (sound/audio) files.
     */
    class wavHelper {

        /**
         * based on https://gist.github.com/yomakkkk/2290864
         * and http://soundfile.sapp.org/doc/WaveFormat/
         * and http://www.lightlink.com/tjweber/StripWav/WAVE.html
         * and http://www-mmsp.ece.mcgill.ca/Documents/AudioFormats/WAVE/WAVE.html
         */
        public static int[] read ( String fname, out int w, out int h, out int min, out int max ) {
            //init additional return values
            w = h = min = max = 0;

            //open the input data file
            FileStream   fs = new FileStream( fname, FileMode.Open, FileAccess.Read );
            BinaryReader br = new BinaryReader( fs );

            //RIFF tagged file formmat

            //read the header
            char[] riffID = br.ReadChars( 4 );  //'RIFF' for little endian (intel), or 'RIFX' for big endian (motorola)
            uint   size   = br.ReadUInt32();    //4 + (8 + subchunk1size) + (8 + subchunk2size)
            char[] wavID  = br.ReadChars( 4 );  //'WAVE'

            Console.WriteLine( riffID );
            Console.WriteLine( size );
            Console.WriteLine( wavID );

            bool   fmtFound = false;
            ushort formatTag = 0;         //1=PCM (linear quantization);3=IEEE float; other values indicate compression
            ushort channels = 0;          //1=mono (left only), 2=stereo (l1,r1,l2,r2,...)
            uint   samplesPerSec;         //sample rate
            uint   avgBytesPerSec;        //for buffer estimation
            ushort blockAlign;            //data block size
            ushort bitsPerSample = 0;     //typically 8 or 16 (but could be 24 or 32)

            bool   factFound = false;
            UInt32 dwSampleLength;

            bool   dataFound = false;
            uint   dataSize = 0;          //size of data in bytes
            byte[] data = null;

            bool   infoFound = false, unknownFound = false, peakFound = false, listFound = false, id3Found = false;

            while (fs.Position < fs.Length) {
                char[] id = br.ReadChars( 4 );
                uint   sz = br.ReadUInt32();    //chunk size
                if (id[ 0 ] == 'f' && id[ 1 ] == 'm' && id[ 2 ] == 't' && id[ 3 ] == ' ') {         // 'fmt ' (wave format chunk)
                    fmtFound = true;
                    formatTag = br.ReadUInt16();    //1=PCM (linear quantization);3=IEEE float; other values indicate compression
                    channels = br.ReadUInt16();    //1=mono (left only), 2=stereo (l1,r1,l2,r2,...)
                    samplesPerSec = br.ReadUInt32();    //sample rate
                    avgBytesPerSec = br.ReadUInt32();    //for buffer estimation
                    blockAlign = br.ReadUInt16();    //data block size
                    bitsPerSample = br.ReadUInt16();    //typically 8 or 16 (but could be 24 or 32)
                    //sz should/may be 16, 18, or 40; handle other possible (unlikely) extra data in chunk.
                    if ((sz - 16) > 0) {
                        byte[] skip = br.ReadBytes( (int) sz );
                    }
                } else if (id[ 0 ] == 'f' && id[ 1 ] == 'a' && id[ 2 ] == 'c' && id[ 3 ] == 't') {  // 'fact'
                    factFound = true;
                    dwSampleLength = br.ReadUInt32();    //number of samples (per channel)
                } else if (id[ 0 ] == 'd' && id[ 1 ] == 'a' && id[ 2 ] == 't' && id[ 3 ] == 'a') {  // 'data'
                    dataFound = true;
                    dataSize = sz;
                    data = br.ReadBytes( (int)sz );
                } else if (id[ 0 ] == 'I' && id[ 1 ] == 'N' && id[ 2 ] == 'F' && id[ 3 ] == 'O') {  // 'INFO'
                    infoFound = true;
                    byte[] skip = br.ReadBytes( (int)sz );
                } else if (id[ 0 ] == 'P' && id[ 1 ] == 'E' && id[ 2 ] == 'A' && id[ 3 ] == 'K') {  // 'PEAK'
                    peakFound = true;
                    byte[] skip = br.ReadBytes( (int)sz );
                } else if (id[ 0 ] == 'L' && id[ 1 ] == 'I' && id[ 2 ] == 'S' && id[ 3 ] == 'T') {  // 'LIST'
                    listFound = true;
                    byte[] skip = br.ReadBytes( (int)sz );
                } else if (id[ 0 ] == 'i' && id[ 1 ] == 'd' && id[ 2 ] == '3' && id[ 3 ] == ' ') {  // 'id3 '
                    id3Found = true;
                    byte[] skip = br.ReadBytes( (int)sz );
                } else {
                    unknownFound = true;
                    byte[] skip = br.ReadBytes( (int)sz );
                    Console.WriteLine( id );
                }
            }

            //at this point, we need to have discovered at least an 'fmt ' chunk and a 'data' chunk.
            Debug.Assert( fmtFound && dataFound );
            w = channels;
            int bytesPerSample = bitsPerSample / 8;
            Debug.Assert( 1 <= bytesPerSample && bytesPerSample <= 4 );
            h = (int)(dataSize / bytesPerSample / channels);

            /** @todo: george - needs work. 8-bit ints unsigned? 16, 24, 32-bit ints signed? 32-bit floats? */
            /** @todo: george - need to determine min and max */
            int[] result = new int[ w * h ];
            if (bytesPerSample == 1) {
                min = max = data[ 0 ];
                for (int i = 0; i < w * h; i++) {
                    result[ i ] = data[ i ];
                    if (result[ i ] < min)
                        min = result[ i ];
                    if (result[ i ] > max)
                        max = result[ i ];
                }
            } else if (bytesPerSample == 2) {
                min = max = data[ 0 ] + (data[ 1 ] << 8);
                for (int i = 0, j = 0; i < w * h; i++, j += 2) {
                    result[ i ] = data[ j ] + (data[ j + 1 ] << 8);
                    if (result[ i ] < min)
                        min = result[ i ];
                    if (result[ i ] > max)
                        max = result[ i ];
                }
            } else if (bytesPerSample == 3) {
                min = max = data[ 0 ] + (data[ 1 ] << 8) + (data[ 2 ] << 16);
                for (int i = 0, j = 0; i < w * h; i++, j += 3) {
                    result[ i ] = data[ j ] + (data[ j + 1 ] << 8) + (data[ j + 2 ] << 16);
                    if (result[ i ] < min)
                        min = result[ i ];
                    if (result[ i ] > max)
                        max = result[ i ];
                }
            } else if (bytesPerSample == 4) {
                if (formatTag == 1) {
                    min = max = data[ 0 ] + (data[ 1 ] << 8) + (data[ 2 ] << 16) + (data[ 3 ] << 24);
                    for (int i = 0, j = 0; i < w * h; i++, j += 4) {
                        result[ i ] = data[ j ] + (data[ j + 1 ] << 8) + (data[ j + 2 ] << 16) + (data[ j + 3 ] << 24);
                        if (result[ i ] < min)
                            min = result[ i ];
                        if (result[ i ] > max)
                            max = result[ i ];
                    }
                } else if (formatTag == 3) {
                    Debug.Assert( dataFound );
                    Debug.Assert( fs.CanSeek );
                    /**
                     * @todo: george - need to convert floats to ints; tricky.
                     * need to load floats, determine float min and max, then scale using min and max (but of what intdata type?)
                     */
                    float[] buff = new float[ w*h ];

                    //a better (safe) way to do the code below would be to replace it with BitConverter.ToSingle(Byte[], Int32)

                    unsafe {
                        int x = data[0] + (data[1]<<8) + (data[2]<<16) + (data[3]<<24);
                        float* fptr = (float*) &x;
                        float f = *fptr;
                        float fMin = f;
                        float fMax = f;
                        for (int i = 1, j = 4; i < w * h; i++, j += 4) {
                            x = data[ j ] + (data[ j + 1 ] << 8) + (data[ j + 2 ] << 16) + (data[ j + 3 ] << 24);
                            fptr = (float*)&x;
                            f = *fptr;
                            buff[ i ] = f;

                            if (f < fMin)
                                fMin = f;
                            if (f > fMax)
                                fMax = f;
                        }
                        //now scale
                        //should be -1.0 <= fMin <= fMax <= +1.0
                        Console.WriteLine( fMin );
                        Console.WriteLine( fMax );
                    }
                    Debug.Assert( false );
                } else {
                    Debug.Assert( false );
                }
            } else {
                Debug.Assert( false );
            }

            br.Close();
            fs.Close();

            return result;
        }
        //-------------------------------------------------------------------
        /** @todo write/save audio wav file data. */
        public static void write ( String fname, int[] buff, int w, int h, int min, int max ) {

        }

    }

}
