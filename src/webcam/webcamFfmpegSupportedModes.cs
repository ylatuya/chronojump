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
 * Copyright (C) 2004-2017   Xavier de Blas <xaviblas@gmail.com> 
 */

using System;
using System.Collections.Generic; //List<T>
using System.Text.RegularExpressions; //Regex


public abstract class WebcamFfmpegSupportedModes
{
	protected List<WebcamSupportedModesList> wsmListOfLists;
	protected string modesStr;
	protected string errorStr;
	protected string cameraCode;

	public abstract void GetModes();

	//for mac and maybe windows, because in Linux it founds a default mode and it works
	protected void initialize ()
	{
		wsmListOfLists = new List<WebcamSupportedModesList>();
		modesStr = "";
		errorStr = "";
	}

	protected abstract string parseSupportedModes(string allOutput);

	protected string printListOfLists()
	{
		string nothingFound = "Not found any mode supported for your camera.";
		string str = "";
		string sep = "";
		bool foundAtLeastOne = false;
		foreach(WebcamSupportedModesList wsmList in wsmListOfLists)
		{
			str += sep + wsmList.PixelFormat + "\n";
			sep = "\n";
			str += wsmList.ToString();
			foundAtLeastOne = true;
		}

		if(foundAtLeastOne)
			return str;
		else
			return nothingFound;
	}

	public string ErrorStr
	{
		get { return errorStr;  }
	}

	public string ModesStr
	{
		get { return modesStr;  }
	}
}

public class WebcamFfmpegSupportedModesLinux : WebcamFfmpegSupportedModes
{
	public WebcamFfmpegSupportedModesLinux()
	{
		initialize();
	}

	public override void GetModes()
	{
		List<string> parameters = new List<string>();
		parameters.Add("--list-formats-ext");
		ExecuteProcess.Result execute_result = ExecuteProcess.run ("v4l2-ctl", parameters, true, true);
		if(! execute_result.success) {
			errorStr = "Need to install v4l2-ctl (on v4l-utils) to know modes";
			return;
		}

		modesStr = parseSupportedModes(execute_result.stdout);
	}

	protected override string parseSupportedModes(string allOutput)
	{
		/*
		 * break the big string in \n strings
		 * https://stackoverflow.com/a/1547483
		 */
		string[] lines = allOutput.Split(
				new[] { Environment.NewLine },
				StringSplitOptions.None
				);

		WebcamSupportedModesList wsmList = null;
		WebcamSupportedMode currentMode = null;
		foreach(string l in lines)
		{
			LogB.Information("line: " + l);
			if(l.Contains("Pixel Format:"))
			{
				wsmList = new WebcamSupportedModesList(l);
				wsmListOfLists.Add(wsmList);

				continue;
			}

			string resolutionStr = matchResolution(l);
			if(wsmList != null && l.Contains("Size: Discrete") && resolutionStr != "")
			{
				if(wsmList.ModeExist(resolutionStr))
					currentMode = wsmList.GetMode(resolutionStr);
				else {
					currentMode = new WebcamSupportedMode(resolutionStr);
					wsmList.Add(currentMode);
				}
			}

			if(l.Contains("Interval: Discrete") && l.Contains("fps") && matchFPS(l) != "")
			{
				if(currentMode != null)
					currentMode.AddFramerate(matchFPS(l));
			}
		}

		return printListOfLists();
	}

	private string matchResolution(string l)
	{
		Match match = Regex.Match(l, @"(\d+)x(\d+)");

		LogB.Information("match group count is 3?", (match.Groups.Count == 3).ToString());

		if(match.Groups.Count == 3)
			return string.Format("{0}x{1}", match.Groups[1].Value, match.Groups[2].Value);

		return "";
	}

	private string matchFPS(string l)
	{
		Match match = Regex.Match(l, @"\((\d+).(\d+) fps\)");

		LogB.Information("match group count is 3?", (match.Groups.Count == 3).ToString());

		if(match.Groups.Count == 3)
			return string.Format("{0}.{1}", match.Groups[1].Value, match.Groups[2].Value);

		return "";
	}
}

public class WebcamFfmpegSupportedModesWindows : WebcamFfmpegSupportedModes
{
	private WebcamSupportedModesList wsmList;

	public WebcamFfmpegSupportedModesWindows(string cameraCode)
	{
		initialize();
		this.cameraCode = cameraCode;
	}

