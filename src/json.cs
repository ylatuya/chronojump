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
 * Copyright (C) 2016-2017 Carles Pina & Xavier de Blas
 */

using System;
using System.Net;
using System.Web;
using System.IO;
using System.Json;
using System.Text;
using System.Collections;
using System.Collections.Generic; //Dictionary
using Mono.Unix;


public class Json
{
	public string ResultMessage;
	static string serverUrl = "http://api.chronojump.org:8080";
	//string serverUrl = "http://192.168.200.1:8080";

	public static void ChangeServerUrl(string url)
	{
		serverUrl = url;
	}

	public Json()
	{
		ResultMessage = "";
	}

	public bool PostCrashLog(string email, string comments) 
	{
		string filePath = UtilAll.GetLogFileOld();

		if(! File.Exists(filePath)) {
			this.ResultMessage = Catalog.GetString("Could not send file.\nIt does not exist.");
			return false;
		}

		if(comments != null && comments != "")
			Util.InsertTextBeginningOfFile(
					"----------\nUser comments:\n" + comments + "\n----------\n", filePath);

		// Create a request using a URL that can receive a post. 
		WebRequest request = WebRequest.Create (serverUrl + "/backtrace/" + UtilAll.ReadVersionFromBuildInfo() + "-" + email);

		// Set the Method property of the request to POST.
		request.Method = "POST";

		// Create POST data and convert it to a byte array.
		byte[] byteArray = readFile(filePath);

		// Set the ContentType property of the WebRequest.
		request.ContentType = "application/x-www-form-urlencoded";

		// Set the ContentLength property of the WebRequest.
		request.ContentLength = byteArray.Length;

		// Get the request stream.
		Stream dataStream;
		try {
			dataStream = request.GetRequestStream ();
		} catch {
			LogB.Warning("Error sending datastream");
			this.ResultMessage = Catalog.GetString("Could not send file.") + "\n" + 
				string.Format(Catalog.GetString("You are not connected to the Internet\nor {0} server is down."),
						serverUrl);
			return false;
		}

		// Write the data to the request stream.
		dataStream.Write (byteArray, 0, byteArray.Length);

		// Close the Stream object.
		dataStream.Close ();
        
		// Get the response.
		WebResponse response;
		try {
			response = request.GetResponse ();
		} catch {
			LogB.Warning("Error getting response");
			this.ResultMessage = Catalog.GetString("Could not send file.") + "\n" + 
				string.Format(Catalog.GetString("You are not connected to the Internet\nor {0} server is down."),
						serverUrl);
			return false;
		}

		// Display the status.
		LogB.Information(((HttpWebResponse)response).StatusDescription);

		// Get the stream containing content returned by the server.
		dataStream = response.GetResponseStream ();

		// Open the stream using a StreamReader for easy access.
		StreamReader reader = new StreamReader (dataStream);

		// Read the content.
		string responseFromServer = reader.ReadToEnd ();

		// Display the content.
		LogB.Information(responseFromServer);

		// Clean up the streams.
		reader.Close ();
		dataStream.Close ();
		response.Close ();    


		JsonValue result = JsonValue.Parse(responseFromServer);
		string crash_id = result["crash_id"];
		LogB.Information("crash_id: ", crash_id);

		this.ResultMessage = Catalog.GetString("Log sent. Thank you.");
		return true;
	}
	
	private byte[] readFile(string filePath)
	{
		return System.IO.File.ReadAllBytes(filePath); 
	}


	//public bool ChronojumpUpdated = true;
	public bool GetLastVersion(string currentVersion) 
	{
		// Create a request using a URL that can receive a post. 
		WebRequest request = WebRequest.Create (serverUrl + "/version");
		
		// Set the Method property of the request to GET.
		request.Method = "GET";
		
		// Set the ContentType property of the WebRequest.
		//request.ContentType = "application/x-www-form-urlencoded";
		
		HttpWebResponse response;
		try {
			response = (HttpWebResponse) request.GetResponse();
		} catch {
			this.ResultMessage = 
				Catalog.GetString("Could not get last version.") + "\n" +
				string.Format(Catalog.GetString("You are not connected to the Internet\nor {0} server is down."), 
				serverUrl);
			return false;
		}

		string responseFromServer;
		using (var sr = new StreamReader(response.GetResponseStream()))
		{
			responseFromServer = sr.ReadToEnd();
		}

		//this prints:
		// {"stable": "1.4.9"}
		//this.ResultMessage = "Last version published: " + responseFromServer;
		
		string [] strFull = responseFromServer.Split(new char[] {':'});
		int startPos = strFull[1].IndexOf('"') +1;
		int endPos = strFull[1].LastIndexOf('"') -2;

		string lastVersionPublished = strFull[1].Substring(startPos,endPos); //1.4.9
			
		string updateStr = "";
		if(currentVersion != lastVersionPublished)
			updateStr = "\n\n" + Catalog.GetString("Update software at ") + "www.chronojump.org";
			
		this.ResultMessage =		
			Catalog.GetString("Installed version is: ") + currentVersion + "\n" + 
			Catalog.GetString("Last version published: ") + lastVersionPublished +
			updateStr;
		
		//ChronojumpUpdated = (currentVersion == ResultMessage);

		return true;
	}

