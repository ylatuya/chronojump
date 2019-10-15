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
 * Copyright (C) 2017   Xavier de Blas <xaviblas@gmail.com> 
 */

using System;
//using System.Data;
using System.Collections;
using System.Collections.Generic; //List<T>
using System.IO; //DirectoryInfo
using Mono.Data.Sqlite;
using System.Text.RegularExpressions; //Regex
using Mono.Unix;

class SqliteForceSensor : Sqlite
{
	private static string table = Constants.ForceSensorTable;

	public SqliteForceSensor() {
	}

	~SqliteForceSensor() {}

	/*
	 * create and initialize tables
	 */

	protected internal static void createTable()
	{
		dbcmd.CommandText =
			"CREATE TABLE " + table + " ( " +
			"uniqueID INTEGER PRIMARY KEY, " +
			"personID INT, " +
			"sessionID INT, " +
			"exerciseID INT, " +
			"captureOption TEXT, " + //ForceSensor.CaptureOptions {NORMAL, ABS, INVERTED}
			"angle INT, " + 	//angle can be different than the defaultAngle on exercise
			"laterality TEXT, " +	//"Both" "Right" "Left". stored in english
			"filename TEXT, " +
			"url TEXT, " +		//URL of data files. stored as relative
			"datetime TEXT, " + 	//2019-07-11_15-01-44
			"comments TEXT, " +
			"videoURL TEXT)";	//URL of video of signals. stored as relative
		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();
	}

	public static int Insert (bool dbconOpened, string insertString)
	{
		openIfNeeded(dbconOpened);

		LogB.Information("goint to insert: " + insertString);
		dbcmd.CommandText = "INSERT INTO " + table +
				" (uniqueID, personID, sessionID, exerciseID, captureOption, angle, laterality, filename, url, dateTime, comments, videoURL)" +
				" VALUES " + insertString;
		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery(); //TODO uncomment this again

		string myString = @"select last_insert_rowid()";
		dbcmd.CommandText = myString;
		int myLast = Convert.ToInt32(dbcmd.ExecuteScalar()); // Need to type-cast since `ExecuteScalar` returns an object.

		closeIfNeeded(dbconOpened);

		return myLast;
	}

	public static void Update (bool dbconOpened, string updateString)
	{
		openIfNeeded(dbconOpened);

		dbcmd.CommandText = "UPDATE " + table + " SET " + updateString;

		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		closeIfNeeded(dbconOpened);
	}

	public static void UpdateComments (bool dbconOpened, int uniqueID, string comments)
	{
		openIfNeeded(dbconOpened);

		dbcmd.CommandText = "UPDATE " + table + " SET comments = \"" + comments + "\"" +
			" WHERE uniqueID = " + uniqueID;

		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		closeIfNeeded(dbconOpened);
	}

	/* right now unused
	public static void DeleteSQLAndFile (bool dbconOpened, int uniqueID)
	{
		ForceSensor fs = (ForceSensor) Select (dbconOpened, uniqueID, -1, -1)[0];
		DeleteSQLAndFile (dbconOpened, fs);
	}
	*/
	public static void DeleteSQLAndFiles (bool dbconOpened, ForceSensor fs)
	{
		openIfNeeded(dbconOpened);

		dbcmd.CommandText = "DELETE FROM " + table + " WHERE uniqueID = " + fs.UniqueID;

		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		closeIfNeeded(dbconOpened);

		//delete the files
		Util.FileDelete(fs.FullURL);

		if(fs.FullVideoURL != "")
			Util.FileDelete(fs.FullVideoURL);
	}

