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
//using System.Collections.Generic;
//using System.Text;
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

        protected bool    mIsColor;        ///< true if color (rgb); false if gray
        protected bool    mImageModified;  ///< true if image has been modified
        protected int     mW;              ///< image width
        protected int     mH;              ///< image height
        protected int     mMin;            ///< overall min image pixel value
        protected int     mMax;            ///< overall max image pixel value
        protected String  mFname;          ///< (optional) file name

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
                //technique (trick?  kludge?) to implement pass-by-reference
                int[] w   = new int[ 1 ];
                int[] h   = new int[ 1 ];
                int[] spp = new int[ 1 ];
                int[] min = new int[ 1 ];
                int[] max = new int[ 1 ];
                int[] originalData = pnmHelper.read_pnm_file( fileName, w, h, spp, min, max );
                t.print();
                if (max[ 0 ] > 255)
                    MessageBox.Show( "Warning:\n\nMax value of " + max[0] + " exceeds limit of 255." );
                if (spp[ 0 ] == 3) {
                    id = new ColorImageData( originalData, w[0], h[0] );
                    id.mFname = fileName;
                    return id;
                }
                if (spp[ 0 ] == 1) {
                    id = new GrayImageData( originalData, w[0], h[0] );
                    id.mFname = fileName;
                    return id;
                }
            }
            else {
                Console.WriteLine("here");
                //Console.Out.WriteLine("here");
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
                        for (int i=0; i<bm.Palette.Entries.Length; i++) {
                            Color c = bm.Palette.Entries[ i ];
                            if (c.R == c.G && c.G == c.B) continue;
                            isGray = false;
                            break;
                        }
                        int bpp = 0;
                        if (bm.PixelFormat == PixelFormat.Format8bppIndexed) bpp = 8;
                        else if (bm.PixelFormat == PixelFormat.Format4bppIndexed) bpp = 4;
                        if (isGray)
                            id = new GrayImageData(  bm, bm.Palette.Entries, bpp );
                        else
                            id = new ColorImageData( bm, bm.Palette.Entries, bpp );
                        id.mFname = fileName;
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
            }
            return null;
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
                    pnmHelper.write_binary_pgm_or_ppm_data8( fname, mDisplayData,  mW, mH, mIsColor ? 3 : 1 );
                else
                    pnmHelper.write_binary_pgm_or_ppm_data8( fname, mOriginalData, mW, mH, mIsColor ? 3 : 1 );
            }
            else if (up.EndsWith( ".pnm" ) || up.EndsWith( ".ppm" ) || up.EndsWith( ".pgm" )) {
                if (mDisplayData != null)
                    pnmHelper.write_pgm_or_ppm_ascii_data( fname, mDisplayData,  mW, mH, mIsColor ? 3 : 1 );
                else
                    pnmHelper.write_pgm_or_ppm_ascii_data( fname, mOriginalData, mW, mH, mIsColor ? 3 : 1 );
            }
            else {
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
        public String getFname (  ) {
            return mFname;
        }

    }

}