	public override void GetModes()
	{
		bool testParsing = false; //change it to true to test the parsing method
		if(testParsing)
		{
			modesStr = parseSupportedModes(parseSupportedModesTestString);
			return;
		}

		string executable = System.IO.Path.Combine(Util.GetPrefixDir(), "bin/ffmpeg.exe");
		//ffmpeg -f dshow -list_options true -i video="USB 2.0 WebCamera"
		List<string> parameters = new List<string>();
		parameters.Add("-f");
		parameters.Add("dshow");
		parameters.Add("-list_options");
		parameters.Add("true");
		parameters.Add("-i");
		parameters.Add("video=" + cameraCode);
		ExecuteProcess.Result execute_result = ExecuteProcess.run (executable, parameters, true, true);

		//TODO: check if ffmpeg installed, but take care because right now this always gets error, so we need to not return
		/*
		   if(! execute_result.success) {
		   new DialogMessage("Chronojump - Modes of this webcam",
		   Constants.MessageTypes.WARNING, "Need to install ffmpeg");
		   return;
		   }
		   */

		//modesStr = execute_result.stdout;
		//modesStr = execute_result.allOutput;
		modesStr = parseSupportedModes(execute_result.allOutput);
	}

	//TODO: have a class that sorts resolutions and framerates
	protected override string parseSupportedModes(string allOutput)
	{
		/*
		 * break the big string in \n strings
		 * https://stackoverflow.com/a/1547483
		 */
		string currentPixelFormat = "";
		string[] lines = allOutput.Split(
				new[] { Environment.NewLine },
				StringSplitOptions.None
				);

		foreach(string l in lines)
		{
			LogB.Information("line: " + l);
			if(l.Contains("pixel_format="))
			{
				string pixelFormat = parsePixelFormat(l);
				if(pixelFormat != currentPixelFormat)
				{
					wsmList = new WebcamSupportedModesList(pixelFormat);
					wsmListOfLists.Add(wsmList);
					currentPixelFormat = pixelFormat;
				}

				parseSupportedMode(l);
			}
		}

		return printListOfLists();
	}

	private void parseSupportedMode(string l)
	{
		//match this:
		//pixel_format=yuyv422  min s=640x480 fps=5 max s=640x480 fps=30
		Match match = Regex.Match(l, @"max\s+s=(\d+)x(\d+)\s+fps=(\d+)");

		LogB.Information("match group count is 4?", (match.Groups.Count == 4).ToString());

		if(match.Groups.Count == 4)
		{
			//return string.Format("{0}x{1} \t{2} fps", match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
			string resolutionStr = string.Format("{0}x{1}", match.Groups[1].Value, match.Groups[2].Value);
			WebcamSupportedMode currentMode = null;
			if(wsmList.ModeExist(resolutionStr))
				currentMode = wsmList.GetMode(resolutionStr);
			else {
				currentMode = new WebcamSupportedMode(resolutionStr);
				wsmList.Add(currentMode);
			}

			string framerate = string.Format("{0}", match.Groups[3].Value);

			currentMode.AddFramerate(framerate);
		}

		return;
	}

	private string parsePixelFormat(string l)
	{
		Match match = Regex.Match(l, @"pixel_format=(\S+)\s+");
		if(match.Groups.Count == 2)
			return string.Format("{0}", match.Groups[1].Value);

		return "";
	}

	// test ParseSupportModes (unsorted to check if sorts well)
	private string parseSupportedModesTestString = @"
pixel_format=uyyv422  min s=176x144 fps=5 max s=176x144 fps=30
pixel_format=uyyv422  min s=160x120 fps=5 max s=160x120 fps=30
pixel_format=uyyv422  min s=320x240 fps=5 max s=320x240 fps=30";
}

public class WebcamFfmpegSupportedModesMac : WebcamFfmpegSupportedModes
{
	private WebcamSupportedModesList wsmList;

	public WebcamFfmpegSupportedModesMac(string cameraCode)
	{
		initialize();
		this.cameraCode = cameraCode;
	}

	public override void GetModes()
	{
		bool testParsing = false; //change it to true to test the parsing method
		if(testParsing)
		{
			modesStr = parseSupportedModes(parseSupportedModesTestString);
			return;
		}

		//select and impossible mode just to get an error on mac, this error will give us the "Supported modes"
		Webcam webcamPlay = new WebcamFfmpeg (Webcam.Action.PLAYPREVIEW, UtilAll.GetOSEnum(),
				cameraCode, "8000x8000", "8000");

		Webcam.Result result = webcamPlay.PlayPreviewNoBackgroundWantStdoutAndStderr();
		modesStr = parseSupportedModes(result.output);
	}