	//SELECT forceSensor.*, forceSensorExercise.Name FROM forceSensor, forceSensorExercise WHERE forceSensor.exerciseID = forceSensorExercise.UniqueID ORDER BY forceSensor.uniqueID;
	public static ArrayList Select (bool dbconOpened, int uniqueID, int personID, int sessionID)
	{
		openIfNeeded(dbconOpened);

		string selectStr = "SELECT " + table + ".*, " + Constants.ForceSensorExerciseTable + ".Name FROM " + table + ", " + Constants.ForceSensorExerciseTable;
		string whereStr = " WHERE " + table + ".exerciseID = " + Constants.ForceSensorExerciseTable + ".UniqueID ";

		string uniqueIDStr = "";
		if(uniqueID != -1)
			uniqueIDStr = " AND " + table + ".uniqueID = " + uniqueID;

		string personIDStr = "";
		if(personID != -1)
			personIDStr = " AND " + table + ".personID = " + personID;

		string sessionIDStr = "";
		if(sessionID != -1)
			sessionIDStr = " AND " + table + ".sessionID = " + sessionID;

		dbcmd.CommandText = selectStr + whereStr + uniqueIDStr + personIDStr + sessionIDStr + " Order BY " + table + ".uniqueID";

		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		SqliteDataReader reader;
		reader = dbcmd.ExecuteReader();

		ArrayList array = new ArrayList(1);
		ForceSensor fs;

		while(reader.Read()) {
			fs = new ForceSensor (
					Convert.ToInt32(reader[0].ToString()),	//uniqueID
					Convert.ToInt32(reader[1].ToString()),	//personID
					Convert.ToInt32(reader[2].ToString()),	//sessionID
					Convert.ToInt32(reader[3].ToString()),	//exerciseID
					(ForceSensor.CaptureOptions) Enum.Parse(
						typeof(ForceSensor.CaptureOptions), reader[4].ToString()), 	//captureOption
					Convert.ToInt32(reader[5].ToString()),	//angle
					reader[6].ToString(),			//laterality
					reader[7].ToString(),			//filename
					Util.MakeURLabsolute(fixOSpath(reader[8].ToString())),	//url
					reader[9].ToString(),			//datetime
					reader[10].ToString(),			//comments
					reader[11].ToString(),			//videoURL
					reader[12].ToString()			//exerciseName
					);
			array.Add(fs);
		}

		reader.Close();
		closeIfNeeded(dbconOpened);

		return array;
	}

	public static ArrayList SelectRowsOfAnExercise(bool dbconOpened, int exerciseID)
	{
		openIfNeeded(dbconOpened);

		dbcmd.CommandText = "select count(*), " +
			Constants.PersonTable + ".name, " +
			Constants.SessionTable + ".name, " +
			Constants.SessionTable + ".date " +
			" FROM " + table + ", " + Constants.PersonTable + ", " + Constants.SessionTable +
			" WHERE exerciseID == " + exerciseID +
			" AND " + Constants.PersonTable + ".uniqueID == " + table + ".personID " +
		        " AND " + Constants.SessionTable + ".uniqueID == " + table + ".sessionID " +
			" GROUP BY sessionID, personID";

		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();
		SqliteDataReader reader;
		reader = dbcmd.ExecuteReader();

		ArrayList array = new ArrayList();
		int count = 0;
		while(reader.Read()) {
			array.Add(new string [] {
					count.ToString(),
					reader[0].ToString(), //count
					reader[1].ToString(), //person name
					reader[2].ToString(), //session name
					reader[3].ToString()  //session date
			});
			count ++;
		}

		reader.Close();
		closeIfNeeded(dbconOpened);

		return array;
	}

	public static ArrayList SelectSessionOverviewSets (bool dbconOpened, int sessionID)
	{
		if(! dbconOpened)
			Sqlite.Open();

		dbcmd.CommandText =
			"SELECT person77.name, person77.sex, forceSensorExercise.name, COUNT(*)" +
			" FROM person77, personSession77, forceSensorExercise, forceSensor" +
			" WHERE person77.uniqueID == forceSensor.personID AND personSession77.personID == forceSensor.personID AND personSession77.sessionID == forceSensor.sessionID AND forceSensorExercise.uniqueID==forceSensor.exerciseID AND forceSensor.sessionID == " + sessionID +
			" GROUP BY forceSensor.personID, exerciseID" +
			" ORDER BY person77.name";

		LogB.SQL(dbcmd.CommandText.ToString());

		SqliteDataReader reader;
		reader = dbcmd.ExecuteReader();

		ArrayList array = new ArrayList();
		while(reader.Read())
		{
			string [] s = {
				reader[0].ToString(), 	//person name
				reader[1].ToString(), 	//person sex
				reader[2].ToString(), 	//exercise name
				reader[3].ToString()	//sets count
			};
			array.Add (s);
		}

		reader.Close();
		if(! dbconOpened)
			Sqlite.Close();

		return array;
	}

