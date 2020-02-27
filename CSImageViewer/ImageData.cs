/**
    \file   ImageData.cs
    \brief  Contains ImageData class definition.
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
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
//------------------------------------------------------------------------
#pragma warning disable IDE1006

namespace CSImageViewer {

    /** \brief  class containing the actual pixel data values (note that this
     *  class is abstract)
     *
     *  This class contains the actual image pixel data.
     *  Note that this class is abstract.
     */
    abstract public class ImageData {

        protected bool    mIsColor;        ///< true if color (rgb); false if gray (or audio)
        protected bool    mIsAudio;        ///< true if audio; false if color or ordinary gray
        protected bool    mImageModified;  ///< true if image has been modified
        protected int     mW;              ///< image width
        protected int     mH;              ///< image height
        protected int     mMin;            ///< overall min image pixel value
        protected int     mMax;            ///< overall max image pixel value
        protected String  mFname;          ///< (optional) file name
        protected int     mRate;           ///< samples per sec (audio only)

        /** \brief  Actual original (unmodified) unpacked (1 component per
         *          array entry) image data.
         *
         *  If the image data are gray, each entry in this array represents a
         *  gray pixel value.  So mImageData[0] is the first pixel's gray 
         *  value, mImageData[1] is the second pixel's gray value, and so 
         *  on.  Each value may be 8 bits or 16 bits.  16 bits allows for
         *  values in the range [0..65535].
         *  <br> <br>
         *  If the image data are color, triples of entries (i.e., 3) represent 
         *  each color rgb value.  So each value is in [0..255] for 24-bit
         *  color where each component is 8 bits.  So mImageData[0] is the 
         *  first pixel's red value, mImageData[1] is the first pixel's green 
         *  value, mImageData[2] is the first pixel's blue value, mImageData[3] 
         *  is the second pixel's red value, and so on.
         */
        protected int[]  mOriginalData;

        /** \brief  Possibly modified copy of mOriginal data that can be used
         *  for contrast changes, edge detection, filtering, etc.
         */
        public int[]  mDisplayData;

        /** \brief image drawn on screen */
        public Bitmap  mDisplayImage;
        //----------------------------------------------------------------
        /** \brief  Given the name of an input image file, this method
         *  determines the type of image and then invokes the appropriate
         *  constructor.
         *
         *  Note that this static function returns the appropriate subclass of
         *  ImageData depending upon the type of image data (color or gray).
         *
         *  \param    fileName  name of input image file
         *  \returns  an instance of the ImageData class (actually the correct
         *            subclass of ImageData because ImageData is abstract)
         */
        static public ImageData load ( String fileName ) {
            Timer      t  = new Timer();
            ImageData  id = null;
            String     up = fileName.ToUpper();

            if (up.EndsWith( ".PPM" ) || up.EndsWith( ".PNM" ) || up.EndsWith( ".PGM" )) {
                //technique (trick? kludge?) to implement pass-by-reference
                int w, h, spp, min, max;
                int[] originalData = pnmHelper.read_pnm_file( fileName, out w, out h, out spp, out min, out max );
                t.print();
                if (max > 255)
                    MessageBox.Show( "Warning:\n\nMax value of " + max + " exceeds limit of 255." );
                if (min < 0)
                    MessageBox.Show( "Warning:\n\nMin value of " + min + " is less than 0." );
                if (spp == 3) {
                    id = new ColorImageData( originalData, w, h );
                    id.mFname = fileName;
                    Console.WriteLine( id );
                    return id;
                }
                if (spp == 1) {
                    id = new GrayImageData( originalData, w, h );
                    id.mFname = fileName;
                    Console.WriteLine( id );
                    return id;
                }
                MessageBox.Show( "Error:\n\n    Cannot read this file!" );
                return null;
            }

            if (up.EndsWith( ".WAV" )) {
                //basically treat audio wave files like 2d gray files (which are probably wide but not very high)
                int w, h, min, max, sampleRate;
                int[] originalData = wavHelper.read( fileName, out w, out h, out min, out max, out sampleRate );
                t.print();
                if (max > 255)
                    MessageBox.Show( "Warning:\n\nMax value of " + max + " exceeds limit of 255." );
                if (min < 0)
                    MessageBox.Show( "Warning:\n\nMin value of " + min + " is less than 0." );
                id = new GrayImageData( originalData, w, h );
                id.mFname   = fileName;
                id.mIsAudio = true;
                id.mRate    = sampleRate;
                Console.WriteLine( id );
                return id;
            }

            //here for image file types such as gif, jpg, bmp, etc. (but not ppm, pnm, pgm, or wav).

            //see http://www.bobpowell.net/lockingbits.htm for description of Bitmap pixel access.
            Bitmap  bm = (Bitmap) Bitmap.FromFile( fileName, false );
            t.print();
            switch (bm.PixelFormat) {
                case PixelFormat.Format1bppIndexed:
                    MessageBox.Show( "Sorry!\n\n Unsupported PixelFormat - Format1bppIndexed." );
                    break;

                case PixelFormat.Format4bppIndexed:
                case PixelFormat.Format8bppIndexed:
                    if (bm.PixelFormat == PixelFormat.Format4bppIndexed)
                        MessageBox.Show( "Caution!\n\n This hasn't been tested on 4 bpp indexed images." );
                    //this is a bit tricky.  each scalar value in the image is used as an index
                    // into a table (Palette).  Palette entries are rgb but if r==g==b, then this is
                    // actually gray data.  so let's first deteremine if this is color or gray.
                    bool isGray = true;
                    for (int i = 0; i < bm.Palette.Entries.Length; i++) {
                        Color c = bm.Palette.Entries[ i ];
                        if (c.R == c.G && c.G == c.B) continue;
                        isGray = false;
                        break;
                    }
                    int bpp = 0;
                    if (bm.PixelFormat == PixelFormat.Format8bppIndexed)         bpp = 8;
                    else if (bm.PixelFormat == PixelFormat.Format4bppIndexed)    bpp = 4;
                    if (isGray)
                        id = new GrayImageData( bm, bm.Palette.Entries, bpp );
                    else
                        id = new ColorImageData( bm, bm.Palette.Entries, bpp );
                    id.mFname = fileName;
                    Console.WriteLine( id );
                    return id;

                case PixelFormat.Format16bppGrayScale:
                    MessageBox.Show( "Sorry!\n\n Unsupported PixelFormat - Format16bppGrayScale." );
                    break;
                case PixelFormat.Format16bppRgb555:
                    MessageBox.Show( "Sorry!\n\n Unsupported PixelFormat - Format16bppRgb555." );
                    break;
                case PixelFormat.Format16bppRgb565:
                    MessageBox.Show( "Sorry!\n\n Unsupported PixelFormat - Format16bppRgb565." );
                    break;

                case PixelFormat.Format24bppRgb:
                case PixelFormat.Format32bppRgb:
                    id = new ColorImageData( bm );
                    id.mFname = fileName;
                    Console.WriteLine( id );
                    return id;

                case PixelFormat.Format48bppRgb:
                    MessageBox.Show( "Sorry!\n\n Unsupported PixelFormat - Format48bppRgb." );
                    break;
                case PixelFormat.Format16bppArgb1555:
                    MessageBox.Show( "Sorry!\n\n Unsupported PixelFormat - Format16bppArgb1555." );
                    break;

                case PixelFormat.Format32bppArgb:
                    id = new ColorImageData( bm );
                    id.mFname = fileName;
                    Console.WriteLine( id );
                    return id;

                case PixelFormat.Format64bppArgb:
                    MessageBox.Show( "Sorry!\n\n Unsupported PixelFormat - Format64bppArgb." );
                    break;
                case PixelFormat.Format32bppPArgb:
                    MessageBox.Show( "Sorry!\n\n Unsupported PixelFormat - Format32bppPArgb." );
                    break;
                case PixelFormat.Format64bppPArgb:
                    MessageBox.Show( "Sorry!\n\n Unsupported PixelFormat - Format64bppPArgb." );
                    break;
            }

            MessageBox.Show( "Error:\n\n    Cannot read this file!" );
            return null;
        }
        //----------------------------------------------------------------
        /** \brief  Given an instance (of a subclass) of ImageData, construct
         * and return a clone of it.
         *
         *  Note that this static function returns the appropriate subclass of
         *  ImageData depending upon the type of image data (color or gray).
         *
         *  \param    other is the object to clone
         *  \returns  an instance of the ImageData class (actually the correct
         *            subclass of ImageData because ImageData is abstract)
         */
        static public ImageData clone ( ImageData other ) {
            if (other is GrayImageData) {
                GrayImageData copy = new GrayImageData( other.mOriginalData, other.mW, other.mH );
                copy.mIsAudio = other.mIsAudio;
                copy.mRate = other.mRate;
                return copy;
            }
            if (other is ColorImageData) {
                ColorImageData copy = new ColorImageData( other.mOriginalData, other.mW, other.mH );
                copy.mIsAudio = other.mIsAudio;
                copy.mRate = other.mRate;
                return copy;
            }
            return null;
        }
        //----------------------------------------------------------------
        /** ye olde tostringe methode.
         * see https://en.wikipedia.org/wiki/JSON for an example of JSON.
         */
        override public string ToString ( ) {
            return "{ \n"
                + "    \"mMax\": "          + mMax + ", \n"
                + "    \"mDisplayData\": "  + "[...], \n"            //good enough for arrays
                + "    \"mDisplayImage\": " + mDisplayImage + " \n"  //good enough for other contained objects
                + "}";
        }
        //----------------------------------------------------------------
        /** \brief    Save the display image to a file.
         *
         *  \param    fname  name of output image file (formats such as
         *            jpg/jpeg, bmp, png, pnm (raw and ascii), and tif/tiff are
         *            supported.
         *  \returns  nothing (void)
         */
        public void saveDisplayImage ( String fname ) {
            String up = fname.ToLower();
            if (up.EndsWith( ".binary.pnm" ) || up.EndsWith( ".binary.ppm" ) || up.EndsWith( ".binary.pgm" )) {
                if (mDisplayData != null)
                    pnmHelper.write_binary_pgm_or_ppm_data8( fname, mDisplayData, mW, mH, mIsColor ? 3 : 1 );
                else
                    pnmHelper.write_binary_pgm_or_ppm_data8( fname, mOriginalData, mW, mH, mIsColor ? 3 : 1 );
            } else if (up.EndsWith( ".pnm" ) || up.EndsWith( ".ppm" ) || up.EndsWith( ".pgm" )) {
                if (mDisplayData != null)
                    pnmHelper.write_pgm_or_ppm_ascii_data( fname, mDisplayData, mW, mH, mIsColor ? 3 : 1 );
                else
                    pnmHelper.write_pgm_or_ppm_ascii_data( fname, mOriginalData, mW, mH, mIsColor ? 3 : 1 );
            } else if (up.EndsWith( ".wav" ) || up.EndsWith( ".wave" )) {
                if (mDisplayData != null)
                    wavHelper.write( fname, mDisplayData, mW, mH, mRate );
                else
                    wavHelper.write( fname, mOriginalData, mW, mH, mRate );
            } else {
                mDisplayImage.Save( fname );
            }
        }
        //----------------------------------------------------------------
        //
        //accessor methods:
        //
        /** \brief accessor returning whether this is a color (or gray) image. */
        public bool getIsColor ( ) {
            return mIsColor;
        }
        /** \brief accessor returning whether this is audio data. */
        public bool getIsAudio ( ) {
            return mIsColor;
        }
        /** \brief accessor returning whether this image has been modified (changed) or not. */
        public bool getImageModified ( ) {
            return mImageModified;
        }
        /** \brief accessor returning image width. */
        public int getW ( ) {
            return mW;
        }
        /** \brief accessor returning image height. */
        public int getH ( ) {
            return mH;
        }
        /** \brief accessor returning min image value. */
        public int getMin ( ) {
            return mMin;
        }
        /** \brief accessor returning max image value. */
        public int getMax ( ) {
            return mMax;
        }
        /** \brief accessor returning specific pixel value in image (in original image data). */
        public int getData ( int i ) {
            return mOriginalData[ i ];
        }
        /** \brief accessor returning input image file name (if any). */
        public String getFname ( ) {
            return mFname;
        }
        /** \brief accessor returning audio sample rate (audio only). */
        public int getRate ( ) {
            return mRate;
        }
        //----------------------------------------------------------------
        /** this function is NOT in the original start up app. it simply
         * copies the display data to the original data (and sets min and
         * max accordingly. it was added to make pipelines of Strategies 
         * easier. for example, opening is erosion followed by dilation. 
         * so OpeningStrategy = ErosionStrategy then makePermanent then 
         * DilationStrategy then makePermanent. this approach may be used 
         * with any strategy: XStategy then makePermanent.
         */
        public void makePermanent ( ) {
            this.mMax = this.mMin = this.mDisplayData[ 0 ];
            for (int i = 0; i < this.mDisplayData.Length; i++) {
                int v = this.mDisplayData[ i ];
                if (v < this.mMin) this.mMin = v;
                if (v > this.mMax) this.mMax = v;
                this.mOriginalData[ i ] = v;
            }
        }

    }

}