	/*
	 * if software just started, ping gets stuck by network problems, and user try to exit software,
	 * thread.Abort doesn't kill the thread properly
	 * just kill the webRequest
	 */
	WebRequest requestPing;
	bool requestPingAborting;

	public void PingAbort()
	{
		requestPingAborting = true;
		requestPing.Abort(); //cancel an asynchronous request
	}
	public bool Ping(string osVersion, string cjVersion, string machineID) 
	{
		requestPingAborting = false;

		// Create a request using a URL that can receive a post. 
		requestPing = WebRequest.Create (serverUrl + "/ping");

		// Set the Method property of the request to POST.
		requestPing.Method = "POST";

		// Set the ContentType property of the WebRequest.
		requestPing.ContentType = "application/json";

		// Creates the json object
		JsonObject json = new JsonObject();
		json.Add("os_version", osVersion);
		json.Add("cj_version", cjVersion);
		json.Add("machine_id", machineID);

		// Converts it to a String
		String js = json.ToString();

		// Writes the json object into the request dataStream
		Stream dataStream;
		try {
			dataStream = requestPing.GetRequestStream ();
		} catch {
			this.ResultMessage = 
				string.Format(Catalog.GetString("You are not connected to the Internet\nor {0} server is down."), 
				serverUrl);
			return false;
		}
		if(requestPingAborting) {
			LogB.Information("Aborted from PingAbort");
			return false;
		}

		dataStream.Write (Encoding.UTF8.GetBytes(js), 0, js.Length);

		dataStream.Close ();

		// Get the response.
		WebResponse response;
		try {
			response = requestPing.GetResponse ();
		} catch {
			this.ResultMessage = 
				string.Format(Catalog.GetString("You are not connected to the Internet\nor {0} server is down."), 
				serverUrl);
			return false;
		}
		if(requestPingAborting) {
			LogB.Information("Aborted from PingAbort");
			return false;
		}

		// Display the status (will be 201, CREATED)
		Console.WriteLine (((HttpWebResponse)response).StatusDescription);

		// Clean up the streams.
		dataStream.Close ();
		response.Close ();
		
		this.ResultMessage = "Ping sent.";
		return true;
	}

	public Person GetPersonByRFID(string rfid)
	{
		Person person = new Person(-1);

		// Create a request using a URL that can receive a post.
		WebRequest request = WebRequest.Create (serverUrl + "/getPersonByRFID");

		// Set the Method property of the request to POST.
		request.Method = "POST";

		// Set the ContentType property of the WebRequest.
		request.ContentType = "application/json; Charset=UTF-8"; //but this is not enough, see this line:

		// Creates the json object
		JsonObject json = new JsonObject();
		json.Add("rfid", rfid);
		
		// Converts it to a String
		String js = json.ToString();

		// Writes the json object into the request dataStream
		Stream dataStream;
		try {
			dataStream = request.GetRequestStream ();
		} catch {
			this.ResultMessage =
				string.Format(Catalog.GetString("You are not connected to the Internet\nor {0} server is down."),
				serverUrl);
			return person;
		}

		dataStream.Write (Encoding.UTF8.GetBytes(js), 0, js.Length);

		dataStream.Close ();
		
		HttpWebResponse response;
		try {
			response = (HttpWebResponse) request.GetResponse();
		} catch {
			this.ResultMessage = 
				string.Format(Catalog.GetString("You are not connected to the Internet\nor {0} server is down."), 
				serverUrl);
			return person;
		}

		string responseFromServer;
		using (var sr = new StreamReader(response.GetResponseStream()))
		{
			responseFromServer = sr.ReadToEnd();
		}

		LogB.Information("GetPersonByRFID: " + responseFromServer);
		
		if(responseFromServer == "")
			LogB.Information(" Empty "); //never happens
		else if(responseFromServer == "[]")
			LogB.Information(" Empty2 "); //when rfid is not on server
		else {
			//patheticPersonDeserialize("[[2, \"(playername)\", 82.0, \"253,20,150,13\", \"\"]]");
			//patheticPersonDeserialize("[[2, \"(playername)\", 82.0, \"253,20,150,13\", \"jugadors/player.jpg\"]]");
			person = personDeserialize(responseFromServer);
		}

		return person;

	}
	public double LastPersonByRFIDWeight = 0;
	public string LastPersonByRFIDImageURL = "";
	private Person personDeserialize(string strPeople)
	{
		JsonValue jsonPeople = JsonValue.Parse(strPeople);

		// We receive a list of people but we are interested only on the first one (?)
		JsonValue person = jsonPeople [0];

		Int32 id = person [0];
		string player = person [1];
		double weight = person [2];
		double height = person [3];
		string rfid = person [4];
		string image = person [5];

		LastPersonByRFIDWeight = weight;
		LastPersonByRFIDImageURL = image;

		return new Person(id, player, rfid);
	}