	protected internal static void import_from_1_68_to_1_69() //database is opened
	{
		LogB.PrintAllThreads = true; //TODO: remove this
		LogB.Information("at import_from_1_68_to_1_69()");

		string forceSensorDir = Util.GetForceSensorDir();
		if(Sqlite.UpdatingDBFrom == Sqlite.UpdatingDBFromEnum.IMPORTED_SESSION)
			forceSensorDir = Path.Combine(Util.GetDatabaseTempImportDir(), "forceSensor");

		int unknownPersonID = Sqlite.ExistsAndGetUniqueID(true, Constants.PersonTable, Catalog.GetString("Unknown"));
		int unknownExerciseID = Sqlite.ExistsAndGetUniqueID(true, Constants.ForceSensorExerciseTable, Catalog.GetString("Unknown"));

		DirectoryInfo [] sessions = new DirectoryInfo(forceSensorDir).GetDirectories();
		foreach (DirectoryInfo session in sessions) //session.Name will be the UniqueID
		{
			FileInfo[] files = session.GetFiles();
			foreach (FileInfo file in files)
			{
				string fileWithoutExtension = Util.RemoveExtension(Util.GetLastPartOfPath(file.Name));
				ForceSensorLoadTryToAssignPersonAndMore fslt =
					new ForceSensorLoadTryToAssignPersonAndMore(true, fileWithoutExtension, Convert.ToInt32(session.Name));

				Person p = fslt.GetPerson();
				//if person is not foundz
				if(p.UniqueID == -1)
				{
					if(unknownPersonID == -1)
					{
//TODO: atencio pq aixo no s'esta insertant al final, s'inserta pero no sabem de moment on
//i suposo que l'exercici tampoc
						LogB.Information("going to insert person Unknown");
						Person pUnknown = new Person (Catalog.GetString("Unknown"), "M", DateTime.Now,
								Constants.RaceUndefinedID,
								Constants.CountryUndefinedID,
								"", "", //future1: rfid
								Constants.ServerUndefinedID, true); //dbconOpened
						unknownPersonID = pUnknown.UniqueID;

						//el crea pero no queda guardat a la bd
					}
					p.UniqueID = unknownPersonID;
					p.Name = Catalog.GetString("Unknown");
				}

				if(! Util.IsNumber(session.Name, false))
					continue;

				//at the beginning exercise was not written on the filename, because force sensor started without exercises on sql
				//"person name_2017-11-11_19-35-55.csv"
				//if cannot found exercise, assign to Unknown
				int exerciseID = -1;
				string exerciseName = fslt.Exercise;
				if(fslt.Exercise != "")
					exerciseID = ExistsAndGetUniqueID(true, Constants.ForceSensorExerciseTable, fslt.Exercise);

				if(fslt.Exercise == "" || exerciseID == -1)
				{
					if(unknownExerciseID == -1)
					{
						ForceSensorExercise fse = new ForceSensorExercise (-1, Catalog.GetString("Unknown"), 0, "", 0, "", false, false, false);
						//note we are on 1_68 so we need this import method
						//unknownExerciseID = SqliteForceSensorExercise.InsertAtDB_1_68(true, fse);
						unknownExerciseID = SqliteForceSensorExercise.Insert(true, fse);
					}

					exerciseID = unknownExerciseID;
					exerciseName = Catalog.GetString("Unknown");

					//put the old path on comment
					fslt.Comment = file.Name;
				}

				if(fslt.Exercise != "")
				{
					ForceSensorExercise fse = new ForceSensorExercise (-1, fslt.Exercise, 0, "", 0, "", false, false, false);
					//note we are on 1_68 so we need this import method
					//unknownExerciseID = SqliteForceSensorExercise.InsertAtDB_1_68(true, fse);
					unknownExerciseID = SqliteForceSensorExercise.Insert(true, fse);
				}

				//laterality (in English)
				string lat = fslt.Laterality;
				if(lat == Catalog.GetString(Constants.ForceSensorLateralityRight))
					lat = Constants.ForceSensorLateralityRight;
				else if(lat == Catalog.GetString(Constants.ForceSensorLateralityLeft))
					lat = Constants.ForceSensorLateralityLeft;
				else
					lat = Constants.ForceSensorLateralityBoth;

				string parsedDate = UtilDate.ToFile(DateTime.MinValue);
				LogB.Information("KKKKKK " + file.Name);
				Match match = Regex.Match(file.Name, @"(\d+-\d+-\d+_\d+-\d+-\d+)");
				if(match.Groups.Count == 2)
					parsedDate = match.Value;

				//filename will be this
				string myFilename = p.UniqueID + "_" + p.Name + "_" + parsedDate + ".csv";
				//try to rename the file
				try{
					//File.Move(file.FullName, Util.GetForceSensorSessionDir(Convert.ToInt32(session.Name)) + Path.DirectorySeparatorChar + myFilename);
					//file.MoveTo(myFilename);
					LogB.Information("copy from file.FullName: " + file.FullName);
				        LogB.Information("copy to: " + file.FullName.Replace(file.Name, myFilename));
					File.Move(file.FullName, file.FullName.Replace(file.Name, myFilename));
				} catch {
					//if cannot, then use old filename
					//myFilename = file.FullName;
					LogB.Information("catched at move, using the old filename: " + file.Name);
					myFilename = file.Name;
				}

				LogB.Information("going to insert forceSensor");
				ForceSensor forceSensor = new ForceSensor(-1, p.UniqueID, Convert.ToInt32(session.Name), exerciseID,
						ForceSensor.CaptureOptions.NORMAL,
						ForceSensor.AngleUndefined, lat,
						myFilename,
						Util.MakeURLrelative(Util.GetForceSensorSessionDir(Convert.ToInt32(session.Name))),
						parsedDate, fslt.Comment, "", exerciseName);
				forceSensor.InsertSQL(true);
			}
		}

		LogB.PrintAllThreads = false; //TODO: remove this
	}
}