	protected override string parseSupportedModes(string allOutput)
	{
		/*
		 * break the big string in \n strings
		 * https://stackoverflow.com/a/1547483
		 */
		string[] lines = allOutput.Split(
				new[] { Environment.NewLine },
				StringSplitOptions.None
				);

		bool started = false;
		//on mac seems there is only one pixel format
		wsmList = new WebcamSupportedModesList("");
		wsmListOfLists.Add(wsmList);
		foreach(string l in lines)
		{
			LogB.Information("line: " + l);

			//devices start after the videoDevString line
			if(! started)
			{
				if(l.Contains("Supported modes"))
					started = true;

				continue;
			}

			parseSupportedMode(l);

			//after the list of video devices comes the list of audio devices, skip it
			if(l.Contains("Input/output"))
				break;
		}

		return printListOfLists();
	}

	private void parseSupportedMode(string l)
	{
		if(! l.Contains("avfoundation"))
			return;

		//parse this:
		//	[avfoundation @ 0x7f849a8be800]   1280x720@[23.999981 23.999981]fps
		//use: https://regex101.com/r/lZ5mN8/50
		// 	(\d+)x(\d+)@\[(\d+).(\d+)\s+

		Match match = Regex.Match(l, @"(\d+)x(\d+)@\[(\d+).(\d+)\s+");

		//TODO: use these lines
		//LogB.Information("match group count: ", match.Groups.Count.ToString());
		//if(match.Groups.Count != 5) //first is all match, second is the first int (width), last one is the decimals of the resolution
		//	return "";
		LogB.Information("match group count is 5?", (match.Groups.Count == 5).ToString());
		LogB.Information("match group count is -5?", (match.Groups.Count == -5).ToString());

		string resolutionStr = string.Format("{0}x{1}", match.Groups[1].Value, match.Groups[2].Value);
		WebcamSupportedMode currentMode = null;
		if(wsmList.ModeExist(resolutionStr))
			currentMode = wsmList.GetMode(resolutionStr);
		else {
			currentMode = new WebcamSupportedMode(resolutionStr);
			wsmList.Add(currentMode);
		}

		string framerate = string.Format("{0}.{1}", match.Groups[3].Value, match.Groups[4].Value);
		currentMode.AddFramerate(framerate);
	}

	// test ParseSupportModes
	private string parseSupportedModesTestString = @"Supported modes:
[avfoundation @ 0x7f849a8be800]   160x120@[29.970000 29.970000]fps
[avfoundation @ 0x7f849a8be800]   160x120@[25.000000 25.000000]fps
[avfoundation @ 0x7f849a8be800]   160x120@[23.999981 23.999981]fps
[avfoundation @ 0x7f849a8be800]   160x120@[14.999993 14.999993]fps
[avfoundation @ 0x7f849a8be800]   176x144@[29.970000 29.970000]fps
[avfoundation @ 0x7f849a8be800]   176x144@[25.000000 25.000000]fps
[avfoundation @ 0x7f849a8be800]   176x144@[23.999981 23.999981]fps
[avfoundation @ 0x7f849a8be800]   176x144@[14.999993 14.999993]fps
[avfoundation @ 0x7f849a8be800]   320x240@[29.970000 29.970000]fps
[avfoundation @ 0x7f849a8be800]   320x240@[25.000000 25.000000]fps
[avfoundation @ 0x7f849a8be800]   320x240@[23.999981 23.999981]fps
[avfoundation @ 0x7f849a8be800]   320x240@[14.999993 14.999993]fps
[avfoundation @ 0x7f849a8be800]   352x288@[29.970000 29.970000]fps
[avfoundation @ 0x7f849a8be800]   352x288@[25.000000 25.000000]fps
[avfoundation @ 0x7f849a8be800]   352x288@[23.999981 23.999981]fps
[avfoundation @ 0x7f849a8be800]   352x288@[14.999993 14.999993]fps
[avfoundation @ 0x7f849a8be800]   640x480@[29.970000 29.970000]fps
[avfoundation @ 0x7f849a8be800]   640x480@[25.000000 25.000000]fps
[avfoundation @ 0x7f849a8be800]   640x480@[23.999981 23.999981]fps
[avfoundation @ 0x7f849a8be800]   640x480@[14.999993 14.999993]fps
[avfoundation @ 0x7f849a8be800]   960x540@[29.970000 29.970000]fps
[avfoundation @ 0x7f849a8be800]   960x540@[25.000000 25.000000]fps
[avfoundation @ 0x7f849a8be800]   960x540@[23.999981 23.999981]fps
[avfoundation @ 0x7f849a8be800]   960x540@[14.999993 14.999993]fps
[avfoundation @ 0x7f849a8be800]   1024x576@[29.970000 29.970000]fps
[avfoundation @ 0x7f849a8be800]   1024x576@[25.000000 25.000000]fps
[avfoundation @ 0x7f849a8be800]   1024x576@[23.999981 23.999981]fps
[avfoundation @ 0x7f849a8be800]   1024x576@[14.999993 14.999993]fps
[avfoundation @ 0x7f849a8be800]   1280x720@[29.970000 29.970000]fps
[avfoundation @ 0x7f849a8be800]   1280x720@[25.000000 25.000000]fps
[avfoundation @ 0x7f849a8be800]   1280x720@[23.999981 23.999981]fps
[avfoundation @ 0x7f849a8be800]   1280x720@[14.999993 14.999993]fps
0: Input/output error";
}