	//to retrieve images from flask (:5050)
	private string getImagesUrl()
	{
		int posOfLastColon = serverUrl.LastIndexOf(':');
		return serverUrl.Substring(0, posOfLastColon) + ":5000/static/images/";
	}

	//imageHalfUrl is "jugadors/*.jpg"
	public bool DownloadImage(string imageHalfUrl, int personID)
	{
		try {
			using (WebClient client = new WebClient())
			{
				LogB.Information ("DownloadImage!!");
				LogB.Information (getImagesUrl() + imageHalfUrl);
				LogB.Information (Path.Combine(Path.GetTempPath(), personID.ToString()));
				client.DownloadFile(new Uri(getImagesUrl() + imageHalfUrl),
						Path.Combine(Path.GetTempPath(), personID.ToString()));
			}
		} catch {
			LogB.Warning("DownloadImage catched");
			return false;
		}

		return true;
	}

	public List<Task> GetTasks(int personID)
	{
		// Create a request using a URL that can receive a post.
		WebRequest request = WebRequest.Create (serverUrl + "/getTasks");

		// Set the Method property of the request to POST.
		request.Method = "POST";

		// Set the ContentType property of the WebRequest.
		request.ContentType = "application/json; Charset=UTF-8"; //but this is not enough, see this line:

		// Creates the json object
		JsonObject json = new JsonObject();
		json.Add("personId", personID.ToString());

		// Converts it to a String
		String js = json.ToString();

		// Writes the json object into the request dataStream
		Stream dataStream;
		try {
			dataStream = request.GetRequestStream ();
		} catch {
			this.ResultMessage =
				string.Format(Catalog.GetString("You are not connected to the Internet\nor {0} server is down."),
				serverUrl);
			return new List<Task>();
		}

		dataStream.Write (Encoding.UTF8.GetBytes(js), 0, js.Length);
		dataStream.Close ();

		HttpWebResponse response;
		try {
			response = (HttpWebResponse) request.GetResponse();
		} catch {
			this.ResultMessage =
				string.Format(Catalog.GetString("You are not connected to the Internet\nor {0} server is down."), 
				serverUrl);
			return new List<Task>();
		}

		string responseFromServer;
		using (var sr = new StreamReader(response.GetResponseStream()))
		{
			responseFromServer = sr.ReadToEnd();
		}

		LogB.Information("GetTasks: " + responseFromServer);

		if(responseFromServer == "" || responseFromServer == "[]")
		{
			LogB.Information(" Empty ");
			return new List<Task>();
		}

		return patheticTasksDeserialize(responseFromServer);
	}
	private List<Task> patheticTasksDeserialize(string responseFromServer)
	{
		List<Task> list = new List<Task>();

		// 	[[1, "one task"], [3, "another task"]]

		//1) convert it to:
		// 	[1, "one task"], [3, "another task"]
		responseFromServer = responseFromServer.Substring(1, responseFromServer.Length -2);

		string [] strFull = responseFromServer.Split(new char[] {']'});
		foreach(string str in strFull)
		{
			if(str == null || str == "")
				continue;

			string s = str;
			LogB.Information("before: " + s);
			if(s.StartsWith(", ["))
				s = s.Substring(3);
			else
				s = s.Substring(1);

			//don't use this because comments can have a comma
			//string [] s2 = s.Split(new char[] {','});

			//get the first comma
			int sepPos = s.IndexOf(',');
			string sId = s.Substring(0, sepPos);
			string sComment = s.Substring(sepPos +1);
			sComment = sComment.Substring(2, sComment.Length -3); 	//remove initial ' "' and end '"'

			list.Add(new Task(Convert.ToInt32(sId), sComment));
		}
		return list;
	}