class SqliteForceSensorExercise : Sqlite
{
	private static string table = Constants.ForceSensorExerciseTable;

	public SqliteForceSensorExercise() {
	}

	~SqliteForceSensorExercise() {}

	/*
	 * create and initialize tables
	 */

	protected internal static void createTable()
	{
		dbcmd.CommandText =
			"CREATE TABLE " + table + " ( " +
			"uniqueID INTEGER PRIMARY KEY, " +
			"name TEXT, " +
			"percentBodyWeight INT NOT NULL, " +
			"resistance TEXT, " + 				//unused
			"angleDefault INT, " +
			"description TEXT, " +
			"tareBeforeCapture INT, " +
			"forceResultant INT NOT NULL, " +
			"elastic INT NOT NULL)";
		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();
	}

	//undefined defaultAngle will be 1000
	//note execution can have a different angle than the default angle
	public static int Insert (bool dbconOpened, ForceSensorExercise ex)
	{
		if(! dbconOpened)
			Sqlite.Open();

		dbcmd.CommandText = "INSERT INTO " + table +
				" (uniqueID, name, percentBodyWeight, resistance, angleDefault, " +
				" description, tareBeforeCapture, forceResultant, elastic)" +
				" VALUES (" + ex.ToSQLInsertString() + ")";
		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		string myString = @"select last_insert_rowid()";
		dbcmd.CommandText = myString;
		int myLast = Convert.ToInt32(dbcmd.ExecuteScalar()); // Need to type-cast since `ExecuteScalar` returns an object.

		if(! dbconOpened)
			Sqlite.Close();

		return myLast;
	}

	/*
	 * is there any need of this?
	 *
	public static int InsertAtDB_1_68 (bool dbconOpened, ForceSensorExercise ex)
	{
		if(! dbconOpened)
			Sqlite.Open();

		dbcmd.CommandText = "INSERT INTO " + table +
				" (uniqueID, name, percentBodyWeight, resistance, angleDefault, " +
				" description, tareBeforeCapture)" +
				" VALUES (" + ex.ToSQLInsertString_DB_1_68() + ")";
		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		string myString = @"select last_insert_rowid()";
		dbcmd.CommandText = myString;
		int myLast = Convert.ToInt32(dbcmd.ExecuteScalar()); // Need to type-cast since `ExecuteScalar` returns an object.

		if(! dbconOpened)
			Sqlite.Close();

		return myLast;
	}
	*/

	public static void Update (bool dbconOpened, ForceSensorExercise ex)
	{
		if(! dbconOpened)
			Sqlite.Open();

		dbcmd.CommandText = "UPDATE " + table + " SET " +
			" name = \"" + ex.Name +
			"\", percentBodyWeight = " + ex.PercentBodyWeight +
			", resistance = \"" + ex.Resistance + 					//unused
			"\", angleDefault = " + ex.AngleDefault +
			", description = \"" + ex.Description +
			"\", tareBeforeCapture = " + Util.BoolToInt(ex.TareBeforeCapture).ToString() +
			", forceResultant = " + Util.BoolToInt(ex.ForceResultant).ToString() +
			", elastic = " + Util.BoolToInt(ex.Elastic).ToString() +
			" WHERE uniqueID = " + ex.UniqueID;

		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		if(! dbconOpened)
			Sqlite.Close();
	}

