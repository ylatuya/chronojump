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
 * Xavier de Blas: 
 */

using System;
using System.Data;
using System.IO;
using System.Collections; //ArrayList
using Mono.Data.Sqlite;

using Mono.Unix; //Catalog

class SqliteServer : Sqlite
{
	public SqliteServer() {
	}
	
	~SqliteServer() {}

	public void CreatePingTable()
	 {
		dbcmd.CommandText = 
			"CREATE TABLE " + Constants.ServerPingTable + " ( " +
			"uniqueID INTEGER PRIMARY KEY, " +
			"evaluatorID INT, " + //foreign key
			"cjVersion TEXT, " +
			"osVersion TEXT, " +
			"IP TEXT, " +
			"date TEXT ) ";
		dbcmd.ExecuteNonQuery();
	 }

	public void CreateEvaluatorTable()
	 {
		dbcmd.CommandText = 
			"CREATE TABLE " + Constants.ServerEvaluatorTable + " ( " +
			"uniqueID INTEGER PRIMARY KEY, " +
			"name TEXT, " +
			"email TEXT, " +
			"dateborn TEXT, " +
			"countryID INT, " + //foreign key
			"confiable INT ) "; //bool
		dbcmd.ExecuteNonQuery();
	 }

	//public static int InsertPing(ServerPing ping)
	public static int InsertPing(bool dbconOpened, int evaluatorID, string cjVersion, string osVersion, string ip, string date)
	{
		if(! dbconOpened)
			dbcon.Open();

		string uniqueID = "NULL";

		string myString = "INSERT INTO " + Constants.ServerPingTable + 
			" (uniqueID, evaluatorID, cjVersion, osVersion, IP, date) VALUES (" + 
			uniqueID + ", " + evaluatorID + ", '" + 
			cjVersion + "', '" + osVersion + "', '" +
			ip + "', '" + date + "')" ;
		
		dbcmd.CommandText = myString;
		
		Log.WriteLine(dbcmd.CommandText.ToString());
		
		dbcmd.ExecuteNonQuery();
		int myReturn = dbcon.LastInsertRowId;

		if(! dbconOpened)
			dbcon.Close();

		return myReturn;
	}

	public static int InsertEvaluator(bool dbconOpened, string name, string email, string dateBorn, int countryID, bool confiable)
	{
		if(! dbconOpened)
			dbcon.Open();

		string uniqueID = "NULL";

		string myString = "INSERT INTO " + Constants.ServerEvaluatorTable + 
			" (uniqueID, name, email, dateBorn, countryID, confiable) VALUES (" + 
			uniqueID + ", '" + name + "', '" + 
			email + "', '" + dateBorn + "', " +
			countryID + ", " + Util.BoolToInt(confiable) + ")" ;
		
		dbcmd.CommandText = myString;
		
		Log.WriteLine(dbcmd.CommandText.ToString());
		
		dbcmd.ExecuteNonQuery();
		int myReturn = dbcon.LastInsertRowId;

		if(! dbconOpened)
			dbcon.Close();

		return myReturn;
	}

	public static string [] Stats() {
		ArrayList stats = new ArrayList();
			
		dbcon.Open();

		/*
		 * is good to add the string stuff like "Pings" 
		 * because then client will show this data or not 
		 * depending if it matches what want to show.
		 * Maintain the ':' as separator
		*/
		stats.Add("Pings:" + Sqlite.Count(Constants.ServerPingTable, true).ToString());
		stats.Add("Evaluators:" + Sqlite.Count(Constants.ServerEvaluatorTable, true).ToString());
		stats.Add("Sessions:" + Sqlite.Count(Constants.SessionTable, true).ToString());
		stats.Add("Persons:" + Sqlite.Count(Constants.PersonTable, true).ToString());
		stats.Add("Jumps:" + Sqlite.Count(Constants.JumpTable, true).ToString());
		stats.Add("JumpsRj:" + Sqlite.Count(Constants.JumpRjTable, true).ToString());
		stats.Add("Runs:" + Sqlite.Count(Constants.RunTable, true).ToString());
		stats.Add("RunsInterval:" + Sqlite.Count(Constants.RunIntervalTable, true).ToString());
		stats.Add("ReactionTimes:" + Sqlite.Count(Constants.ReactionTimeTable, true).ToString());
		stats.Add("Pulses:" + Sqlite.Count(Constants.PulseTable, true).ToString());
		
		dbcon.Close();

		string [] statsString = Util.ArrayListToString(stats);
		return statsString;
	}
	
	/*
	 * this is only called on client
	 */
	public static string [] StatsMine() {
		ArrayList stats = new ArrayList();
			
		dbcon.Open();

		/*
		 * is good to add the string stuff like "Pings" 
		 * because then client will show this data or not 
		 * depending if it matches what want to show.
		 * Maintain the ':' as separator
		*/
		stats.Add("Sessions:" + Sqlite.CountCondition(Constants.SessionTable, true, "serverUniqueID", ">", "0").ToString());
		stats.Add("Persons:" + Sqlite.CountCondition(Constants.PersonTable, true, "serverUniqueID", ">", "0").ToString());
		stats.Add("Jumps:" + Sqlite.CountCondition(Constants.JumpTable, true, "simulated", ">", "0").ToString());
		stats.Add("JumpsRj:" + Sqlite.CountCondition(Constants.JumpRjTable, true, "simulated", ">", "0").ToString());
		stats.Add("Runs:" + Sqlite.CountCondition(Constants.RunTable, true, "simulated", ">", "0").ToString());
		stats.Add("RunsInterval:" + Sqlite.CountCondition(Constants.RunIntervalTable, true, "simulated", ">", "0").ToString());
		stats.Add("ReactionTimes:" + Sqlite.CountCondition(Constants.ReactionTimeTable, true, "simulated", ">", "0").ToString());
		stats.Add("Pulses:" + Sqlite.CountCondition(Constants.PulseTable, true, "simulated", ">", "0").ToString());
		
		dbcon.Close();

		string [] statsString = Util.ArrayListToString(stats);
		return statsString;
	}
	

}
