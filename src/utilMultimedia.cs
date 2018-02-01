/*
 * This file is part of ChronoJump
 *
 * ChronoJump is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or   
 *    (at your option) any later version.
 *    
 * ChronoJump is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
 *    GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *   Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 *
 *  Copyright (C) 2004-2017   Xavier de Blas <xaviblas@gmail.com> 
 */

using System;
using System.Text; //StringBuilder
using System.Collections; //ArrayList
using System.Collections.Generic; //List
using System.Diagnostics; 	//for detect OS
using System.IO; 		//for detect OS
using Cairo;
using Gdk;

//this class tries to be a space for methods that are used in different classes
public class UtilMultimedia
{
	/*
	 * VIDEO
	 */

	public static string [] GetVideoDevices () {
		List<LongoMatch.Video.Utils.Device> devices = LongoMatch.Video.Utils.Device.ListVideoDevices();
		string [] devicesStr = new String[devices.Count];
		int count = 0;
		LogB.Information("Searching video devices");
		foreach(LongoMatch.Video.Utils.Device dev in devices) {
			devicesStr[count++] = dev.ID.ToString();
			LogB.Information(dev.ID.ToString());
		}
		return devicesStr;
	}

	/*
	 * IMAGES
	 */

	private enum ImageTypes { UNKNOWN, PNG, JPEG }
	private static ImageTypes getImageType(string filename)
	{
		if(filename.ToLower().EndsWith("jpeg") || filename.ToLower().EndsWith("jpg"))
			return ImageTypes.JPEG;
		else if(filename.ToLower().EndsWith("png"))
			return ImageTypes.PNG;

		return ImageTypes.UNKNOWN;
	}

	public static void ResizeImages()
	{
		string file1 = "/home/xavier/Imatges/tenerife.png";
		string file2 = "/home/xavier/Imatges/2016-06-07-152244.jpg";
		string file3 = "/home/xavier/Imatges/humor-desmotivaciones-ofender-lasers.jpeg";

		resizeImage(file1, "/home/xavier/Imatges/prova1.png", 100, 150);
		resizeImage(file2, "/home/xavier/Imatges/prova2.png", 200, 200);
		resizeImage(file3, "/home/xavier/Imatges/prova3.png", 400, 350);
	}
	
	private static void resizeImage(string filenameOriginal, string filenameDest, int width, int height)
	{
		ImageSurface imgSurface;
		if(getImageType(filenameOriginal) == ImageTypes.PNG)
		{
			imgSurface = LoadPngToCairoImageSurface(filenameOriginal);
		}
		else if(getImageType(filenameOriginal) == ImageTypes.JPEG)
		{
			imgSurface = LoadJpegToCairoImageSurface(filenameOriginal);
		}
		else //(getImageType(filenameOriginal) == ImageTypes.UNKNOWN)
		{
			return;
		}
		
		ImageSurfaceResize(imgSurface, filenameDest, width, height);
	}

	public static ImageSurface LoadJpegToCairoImageSurface(string jpegFilename)
	{
		Gdk.Pixbuf pixbuf = new Pixbuf (jpegFilename); //from a file
		return pixbufToCairoImageSurface(pixbuf);
	}

	// Thanks to: Chris Thomson
	// https://stackoverflow.com/questions/25106063/how-do-i-draw-pixbufs-onto-a-surface-with-cairo-sharp
	private static ImageSurface pixbufToCairoImageSurface(Pixbuf pixbuf)
	{
		ImageSurface imgSurface = new ImageSurface(Format.ARGB32, pixbuf.Width, pixbuf.Height);

		using (Cairo.Context cr = new Cairo.Context(imgSurface)) {
			Gdk.CairoHelper.SetSourcePixbuf (cr, pixbuf, 0, 0);
			cr.Paint ();
			cr.Dispose ();
		}

		return imgSurface;
	}

	public static ImageSurface LoadPngToCairoImageSurface (string pngFilename)
	{
		Cairo.ImageSurface imgSurface = new Cairo.ImageSurface(pngFilename);
                Context cr = new Context(imgSurface);
                cr.SetSource(imgSurface);
                cr.Paint();
		cr.Dispose ();

		return imgSurface;
	}

	public static void ImageSurfaceResize(ImageSurface imgSurface, string filename_dest,
			int width, int height)
	{
		Surface surfaceResized = scale_surface(
				imgSurface, imgSurface.Width, imgSurface.Height, width, height);

		LogB.Information("ImageFileResize - " + filename_dest);
		try {
			surfaceResized.WriteToPng(filename_dest);
		} catch {
			LogB.Warning("Catched at ImageFileResize");
		}
	}

        // Thanks to: Owen Taylor
	// https://lists.freedesktop.org/archives/cairo/2006-January/006178.html
	private static Surface scale_surface (Surface old_surface,
			int old_width, int old_height,
			int new_width, int new_height)
	{
		Surface new_surface = old_surface.CreateSimilar(Cairo.Content.ColorAlpha, new_width, new_height);
		Context cr = new Context (new_surface);

		/* Scale *before* setting the source surface (1) */
		cr.Scale ((double)new_width / old_width, (double)new_height / old_height);
		cr.SetSourceSurface (old_surface, 0, 0);

		/* To avoid getting the edge pixels blended with 0 alpha, which would
		 * occur with the default EXTEND_NONE. Use EXTEND_PAD for 1.2 or newer (2)
		 */
		Cairo.Pattern pattern = new SurfacePattern (old_surface);
		pattern.Extend = Cairo.Extend.Reflect;

		/* Replace the destination with the source instead of overlaying */
		cr.Operator = Cairo.Operator.Source;

		/* Do the actual drawing */
		cr.Paint();

		cr.Dispose();

		return new_surface;
	}

}