	public bool UpdateTask(int taskId, int done)
	{
		// Create a request using a URL that can receive a post.
		WebRequest request = WebRequest.Create (serverUrl + "/updateTask");

		// Set the Method property of the request to POST.
		request.Method = "POST";

		// Set the ContentType property of the WebRequest.
		request.ContentType = "application/json; Charset=UTF-8"; //but this is not enough, see this line:

		// Creates the json object
		JsonObject json = new JsonObject();

		json.Add("taskId", taskId);
		json.Add("done", done);

		// Converts it to a String
		String js = json.ToString();

		// Writes the json object into the request dataStream
		Stream dataStream;
		try {
			dataStream = request.GetRequestStream ();
		} catch {
			this.ResultMessage =
				string.Format(Catalog.GetString("You are not connected to the Internet\nor {0} server is down."),
				serverUrl);
			return false;
		}

		dataStream.Write (Encoding.UTF8.GetBytes(js), 0, js.Length);

		dataStream.Close ();

		// Get the response.
		WebResponse response;
		try {
			response = request.GetResponse ();
		} catch {
			this.ResultMessage =
				string.Format(Catalog.GetString("You are not connected to the Internet\nor {0} server is down."),
				serverUrl);
			return false;
		}

		// Display the status (will be 202, CREATED)
		Console.WriteLine (((HttpWebResponse)response).StatusDescription);

		// Clean up the streams.
		dataStream.Close ();
		response.Close ();

		this.ResultMessage = "Update task sent.";
		return true;
	}

	/*
	public bool UploadEncoderData()
	{
		return UploadEncoderData(1, 1, "40.2", "lateral", "8100.5", 8);
	}
	*/
	public bool UploadEncoderData(int personId, int machineId, string exerciseName, string resistance, UploadEncoderDataObject uo)
	{
		// Create a request using a URL that can receive a post.
		WebRequest request = WebRequest.Create (serverUrl + "/uploadEncoderData");

		// Set the Method property of the request to POST.
		request.Method = "POST";

		// Set the ContentType property of the WebRequest.
		request.ContentType = "application/json; Charset=UTF-8"; //but this is not enough, see this line:
		exerciseName = Util.RemoveAccents(exerciseName);

		// Creates the json object
		JsonObject json = new JsonObject();

		json.Add("personId", personId);
		json.Add("machineId", machineId);
		json.Add("exerciseName", exerciseName);
		json.Add("resistance", resistance);
		json.Add("repetitions", uo.repetitions);

		json.Add("numBySpeed", uo.numBySpeed);
		json.Add("lossBySpeed", uo.lossBySpeed);
		json.Add("rangeBySpeed", uo.rangeBySpeed);
		json.Add("vmeanBySpeed", uo.vmeanBySpeed);
		json.Add("vmaxBySpeed", uo.vmaxBySpeed);
		json.Add("pmeanBySpeed", uo.pmeanBySpeed);
		json.Add("pmaxBySpeed", uo.pmaxBySpeed);

		json.Add("numByPower", uo.numByPower);
		json.Add("lossByPower", uo.lossByPower);
		json.Add("rangeByPower", uo.rangeByPower);
		json.Add("vmeanByPower", uo.vmeanByPower);
		json.Add("vmaxByPower", uo.vmaxByPower);
		json.Add("pmeanByPower", uo.pmeanByPower);
		json.Add("pmaxByPower", uo.pmaxByPower);

		// Converts it to a String
		String js = json.ToString();

		// Writes the json object into the request dataStream
		Stream dataStream;
		try {
			dataStream = request.GetRequestStream ();
		} catch {
			this.ResultMessage =
				string.Format(Catalog.GetString("You are not connected to the Internet\nor {0} server is down."),
				serverUrl);
			return false;
		}

		dataStream.Write (Encoding.UTF8.GetBytes(js), 0, js.Length);

		dataStream.Close ();

		// Get the response.
		WebResponse response;
		try {
			response = request.GetResponse ();
		} catch {
			this.ResultMessage =
				string.Format(Catalog.GetString("You are not connected to the Internet\nor {0} server is down."),
				serverUrl);
			return false;
		}

		// Display the status (will be 202, CREATED)
		Console.WriteLine (((HttpWebResponse)response).StatusDescription);

		// Clean up the streams.
		dataStream.Close ();
		response.Close ();

		this.ResultMessage = "Encoder data sent.";
		return true;
	}