	public static void Delete (bool dbconOpened, int uniqueID)
	{
		openIfNeeded(dbconOpened);

		dbcmd.CommandText = "DELETE FROM " + table + " WHERE uniqueID = " + uniqueID;

		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		closeIfNeeded(dbconOpened);
	}


	public static ArrayList Select (bool dbconOpened, int uniqueID, bool onlyNames)
	{
		if(! dbconOpened)
			Sqlite.Open();

		string uniqueIDStr = "";
		if(uniqueID != -1)
			uniqueIDStr = " WHERE " + table + ".uniqueID = " + uniqueID;

		if(onlyNames)
			dbcmd.CommandText = "SELECT name FROM " + table + uniqueIDStr;
		else
			dbcmd.CommandText = "SELECT * FROM " + table + uniqueIDStr;

		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		SqliteDataReader reader;
		reader = dbcmd.ExecuteReader();

		ArrayList array = new ArrayList(1);
		ForceSensorExercise ex = new ForceSensorExercise();

		if(onlyNames) {
			while(reader.Read()) {
				ex = new ForceSensorExercise (reader[0].ToString());
				array.Add(ex);
			}
		} else {
			while(reader.Read()) {
				ex = new ForceSensorExercise (
						Convert.ToInt32(reader[0].ToString()),	//uniqueID
						reader[1].ToString(),			//name
						Convert.ToInt32(reader[2].ToString()),	//percentBodyWeight
						reader[3].ToString(),			//resistance (unused)
						Convert.ToInt32(reader[4].ToString()), 	//angleDefault
						reader[5].ToString(),			//description
						Util.IntToBool(Convert.ToInt32(reader[6].ToString())),	//tareBeforeCapture
						Util.IntToBool(Convert.ToInt32(reader[7].ToString())),	//forceResultant
						Util.IntToBool(Convert.ToInt32(reader[8].ToString()))	//elastic
						);
				array.Add(ex);
			}
		}

		reader.Close();
		if(! dbconOpened)
			Sqlite.Close();

		return array;
	}

	//database is opened
	protected internal static void import_partially_from_1_73_to_1_74_unify_resistance_and_description()
	{
		ArrayList exercises = Select(true, -1, false);
		foreach (ForceSensorExercise ex in exercises)
		{
			LogB.Information(ex.ToString());
			if(ex.Resistance == "")
				continue;

			if(ex.Description == "")
				ex.Description = ex.Resistance;
			else
				ex.Description = ex.Resistance + " - " + ex.Description;

			ex.Resistance = "";

			Update(true, ex);
		}
	}
}


class SqliteForceSensorRFD : Sqlite
{
	private static string table = Constants.ForceRFDTable;

	public SqliteForceSensorRFD() {
	}

	~SqliteForceSensorRFD() {}

	/*
	 * create and initialize tables
	 */

	protected internal static void createTable()
	{
		dbcmd.CommandText = 
			"CREATE TABLE " + table + " ( " +
			"code TEXT, " + 	//RFD1...4, I (Impulse)
			"active INT, " + 	//bool
			"function TEXT, " +
			"type TEXT, " +
			"num1 INT, " +
			"num2 INT )";
		dbcmd.ExecuteNonQuery();
	}

	public static void InsertDefaultValues(bool dbconOpened)
	{
		openIfNeeded(dbconOpened);

		Insert(true, new ForceSensorRFD("RFD1", true,
					ForceSensorRFD.Functions.FITTED, ForceSensorRFD.Types.INSTANTANEOUS, 0, -1));
		Insert(true, new ForceSensorRFD("RFD2", true,
					ForceSensorRFD.Functions.RAW, ForceSensorRFD.Types.AVERAGE, 0, 100));
		Insert(true, new ForceSensorRFD("RFD3", false,
					ForceSensorRFD.Functions.FITTED, ForceSensorRFD.Types.PERCENT_F_MAX, 50, -1));
		Insert(true, new ForceSensorRFD("RFD4", false,
					ForceSensorRFD.Functions.RAW, ForceSensorRFD.Types.RFD_MAX, -1, -1));

		InsertDefaultValueImpulse(true);

		closeIfNeeded(dbconOpened);
	}

	public static void InsertDefaultValueImpulse(bool dbconOpened)
	{
		openIfNeeded(dbconOpened);

		Insert(true, new ForceSensorImpulse(true,
					ForceSensorImpulse.Functions.RAW, ForceSensorImpulse.Types.IMP_RANGE, 0, 500));

		closeIfNeeded(dbconOpened);
	}