public class WebcamSupportedModesList
{
	List<WebcamSupportedMode> l;
	string pixelFormat;

	//new constructor, only linux at the moment
	public WebcamSupportedModesList (string pixelFormat)
	{
		this.pixelFormat = pixelFormat;

		l = new List<WebcamSupportedMode>();
	}
	//old constructor: win and mac now
	public WebcamSupportedModesList ()
	{
		l = new List<WebcamSupportedMode>();
	}

	public bool ModeExist (string resolutionStr)
	{
		WebcamSupportedMode wsmNew = new WebcamSupportedMode(resolutionStr);
		foreach (WebcamSupportedMode wsm in l)
			if(wsm.ResolutionWidth == wsmNew.ResolutionWidth &&
					wsm.ResolutionHeight == wsmNew.ResolutionHeight)
				return true;

		return false;
	}

	//used if ModeExist()
	public WebcamSupportedMode GetMode (string resolutionStr)
	{
		WebcamSupportedMode wsmNew = new WebcamSupportedMode(resolutionStr);
		foreach (WebcamSupportedMode wsm in l)
			if(wsm.ResolutionWidth == wsmNew.ResolutionWidth &&
					wsm.ResolutionHeight == wsmNew.ResolutionHeight)
				return wsm;

		return null;
	}

	public void Add (WebcamSupportedMode wsm)
	{
		l.Add(wsm);
	}

	public bool HasRecords ()
	{
		return (l.Count > 0);
	}

	public void Sort()
	{
		WebcamSupportedModeSort wsms = new WebcamSupportedModeSort();
		l.Sort(wsms);
	}

	public override string ToString()
	{
		string str = "";
		foreach(WebcamSupportedMode wsm in l)
			str += wsm.ToString() + "\n";

		return str;
	}

	public string PixelFormat
	{
		get { return pixelFormat; }
	}

	~WebcamSupportedModesList() {}
}

//https://www.geeksforgeeks.org/how-to-sort-list-in-c-sharp-set-1/
//example 2
public class WebcamSupportedModeSort : IComparer<WebcamSupportedMode>
{
	public int Compare(WebcamSupportedMode x, WebcamSupportedMode y)
	{
		if(x == null || y == null)
			return 0;

		return (x.Size).CompareTo(y.Size);
	}
}
public class WebcamSupportedModeSortFramerates : IComparer<string>
{
	public int Compare(string x, string y)
	{
		if(x == null || y == null)
			return 0;

		return Convert.ToDouble(Util.ChangeDecimalSeparator(x)).CompareTo(Convert.ToDouble(Util.ChangeDecimalSeparator(y)));
	}
}

public class WebcamSupportedMode
{
	int resolutionWidth;
	int resolutionHeight;
	List<string> framerates;

	public WebcamSupportedMode (string resolutionStr)
	{
		string [] strFull = resolutionStr.Split(new char[] {'x'});
		if(strFull.Length == 2)
		{
			this.resolutionWidth = Convert.ToInt32(strFull[0]);
			this.resolutionHeight = Convert.ToInt32(strFull[1]);
		}
		framerates = new List<string>();
	}

	public void AddFramerate (string f)
	{
		framerates.Add(f);
	}

	public override string ToString()
	{
		string str = string.Format("\nResolution: {0}x{1}\nFramerates: ", resolutionWidth, resolutionHeight);

		/*
		 * unsorted:
		foreach (string framerate in framerates)
			str += string.Format("\t{0}", framerate);
		*/

		//"\nSorting:";
		WebcamSupportedModeSortFramerates wsmsf = new WebcamSupportedModeSortFramerates();
		framerates.Sort(wsmsf);
		string sepChar = "";
		foreach (string framerate in framerates)
		{
			str += string.Format("{0}{1}", sepChar, framerate);
			sepChar = "; ";
		}

		return str;
	}

	public int CompareTo( WebcamSupportedMode that )
	{
		if ( that == null ) return 1;
		if ( this.Size > that.Size) return 1;
		if ( this.Size < that.Size) return -1;
		return 0;
	}

	public int ResolutionWidth
	{
		get { return resolutionWidth; }
	}
	public int ResolutionHeight
	{
		get { return resolutionHeight; }
	}

	public int Size
	{
		get { return resolutionWidth * resolutionHeight; }
	}

	~WebcamSupportedMode() {}
}
