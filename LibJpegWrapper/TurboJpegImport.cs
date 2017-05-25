using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace TurboJpegWrapper
{
    static class TurboJpegImport
    {
        private const string UnmanagedLibrary = "turbojpeg";
        
        static TurboJpegImport()
        {
            Load();
        }

        internal static bool LibraryFound
        {
            get;
            private set;
        }

        private static void Load()
        {
            // When the library is first called, call LoadLibrary with the full path to the
            // path of the various libaries, to make sure they are loaded from the exact
            // path we specify.

            // Any load errors would also be caught by us here, making it easier to troubleshoot.
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                string path = UnmanagedLibrary;
                
                // Attempt to load the libraries. If they are not found, throw an error.
                // See also http://blogs.msdn.com/b/adam_nathan/archive/2003/04/25/56643.aspx for
                // more information about GetLastWin32Error
                IntPtr result = NativeMethods.LoadLibraryW(path);
                if (result == IntPtr.Zero)
                {
                    var lastError = Marshal.GetLastWin32Error();
                    var error = new Win32Exception(lastError);
                    throw error;
                }

                LibraryFound = true;
            }
            else
            {
                throw new NotSupportedException("Quamotion.TurboJpegWrapper is supported on Windows (.NET FX, .NET Core), Linux (.NET Core) and OS X (.NET Core)");
            }
        }

        /// <summary>
        /// Pixel size (in bytes) for a given pixel format.
        /// </summary>
        public static readonly Dictionary<TJPixelFormats, int> PixelSizes = new Dictionary<TJPixelFormats, int>
        {
            { TJPixelFormats.RGB, 3},
            { TJPixelFormats.BGR, 3},
            { TJPixelFormats.RGBX, 4},
            { TJPixelFormats.BGRX, 4},
            { TJPixelFormats.XBGR, 4},
            { TJPixelFormats.XRGB, 4},
            { TJPixelFormats.Gray, 1},
            { TJPixelFormats.RGBA, 4},
            { TJPixelFormats.BGRA, 4},
            { TJPixelFormats.ABGR, 4},
            { TJPixelFormats.ARGB, 4},
            { TJPixelFormats.CMYK, 4}
        };
        /// <summary>
        /// MCU block width (in pixels) for a given level of chrominance subsampling.
        /// MCU block sizes:
        /// <list type="bullet">
        /// <item><description>8x8 for no subsampling or grayscale</description></item>
        /// <item><description>16x8 for 4:2:2</description></item>
        /// <item><description>8x16 for 4:4:0</description></item>
        /// <item><description>16x16 for 4:2:0</description></item>
        /// <item><description>32x8 for 4:1:1</description></item>
        /// </list>
        /// </summary>
        public static readonly Dictionary<TJSubsamplingOptions, Size> MCUSizes = new Dictionary<TJSubsamplingOptions, Size>
        {
            { TJSubsamplingOptions.Gray, new Size(8, 8) },
            { TJSubsamplingOptions.Sampling444, new Size(8, 8) },
            { TJSubsamplingOptions.Sampling422, new Size(16, 8) },
            { TJSubsamplingOptions.Sampling420, new Size(16, 16) },
            { TJSubsamplingOptions.Sampling440, new Size(8, 16) },
            { TJSubsamplingOptions.Sampling411, new Size(32, 8) },
        };

        /// <summary>
        /// This is port of TJPAD macros from turbojpeg.h
        /// Pad the given width to the nearest 32-bit boundary
        /// </summary>
        /// <param name="width">Width</param>
        /// <returns>Padded width</returns>
        public static int TJPad(int width)
        {
            return ((width) + 3) & (~3);
        }

        /// <summary>
        /// This is port of TJSCALED macros from turbojpeg.h
        /// Compute the scaled value of <paramref name="dimension"/> using the given scaling factor.
        /// </summary>
        /// <param name="dimension">Dimension to scale</param>
        /// <param name="scalingFactor">Scaling factor</param>
        /// <returns></returns>
        public static int TJScaled(int dimension, TJScalingfactor scalingFactor)
        {
            return ((dimension * scalingFactor.Num + scalingFactor.Denom - 1) / scalingFactor.Denom);
        }

        /// <summary>
        /// Create a TurboJPEG compressor instance.
        /// </summary>
        /// <returns>
        /// handle to the newly-created instance, or <see cref="IntPtr.Zero"/> 
        /// if an error occurred (see <see cref="GetErrorStr"/>)</returns>
        public static IntPtr InitCompress()
        {
            return NativeMethods.tjInitCompress();
        }

        /// <summary>
        /// Compress an RGB, grayscale, or CMYK image into a JPEG image.
        /// </summary>
        /// <param name="handle">A handle to a TurboJPEG compressor or transformer instance</param>
        /// 
        /// <param name="srcBuf">
        /// Pointer to an image buffer containing RGB, grayscale, or CMYK pixels to be compressed.
        /// </param>
        /// 
        /// <param name="width">Width (in pixels) of the source image</param>
        /// 
        /// <param name="pitch">
        /// Bytes per line in the source image.  
        /// Normally, this should be <c>width * tjPixelSize[pixelFormat]</c> if the image is unpadded, 
        /// or <c>TJPad(width * tjPixelSize[pixelFormat])</c> if each line of the image
        /// is padded to the nearest 32-bit boundary, as is the case for Windows bitmaps.  
        /// You can also be clever and use this parameter to skip lines, etc.
        /// Setting this parameter to 0 is the equivalent of setting it to
        /// <c>width * tjPixelSize[pixelFormat]</c>.
        /// </param>
        /// 
        /// <param name="height">Height (in pixels) of the source image</param>
        /// 
        /// <param name="pixelFormat">Pixel format of the source image (see <see cref="TJPixelFormats"/> "Pixel formats")</param>
        /// 
        /// <param name="jpegBuf">
        /// Address of a pointer to an image buffer that will receive the JPEG image.
        /// TurboJPEG has the ability to reallocate the JPEG buffer
        /// to accommodate the size of the JPEG image.  Thus, you can choose to:
        /// <list type="number">
        /// <item>
        /// <description>pre-allocate the JPEG buffer with an arbitrary size using <see cref="Alloc"/> and let TurboJPEG grow the buffer as needed</description>
        /// </item>
        /// <item>
        /// <description>set <paramref name="jpegBuf"/> to NULL to tell TurboJPEG to allocate the buffer for you</description>
        /// </item>
        /// <item>
        /// <description>pre-allocate the buffer to a "worst case" size determined by calling <see cref="BufSize"/>.
        /// This should ensure that the buffer never has to be re-allocated (setting <see cref="TJFlags.NoRealloc"/> guarantees this.).</description>
        /// </item>
        /// </list>
        /// If you choose option 1, <paramref name="jpegSize"/> should be set to the size of your pre-allocated buffer.  
        /// In any case, unless you have set <see cref="TJFlags.NoRealloc"/>,
        /// you should always check <paramref name="jpegBuf"/> upon return from this function, as it may have changed.
        /// </param>
        /// 
        /// <param name="jpegSize">
        /// Pointer to an unsigned long variable that holds the size of the JPEG image buffer.
        /// If <paramref name="jpegBuf"/> points to a pre-allocated buffer, 
        /// then <paramref name="jpegSize"/> should be set to the size of the buffer.
        /// Upon return, <paramref name="jpegSize"/> will contain the size of the JPEG image (in bytes.)  
        /// If <paramref name="jpegBuf"/> points to a JPEG image buffer that is being
        /// reused from a previous call to one of the JPEG compression functions, 
        /// then <paramref name="jpegSize"/> is ignored.
        /// </param>
        /// 
        /// <param name="jpegSubsamp">
        /// The level of chrominance subsampling to be used when
        /// generating the JPEG image (see <see cref="TJSubsamplingOptions"/> "Chrominance subsampling options".)
        /// </param>
        /// 
        /// <param name="jpegQual">The image quality of the generated JPEG image (1 = worst, 100 = best)</param>
        /// 
        /// <param name="flags">The bitwise OR of one or more of the <see cref="TJFlags"/> "flags"</param>
        /// 
        /// <returns>0 if successful, or -1 if an error occurred (see <see cref="GetErrorStr"/>)</returns>
        public static int Compress2(IntPtr handle, IntPtr srcBuf, int width, int pitch, int height, TJPixelFormats pixelFormat, ref IntPtr jpegBuf, ref ulong jpegSize, TJSubsamplingOptions jpegSubsamp, int jpegQual, TJFlags flags)
        {
            return NativeMethods.tjCompress2(handle, srcBuf, width, pitch, height, pixelFormat, ref jpegBuf, ref jpegSize, jpegSubsamp, jpegQual, flags);
        }

        /// <summary>
        /// The maximum size of the buffer (in bytes) required to hold a JPEG image with
        /// the given parameters.  The number of bytes returned by this function is
        /// larger than the size of the uncompressed source image.  The reason for this
        /// is that the JPEG format uses 16-bit coefficients, and it is thus possible
        /// for a very high-quality JPEG image with very high-frequency content to
        /// expand rather than compress when converted to the JPEG format.  Such images
        /// represent a very rare corner case, but since there is no way to predict the
        /// size of a JPEG image prior to compression, the corner case has to be handled.
        /// </summary>
        /// <param name="width">Width (in pixels) of the image</param>
        /// <param name="height">Height (in pixels) of the image</param>
        /// <param name="jpegSubsamp">
        /// The level of chrominance subsampling to be used when
        /// generating the JPEG image(see <see cref="TJSubsamplingOptions"/> "Chrominance subsampling options".)
        /// </param>
        /// <returns>
        /// The maximum size of the buffer (in bytes) required to hold the image, 
        /// or -1 if the arguments are out of bounds.
        /// </returns>
        public static long BufSize(int width, int height, TJSubsamplingOptions jpegSubsamp)
        {
            return NativeMethods.tjBufSize(width, height, jpegSubsamp);
        }

        /// <summary>
        ///  Create a TurboJPEG decompressor instance.
        /// </summary>
        /// <returns>A handle to the newly-created instance, or NULL if an error occurred(see <see cref="GetErrorStr"/>)</returns>
        public static IntPtr InitDecompress()
        {
            return NativeMethods.tjInitDecompress();
        }

        /// <summary>
        /// Retrieve information about a JPEG image without decompressing it.
        /// </summary>
        /// <param name="handle">A handle to a TurboJPEG decompressor or transformer instance</param>
        /// <param name="jpegBuf">Pointer to a buffer containing a JPEG image.</param>
        /// <param name="jpegSize">Size of the JPEG image (in bytes)</param>
        /// <param name="width">Pointer to an integer variable that will receive the width (in pixels) of the JPEG image</param>
        /// <param name="height">Pointer to an integer variable that will receive the height (in pixels) of the JPEG image</param>
        /// <param name="jpegSubsamp">
        /// Pointer to an integer variable that will receive the level of chrominance subsampling used 
        /// when the JPEG image was compressed (see <see cref="TJSubsamplingOptions"/> "Chrominance subsampling options".)
        /// </param>
        /// <param name="jpegColorspace">Pointer to an integer variable that will receive one of the JPEG colorspace constants, 
        /// indicating the colorspace of the JPEG image(see <see cref="TJColorSpaces"/> "JPEG colorspaces".)</param>
        /// <returns>0 if successful, or -1 if an error occurred (see <see cref="GetErrorStr"/>)</returns>
        public static int DecompressHeader(IntPtr handle, IntPtr jpegBuf, ulong jpegSize, out int width,
            out int height, out TJSubsamplingOptions jpegSubsamp, out int jpegColorspace)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return NativeMethods.tjDecompressHeader3_x86(handle, jpegBuf, (uint)jpegSize, out width, out height, out jpegSubsamp,
                        out jpegColorspace);
                case 8:
                    return NativeMethods.tjDecompressHeader3_x64(handle, jpegBuf, jpegSize, out width, out height, out jpegSubsamp,
                        out jpegColorspace);

                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        
        /// <summary>
        /// Returns a list of fractional scaling factors that the JPEG decompressor in this implementation of TurboJPEG supports.
        /// </summary>
        /// <param name="numscalingfactors">Pointer to an integer variable that will receive the number of elements in the list</param>
        /// <returns>A pointer to a list of fractional scaling factors, or <see cref="IntPtr.Zero"/> if an error is encountered (see <see cref="GetErrorStr"/>)</returns>
        public static IntPtr GetScalingFactors(out int numscalingfactors)
        {
            return NativeMethods.tjGetScalingFactors(out numscalingfactors);
        }

        /// <summary>
        /// Decompress a JPEG image to an RGB, grayscale, or CMYK image.
        /// </summary>
        /// <param name="handle">A handle to a TurboJPEG decompressor or transformer instance</param>
        /// <param name="jpegBuf">Pointer to a buffer containing the JPEG image to decompress.</param>
        /// <param name="jpegSize">Size of the JPEG image (in bytes)</param>
        /// <param name="dstBuf">
        /// Pointer to an image buffer that will receive the decompressed image.
        /// This buffer should normally be <c> pitch * scaledHeight</c> bytes in size, 
        /// where <c>scaledHeight</c> can be determined by calling <see cref="TJScaled"/> with the JPEG image height and one of the scaling factors returned by <see cref="GetScalingFactors"/>.  
        /// The <paramref name="dstBuf"/> pointer may also be used to decompress into a specific region of a larger buffer.
        /// </param>
        /// <param name="width">
        /// Desired width (in pixels) of the destination image.  
        /// If this is different than the width of the JPEG image being decompressed, then TurboJPEG will use scaling in the JPEG decompressor to generate the largest possible image that will fit within the desired width.
        /// If <paramref name="width"/> is set to 0, then only the height will be considered when determining the scaled image size.
        /// </param>
        /// <param name="pitch">
        /// Bytes per line in the destination image.  Normally, this is <c>scaledWidth* tjPixelSize[pixelFormat]</c> if the decompressed image is unpadded, else <c>TJPad(scaledWidth * tjPixelSize[pixelFormat])</c> if each line of the decompressed image is padded to the nearest 32-bit boundary, as is the case for Windows bitmaps. 
        /// <remarks>Note: <c>scaledWidth</c> can be determined by calling <see cref="TJScaled"/> with the JPEG image width and one of the scaling factors returned by <see cref="GetScalingFactors"/>
        /// </remarks>
        /// You can also be clever and use the pitch parameter to skip lines, etc.
        /// Setting this parameter to 0 is the equivalent of setting it to <c>scaledWidth* tjPixelSize[pixelFormat]</c>.
        /// </param>
        /// <param name="height">
        /// Desired height (in pixels) of the destination image.  
        /// If this is different than the height of the JPEG image being decompressed, then TurboJPEG will use scaling in the JPEG decompressor to generate the largest possible image that will fit within the desired height.
        /// If <paramref name="height"/> is set to 0, then only the width will be considered when determining the scaled image size.
        /// </param>
        /// <param name="pixelFormat">Pixel format of the destination image (see <see cref="TJPixelFormats"/> "Pixel formats".)</param>
        /// <param name="flags">The bitwise OR of one or more of the <see cref="TJFlags"/> "flags"</param>
        /// <returns>0 if successful, or -1 if an error occurred (see <see cref="GetErrorStr"/>)</returns>
        public static int Decompress(IntPtr handle, IntPtr jpegBuf, ulong jpegSize, IntPtr dstBuf, int width,
            int pitch, int height, TJPixelFormats pixelFormat, TJFlags flags)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return NativeMethods.tjDecompress2_x86(handle, jpegBuf, (uint)jpegSize, dstBuf, width, pitch, height, pixelFormat, flags);
                case 8:
                    return NativeMethods.tjDecompress2_x64(handle, jpegBuf, jpegSize, dstBuf, width, pitch, height, pixelFormat, flags);

                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }

        /// <summary>
        /// Allocate an image buffer for use with TurboJPEG.  You should always use
        /// this function to allocate the JPEG destination buffer(s) for <see cref="Compress2"/>
        /// and <see cref="Transform"/> unless you are disabling automatic buffer
        /// (re)allocation (by setting <see cref="TJFlags.NoRealloc"/>.)
        /// </summary>
        /// <param name="bytes">The number of bytes to allocate</param>
        /// <returns>A pointer to a newly-allocated buffer with the specified number of bytes</returns>
        /// <seealso cref="Free"/>
        public static IntPtr Alloc(int bytes)
        {
            return NativeMethods.tjAlloc(bytes);
        }

        /// <summary>
        /// Free an image buffer previously allocated by TurboJPEG.  You should always
        /// use this function to free JPEG destination buffer(s) that were automatically
        /// (re)allocated by <see cref="Compress2"/> or <see cref="Transform"/> or that were manually
        /// allocated using <see cref="Alloc"/>. 
        /// </summary>
        /// <param name="buffer">Address of the buffer to free</param>
        /// <seealso cref="Alloc"/>
        public static void Free(IntPtr buffer)
        {
            NativeMethods.tjFree(buffer);
        }

        /// <summary>
        /// Create a new TurboJPEG transformer instance
        /// </summary>
        /// <returns>@return a handle to the newly-created instance, or NULL if an error occurred(see <see cref="GetErrorStr"/>)</returns>
        public static IntPtr InitTransform()
        {
            return NativeMethods.tjInitTransform();
        }

        /// <summary>
        /// Losslessly transform a JPEG image into another JPEG image.  Lossless
        /// transforms work by moving the raw DCT coefficients from one JPEG image
        /// structure to another without altering the values of the coefficients.  While
        /// this is typically faster than decompressing the image, transforming it, and
        /// re-compressing it, lossless transforms are not free.  Each lossless
        /// transform requires reading and performing Huffman decoding on all of the
        /// coefficients in the source image, regardless of the size of the destination
        /// image.  Thus, this function provides a means of generating multiple
        /// transformed images from the same source or  applying multiple
        /// transformations simultaneously, in order to eliminate the need to read the
        /// source coefficients multiple times.
        /// </summary>
        /// <param name="handle">A handle to a TurboJPEG transformer instance</param>
        /// <param name="jpegBuf">
        /// Pointer to a buffer containing the JPEG source image to transform.
        /// </param>
        /// <param name="jpegSize">Size of the JPEG source image (in bytes)</param>
        /// <param name="n">The number of transformed JPEG images to generate</param>
        /// <param name="dstBufs">
        /// Pointer to an array of n image buffers. <paramref name="dstBufs"/>[i] will receive a JPEG image that has been transformed using the parameters in <paramref name="transforms"/>[i]
        /// TurboJPEG has the ability to reallocate the JPEG buffer
        /// to accommodate the size of the JPEG image.  Thus, you can choose to:
        /// <list type="number">
        /// <item>
        /// <description>pre-allocate the JPEG buffer with an arbitrary size using <see cref="Alloc"/> and let TurboJPEG grow the buffer as needed</description>
        /// </item>
        /// <item>
        /// <description>set <paramref name="dstBufs"/>[i] to NULL to tell TurboJPEG to allocate the buffer for you</description>
        /// </item>
        /// <item>
        /// <description>pre-allocate the buffer to a "worst case" size determined by calling <see cref="BufSize"/>.
        /// This should ensure that the buffer never has to be re-allocated (setting <see cref="TJFlags.NoRealloc"/> guarantees this.).</description>
        /// </item>
        /// </list>
        /// If you choose option 1, <paramref name="dstSizes"/>[i] should be set to the size of your pre-allocated buffer.  
        /// In any case, unless you have set <see cref="TJFlags.NoRealloc"/>,
        /// you should always check <paramref name="dstBufs"/>[i] upon return from this function, as it may have changed.
        /// </param>
        /// <param name="dstSizes">
        /// Pointer to an array of <paramref name="n"/> unsigned long variables that will
        /// receive the actual sizes (in bytes) of each transformed JPEG image.  
        /// If <paramref name="dstBufs"/>[i] points to a pre-allocated buffer, 
        /// then <paramref name="dstSizes"/>[i] should be set to the size of the buffer.  
        /// Upon return, <paramref name="dstSizes"/>[i] will contain the size of the JPEG image (in bytes.)
        /// </param>
        /// <param name="transforms">
        /// Pointer to an array of <see cref="TJTransform"/> structures, each of
        /// which specifies the transform parameters and/or cropping region for the
        /// corresponding transformed output image.
        /// </param>
        /// <param name="flags">flags the bitwise OR of one or more of the <see cref="TJFlags"/> "flags"</param>
        /// <returns>0 if successful, or -1 if an error occurred (see <see cref="GetErrorStr"/>)</returns>
        public static int Transform(IntPtr handle, IntPtr jpegBuf, ulong jpegSize, int n, IntPtr[] dstBufs,
          ulong[] dstSizes, IntPtr transforms, int flags)
        {
            var intSizes = new uint[dstSizes.Length];
            for (var i = 0; i < dstSizes.Length; i++)
            {
                intSizes[i] = (uint)dstSizes[i];
            }
            int result;
            switch (IntPtr.Size)
            {
                case 4:
                    result = NativeMethods.tjTransform_x86(handle, jpegBuf, (uint)jpegSize, n, dstBufs, intSizes, transforms, flags);
                    break;
                case 8:
                    result = NativeMethods.tjTransform_x64(handle, jpegBuf, jpegSize, n, dstBufs, intSizes, transforms, flags);
                    break;
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
            for (var i = 0; i < dstSizes.Length; i++)
            {
                dstSizes[i] = intSizes[i];
            }
            return result;
        }

        /// <summary>
        /// Destroy a TurboJPEG compressor, decompressor, or transformer instance
        /// </summary>
        /// <param name="handle">a handle to a TurboJPEG compressor, decompressor or transformer instance</param>
        /// <returns>0 if successful, or -1 if an error occurred (see <see cref="GetErrorStr"/>)</returns>
        public static int Destroy(IntPtr handle)
        {
            var result = NativeMethods.tjDestroy(handle);
#if DEBUG
            if (result != 0)
                TJUtils.GetErrorAndThrow();
#endif
            return result;
        }

        /// <summary>
        /// Returns a descriptive error message explaining why the last command failed
        /// </summary>
        /// <returns>A descriptive error message explaining why the last command failed</returns>
        public static string GetErrorStr()
        {
            var ptr = NativeMethods.tjGetErrorStr();
            return Marshal.PtrToStringAnsi(ptr);
        }

        private static class NativeMethods
        {

            [DllImport(UnmanagedLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tjTransform")]
            public static extern int tjTransform_x86(IntPtr handle, IntPtr jpegBuf, uint jpegSize, int n,
                IntPtr[] dstBufs,
                uint[] dstSizes, IntPtr transforms, int flags);

            [DllImport(UnmanagedLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tjTransform")]
            public static extern int tjTransform_x64(IntPtr handle, IntPtr jpegBuf, ulong jpegSize, int n,
                IntPtr[] dstBufs,
                uint[] dstSizes, IntPtr transforms, int flags);

            [DllImport(UnmanagedLibrary, CallingConvention = CallingConvention.Cdecl)]
            public static extern int tjDestroy(IntPtr handle);

            [DllImport(UnmanagedLibrary, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr tjGetErrorStr();

            [DllImport(UnmanagedLibrary, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr tjInitTransform();

            [DllImport(UnmanagedLibrary, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr tjInitDecompress();

            [DllImport(UnmanagedLibrary, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr tjInitCompress();

            [DllImport(UnmanagedLibrary, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr tjGetScalingFactors(out int numscalingfactors);

            [DllImport(UnmanagedLibrary, CallingConvention = CallingConvention.Cdecl)]
            public static extern void tjFree(IntPtr buffer);

            [DllImport(UnmanagedLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tjDecompressHeader3")]
            public static extern int tjDecompressHeader3_x86(IntPtr handle, IntPtr jpegBuf, uint jpegSize, out int width, out int height, out TJSubsamplingOptions jpegSubsamp, out int jpegColorspace);

            [DllImport(UnmanagedLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tjDecompressHeader3")]
            public static extern int tjDecompressHeader3_x64(IntPtr handle, IntPtr jpegBuf, ulong jpegSize, out int width, out int height, out TJSubsamplingOptions jpegSubsamp, out int jpegColorspace);
            
            [DllImport(UnmanagedLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tjDecompress2")]
            public static extern int tjDecompress2_x86(IntPtr handle, IntPtr jpegBuf, uint jpegSize, IntPtr dstBuf, int width, int pitch, int height, TJPixelFormats pixelFormat, TJFlags flags);
            [DllImport(UnmanagedLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tjDecompress2")]
            public static extern int tjDecompress2_x64(IntPtr handle, IntPtr jpegBuf, ulong jpegSize, IntPtr dstBuf, int width, int pitch, int height, TJPixelFormats pixelFormat, TJFlags flags);

            [DllImport(UnmanagedLibrary, CallingConvention = CallingConvention.Cdecl)]
            public static extern int tjCompress2(IntPtr handle, IntPtr srcBuf, int width, int pitch, int height, TJPixelFormats pixelFormat, ref IntPtr jpegBuf, ref ulong jpegSize, TJSubsamplingOptions jpegSubsamp, int jpegQual, TJFlags flags);

            [DllImport(UnmanagedLibrary, CallingConvention = CallingConvention.Cdecl)]
            public static extern long tjBufSize(int width, int height, TJSubsamplingOptions jpegSubsamp);

            [DllImport(UnmanagedLibrary, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr tjAlloc(int bytes);

            /// <summary>
            /// The name of the <c>kernel32</c> library
            /// </summary>
            private const string Kernel32 = "kernel32";

            /// <summary>
            /// Loads the specified module into the address space of the calling process. The specified module may cause other modules to be loaded.
            /// </summary>
            /// <param name="dllToLoad">
            /// <para>
            /// The name of the module. This can be either a library module (a <c>.dll</c> file) or an executable module (an <c>.exe</c> file).
            /// The name specified is the file name of the module and is not related to the name stored in the library module itself,
            /// as specified by the LIBRARY keyword in the module-definition (<c>.def</c>) file.
            /// </para>
            /// <para>
            /// If the string specifies a full path, the function searches only that path for the module.
            /// </para>
            /// <para>
            /// If the string specifies a relative path or a module name without a path, the function uses a standard search strategy
            /// to find the module; for more information, see the Remarks.
            /// </para>
            /// <para>
            /// If the function cannot find the module, the function fails. When specifying a path, be sure to use backslashes (<c>\</c>),
            /// not forward slashes (<c>/</c>). For more information about paths, see Naming a File or Directory.
            /// </para>
            /// <para>
            /// If the string specifies a module name without a path and the file name extension is omitted, the function appends the
            /// default library extension <c>.dll</c> to the module name. To prevent the function from appending <c>.dll</c> to the module name,
            /// include a trailing point character (<c>.</c>) in the module name string.
            /// </para>
            /// </param>
            /// <returns>
            /// If the function succeeds, the return value is a handle to the module.
            /// If the function fails, the return value is <see cref="IntPtr.Zero"/>. To get extended error information, call
            /// <see cref="Marshal.GetLastWin32Error"/>.
            /// </returns>
            /// <seealso href="http://msdn.microsoft.com/en-us/library/windows/desktop/ms684175(v=vs.85).aspx"/>
            [DllImport(Kernel32, SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern IntPtr LoadLibraryW(string dllToLoad);
        }
    }
}