	public static void Insert(bool dbconOpened, ForceSensorRFD rfd)
	{
		openIfNeeded(dbconOpened);

		dbcmd.CommandText = "INSERT INTO " + table +
			" (code, active, function, type, num1, num2) VALUES (" + rfd.ToSQLInsertString() + ")";

		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		closeIfNeeded(dbconOpened);
	}
	public static void InsertImpulse(bool dbconOpened, ForceSensorImpulse impulse)
	{
		openIfNeeded(dbconOpened);

		dbcmd.CommandText = "INSERT INTO " + table +
			" (code, active, function, type, num1, num2) VALUES (" + impulse.ToSQLInsertString() + ")";

		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		closeIfNeeded(dbconOpened);
	}


	public static void Update(bool dbconOpened, ForceSensorRFD rfd)
	{
		openIfNeeded(dbconOpened);

		dbcmd.CommandText = "UPDATE " + table + " SET " +
			" active = " + Util.BoolToInt(rfd.active).ToString() + "," +
			" function = \"" + rfd.function.ToString() + "\"" + "," +
			" type = \"" + rfd.type.ToString() + "\"" + "," +
			" num1 = " + rfd.num1.ToString() + "," +
			" num2 = " + rfd.num2.ToString() +
			" WHERE code = \"" + rfd.code + "\"";

		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		closeIfNeeded(dbconOpened);
	}
	public static void UpdateImpulse(bool dbconOpened, ForceSensorImpulse impulse)
	{
		openIfNeeded(dbconOpened);

		dbcmd.CommandText = "UPDATE " + table + " SET " +
			" active = " + Util.BoolToInt(impulse.active).ToString() + "," +
			" function = \"" + impulse.function.ToString() + "\"" + "," +
			" type = \"" + impulse.type.ToString() + "\"" + "," +
			" num1 = " + impulse.num1.ToString() + "," +
			" num2 = " + impulse.num2.ToString() +
			" WHERE code = \"" + impulse.code + "\"";

		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		closeIfNeeded(dbconOpened);
	}

	//used when button_force_rfd_default is clicked
	public static void DeleteAll(bool dbconOpened)
	{
		openIfNeeded(dbconOpened);

		dbcmd.CommandText = "DELETE FROM " + table;

		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		closeIfNeeded(dbconOpened);
	}

	public static List<ForceSensorRFD> SelectAll (bool dbconOpened)
	{
		openIfNeeded(dbconOpened);

		dbcmd.CommandText = "SELECT * FROM " + table + " WHERE code != \"I\"";
		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		SqliteDataReader reader = dbcmd.ExecuteReader();

		List<ForceSensorRFD> l = new List<ForceSensorRFD>();
		while(reader.Read()) {
			ForceSensorRFD rfd = new ForceSensorRFD(
					reader[0].ToString(), 				//code
					Util.IntToBool(Convert.ToInt32(reader[1])), 	//active
					(ForceSensorRFD.Functions) Enum.Parse(
						typeof(ForceSensorRFD.Functions), reader[2].ToString()), 	//function
					(ForceSensorRFD.Types) Enum.Parse(
						typeof(ForceSensorRFD.Types), reader[3].ToString()), 	//type
					Convert.ToInt32(reader[4]), 			//num1
					Convert.ToInt32(reader[5]) 			//num2
					);
			l.Add(rfd);
		}

		reader.Close();
		closeIfNeeded(dbconOpened);

		return l;
	}

	public static ForceSensorImpulse SelectImpulse (bool dbconOpened)
	{
		openIfNeeded(dbconOpened);

		dbcmd.CommandText = "SELECT * FROM " + table + " WHERE code == \"I\"";
		LogB.SQL(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		SqliteDataReader reader = dbcmd.ExecuteReader();

		ForceSensorImpulse impulse = null;
		while(reader.Read()) {
			impulse = new ForceSensorImpulse(
					Util.IntToBool(Convert.ToInt32(reader[1])), 	//active
					(ForceSensorImpulse.Functions) Enum.Parse(
						typeof(ForceSensorImpulse.Functions), reader[2].ToString()), 	//function
					(ForceSensorImpulse.Types) Enum.Parse(
						typeof(ForceSensorImpulse.Types), reader[3].ToString()), //type
					Convert.ToInt32(reader[4]), 			//num1
					Convert.ToInt32(reader[5]) 			//num2
					);
		}

		reader.Close();
		closeIfNeeded(dbconOpened);

		return impulse;
	}
}