	~Json() {}
}

class JsonUtils
{
	public static JsonValue valueOrDefault(JsonValue jsonObject, string key, string defaultValue)
	{
		// Returns jsonObject[key] if it exists. If the key doesn't exist returns defaultValue and
		// logs the anomaly into the Chronojump log.
		if (jsonObject.ContainsKey (key)) {
			return jsonObject [key];
		} else {
			LogB.Information ("JsonUtils::valueOrDefault: returning default (" + defaultValue + ") from JSON: " + jsonObject.ToString ());
			return defaultValue;
		}
	}
}

public class UploadEncoderDataObject
{
	private enum byTypes { SPEED, POWER }

	public int repetitions;

	//variables calculated BySpeed (by best mean speed)
	public int numBySpeed;
	public int lossBySpeed;
	public string rangeBySpeed; //strings with . as decimal point
	public string vmeanBySpeed;
	public string vmaxBySpeed;
	public string pmeanBySpeed;
	public string pmaxBySpeed;

	//variables calculated ByPower (by best mean power)
	public int numByPower;
	public int lossByPower;
	public string rangeByPower; //strings with . as decimal point
	public string vmeanByPower;
	public string vmaxByPower;
	public string pmeanByPower;
	public string pmaxByPower;

	public UploadEncoderDataObject(ArrayList curves)
	{
		repetitions = curves.Count;

		int nSpeed = getBestRep(curves, byTypes.SPEED);
		int nPower = getBestRep(curves, byTypes.POWER);

		EncoderCurve curveBySpeed = (EncoderCurve) curves[nSpeed];
		EncoderCurve curveByPower = (EncoderCurve) curves[nPower];

		rangeBySpeed = Util.ConvertToPoint(curveBySpeed.Height);
		rangeByPower = Util.ConvertToPoint(curveByPower.Height);

		vmeanBySpeed = Util.ConvertToPoint(curveBySpeed.MeanSpeed);
		vmeanByPower = Util.ConvertToPoint(curveByPower.MeanSpeed);
		vmaxBySpeed = Util.ConvertToPoint(curveBySpeed.MaxSpeed);
		vmaxByPower = Util.ConvertToPoint(curveByPower.MaxSpeed);

		pmeanBySpeed = Util.ConvertToPoint(curveBySpeed.MeanPower);
		pmeanByPower = Util.ConvertToPoint(curveByPower.MeanPower);
		pmaxBySpeed = Util.ConvertToPoint(curveBySpeed.PeakPower);
		pmaxByPower = Util.ConvertToPoint(curveByPower.PeakPower);

		//add +1 to show to user
		numBySpeed = nSpeed + 1;
		numByPower = nPower + 1;

		lossBySpeed = getLoss(curves, byTypes.SPEED);
		lossByPower = getLoss(curves, byTypes.POWER);
	}

	private int getBestRep(ArrayList curves, byTypes by)
	{
		int curveNum = 0;
		int i = 0;
		double highest = 0;

		foreach (EncoderCurve curve in curves)
		{
			double compareTo = curve.MeanSpeedD;
			if(by == byTypes.POWER)
				compareTo = curve.MeanPowerD;

			if(compareTo > highest)
			{
				highest = compareTo;
				curveNum = i;
			}
			i ++;
		}
		return curveNum;
	}

	private int getLoss(ArrayList curves, byTypes by)
	{
		double lowest = 100000;
		double highest = 0;

		foreach (EncoderCurve curve in curves)
		{
			double compareTo = curve.MeanSpeedD;
			if(by == byTypes.POWER)
				compareTo = curve.MeanPowerD;

			if(compareTo < lowest)
				lowest = compareTo;
			if(compareTo > highest)
				highest = compareTo;
		}
		return Convert.ToInt32(Util.DivideSafeFraction(100.0 * (highest - lowest), highest));
	}
}

public class Task
{
	public int Id;
	public string Comment;

	public Task()
	{
		Id = -1;
		Comment = "";
	}

	public Task(int id, string comment)
	{
		Id = id;
		Comment = comment;
	}

	public override string ToString()
	{
		return Id.ToString() + ": " + Comment;
	}
}
