/**
    \file   Timer.cs
    \brief  Contains Timer class definition.
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
using System.Windows.Forms;
//----------------------------------------------------------------------
#pragma warning disable IDE1006

namespace CSImageViewer {

/** \brief class containing timer (elapsed time) implementation.
 *  this class may be used to determine the elapsed time of a processing activity.
 *  timer resolution is milliseconds.
 *  \version 4.1 adds message indicating which version (debug or 
 *           release) is executing.
 */
public class Timer {

        private DateTime  mStart;  ///< when this timer started
        private double    mExtra;  ///< used to pause (start/stop) timer
        //----------------------------------------------------------------
        /** \brief    Timer class ctor.  Timer is started immediately.
         *  \returns  nothing (ctor)
         */
        public Timer ( ) {
            reset();
        }
        //----------------------------------------------------------------
        /** \brief    Reset/restart the time.  The timer continues to run.
         *  \returns  nothing (void)
         */
        public void reset ( ) {
            lock (this) {
                mExtra = 0;
                mStart = DateTime.Now;
            }
        }
        //----------------------------------------------------------------
        /** \brief    Get the elapsed time (in seconds).  Note that this
         *  function does not start/stop/reset the timer.  It continues to run.
         *  \returns  the elapsed time in seconds
         */
        public double getElapsedTime ( ) {
            lock (this) {
                TimeSpan  ts = DateTime.Now - mStart;
                return mExtra + ts.TotalMilliseconds / 1000.0;
            }
        }
        //----------------------------------------------------------------
        /** \brief Report the elapsed time so far (using a modal dialog 
         *  box). While this dialog is up, the timer is paused and resumes
         *  when the dialog is dismissed.
         *  \returns  nothing (void)
         */
        public void report ( ) {
            lock (this) {
                //record the total elapsed time and pause the timer while
                // the modal dialog box is up
                mExtra += getElapsedTime();
#if DEBUG
                MessageBox.Show( "(debug version) \n\n    elapsed time = "   + mExtra + " sec" );
#else
                MessageBox.Show( "(release version) \n\n    elapsed time = " + mExtra + " sec" );
#endif
                mStart = DateTime.Now;  //restart the paused timer
            }
        }
        //----------------------------------------------------------------
        /** \brief Report the elapsed time so far to the console.
         *  \returns  nothing (void)
         */
        public void print ( ) {
            lock (this) {
                mExtra += getElapsedTime();
#if DEBUG
                Console.WriteLine( "(debug version) elapsed time="  + mExtra + " sec" );
#else
                Console.WriteLine("(release version) elapsed time=" + mExtra + " sec" );
#endif
                mStart = DateTime.Now;  //restart the paused timer
            }
        }

    }  //end class

}  //end namespace
