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
 * http://www.xdeblas.com, http://www.deporteyciencia.com (parleblas)
 */

using System;
using Gtk;
using Glade;
using Gnome;
using GLib; //for Value
using System.Text; //StringBuilder
using System.Collections; //ArrayList


//load person (jumper)
public class PersonRecuperateWindow {
	
	[Widget] Gtk.Window person_recuperate;
	
	[Widget] Gtk.CheckButton checkbutton_sorted_by_creation_date;
	bool sortByCreationDate = false;
	
	private TreeStore store;
	private string selected;
	[Widget] Gtk.TreeView treeview_person_recuperate;
	[Widget] Gtk.Button button_recuperate;
	
	static PersonRecuperateWindow PersonRecuperateWindowBox;
	Gtk.Window parent;

	private int sessionID;
	
	private Person currentPerson;
	
	PersonRecuperateWindow (Gtk.Window parent, int sessionID) {
		Glade.XML gladeXML = Glade.XML.FromAssembly ("chronojump.glade", "person_recuperate", null);

		gladeXML.Autoconnect(this);
		this.parent = parent;

		this.sessionID = sessionID;
	
		//no posible to recuperate until one person is selected
		button_recuperate.Sensitive = false;
		
		createTreeView(treeview_person_recuperate);
		store = new TreeStore( typeof (string), typeof (string), typeof (string), typeof (string), 
				typeof (string), typeof(string), typeof(string) );
		treeview_person_recuperate.Model = store;
		fillTreeView(treeview_person_recuperate,store);
	}
	
	static public PersonRecuperateWindow Show (Gtk.Window parent, int sessionID)
	{
		if (PersonRecuperateWindowBox == null) {
			PersonRecuperateWindowBox = new PersonRecuperateWindow (parent, sessionID);
		}
		PersonRecuperateWindowBox.person_recuperate.Show ();
		
		return PersonRecuperateWindowBox;
	}
	
	private void createTreeView (Gtk.TreeView tv) {
		tv.HeadersVisible=true;
		int count = 0;
		tv.AppendColumn ( Catalog.GetString("Number"), new CellRendererText(), "text", count++);
		tv.AppendColumn ( Catalog.GetString("Name"), new CellRendererText(), "text", count++);
		tv.AppendColumn ( Catalog.GetString("Sex"), new CellRendererText(), "text", count++);
		tv.AppendColumn ( Catalog.GetString("Height"), new CellRendererText(), "text", count++);
		tv.AppendColumn ( Catalog.GetString("Weight"), new CellRendererText(), "text", count++);
		tv.AppendColumn ( Catalog.GetString("Date born"), new CellRendererText(), "text", count++);
		tv.AppendColumn ( Catalog.GetString("Description"), new CellRendererText(), "text", count++);
	}
	
	private void fillTreeView (Gtk.TreeView tv, TreeStore store) {
		TreeIter iter = new TreeIter();

		string [] mySessions;
		
		if(sortByCreationDate) {
			mySessions = SqlitePerson.SelectAllPersonsRecuperable("uniqueID", sessionID); //returns a string of values separated by ':'
		} else {
			mySessions = SqlitePerson.SelectAllPersonsRecuperable("name", sessionID); //returns a string of values separated by ':'
		}

		
		
		foreach (string session in mySessions) {
			string [] myStringFull = session.Split(new char[] {':'});

			iter = store.AppendValues (myStringFull[0], myStringFull[1], 
					getCorrectSex(myStringFull[2]), myStringFull[4], myStringFull[5],
					myStringFull[3], myStringFull[6]
					);
		}	

	}

	private string getCorrectSex (string sex) 
	{
		if (sex == "M") return  Catalog.GetString("Man");
		else return  Catalog.GetString ("Woman");
	}
	
	private void on_checkbutton_sort_by_creation_date_clicked(object o, EventArgs args) {
		if (sortByCreationDate) { sortByCreationDate = false; }
		else { sortByCreationDate = true; }
		
		store = new TreeStore( typeof (string), typeof (string), typeof (string), typeof (string), 
				typeof (string), typeof(string), typeof(string) );
		treeview_person_recuperate.Model = store;
		
		fillTreeView(treeview_person_recuperate,store);
	}
	
	//puts a value in private member selected
	private void on_treeview_person_recuperate_cursor_changed (object o, EventArgs args)
	{
		TreeView tv = (TreeView) o;
		TreeModel model;
		TreeIter iter;
		selected = "-1";

		// you get the iter and the model if something is selected
		if (tv.Selection.GetSelected (out model, out iter)) {
			selected = (string) model.GetValue (iter, 0);

			//allow clicking button_recuperate
			button_recuperate.Sensitive = true;
		}
	}
	
	void on_button_close_clicked (object o, EventArgs args)
	{
		PersonRecuperateWindowBox.person_recuperate.Hide();
		PersonRecuperateWindowBox = null;
	}
	
	void on_person_recuperate_delete_event (object o, EventArgs args)
	{
		PersonRecuperateWindowBox.person_recuperate.Hide();
		PersonRecuperateWindowBox = null;
	}
	
	void on_row_double_clicked (object o, EventArgs args)
	{
		TreeView tv = (TreeView) o;
		TreeModel model;
		TreeIter iter;

		if (tv.Selection.GetSelected (out model, out iter)) {
			selected = (string) model.GetValue (iter, 0);
			
			//activate on_button_recuperate_clicked()
			button_recuperate.Activate();
		}
	}
	
	void on_button_recuperate_clicked (object o, EventArgs args)
	{
		if(selected != "-1")
		{
			int myInt = SqlitePersonSession.Insert(Convert.ToInt32(selected), sessionID);
			currentPerson = SqlitePersonSession.PersonSelect(selected);

			store = new TreeStore( typeof (string), typeof (string), typeof (string), typeof (string), 
					typeof (string), typeof(string), typeof(string) );
			treeview_person_recuperate.Model = store;
		
			fillTreeView(treeview_person_recuperate,store);
		}
	}
	
	public Button Button_recuperate 
	{
		set {
			button_recuperate = value;	
		}
		get {
			return button_recuperate;
		}
	}
	
	public Person CurrentPerson 
	{
		get {
			return currentPerson;
		}
	}

}

//new person (jumper)
public class PersonAddWindow {
	
	[Widget] Gtk.Window person_win;
	[Widget] Gtk.Entry entry1;
	[Widget] Gtk.RadioButton radiobutton_man;
	[Widget] Gtk.RadioButton radiobutton_woman;
	[Widget] Gtk.SpinButton spinbutton_day;
	[Widget] Gtk.SpinButton spinbutton_month;
	[Widget] Gtk.SpinButton spinbutton_year;
	[Widget] Gtk.TextView textview2;
	[Widget] Gtk.SpinButton spinbutton_height;
	[Widget] Gtk.SpinButton spinbutton_weight;
	
	[Widget] Gtk.Button button_accept;
	
	static PersonAddWindow PersonAddWindowBox;
	Gtk.Window parent;
	ErrorWindow errorWin;

	private Person currentPerson;
	private int sessionID;
	private string sex = "M";
	
	PersonAddWindow (Gtk.Window parent, int sessionID) {
		Glade.XML gladeXML = Glade.XML.FromAssembly ("chronojump.glade", "person_win", null);

		gladeXML.Autoconnect(this);
		this.parent = parent;
		this.sessionID = sessionID;
		button_accept.Sensitive = false; //only make sensitive when required values are inserted

		person_win.Title =  Catalog.GetString ("New jumper");
	}
	
	void on_entries_required_changed (object o, EventArgs args)
	{
		if(entry1.Text.ToString().Length > 0 && (int) spinbutton_weight.Value > 0) {
			button_accept.Sensitive = true;
		}
		else {
			button_accept.Sensitive = false;
		}
	}
		
	void on_radiobutton_man_toggled (object o, EventArgs args)
	{
		sex = "M";
	}
	
	void on_radiobutton_woman_toggled (object o, EventArgs args)
	{
		sex = "F";
	}
	
	static public PersonAddWindow Show (Gtk.Window parent, int sessionID)
	{
		if (PersonAddWindowBox == null) {
			PersonAddWindowBox = new PersonAddWindow (parent, sessionID);
		}
		PersonAddWindowBox.person_win.Show ();
		
		return PersonAddWindowBox;
	}
	
	void on_button_cancel_clicked (object o, EventArgs args)
	{
		PersonAddWindowBox.person_win.Hide();
		PersonAddWindowBox = null;
	}
	
	void on_person_win_delete_event (object o, EventArgs args)
	{
		PersonAddWindowBox.person_win.Hide();
		PersonAddWindowBox = null;
	}
	
	void on_button_accept_clicked (object o, EventArgs args)
	{
		//separate by '/' for not confusing with the ':' separation between the other values
		string dateFull = spinbutton_day.Value.ToString() + "/" + 
			spinbutton_month.Value.ToString() + "/" + spinbutton_year.Value.ToString(); 
		
		bool personExists = SqlitePersonSession.PersonExists (Util.RemoveTilde(entry1.Text));
		if(personExists) {
			//string myString =  Catalog.GetString ("Jumper: '") + Util.RemoveTilde(entry1.Text) +  Catalog.GetString ("' exists. Please, use another name");
			string myString = string.Format(Catalog.GetString("Person: '{0}' exists. Please, use another name"), Util.RemoveTildeAndColonAndDot(entry1.Text) );
			errorWin = ErrorWindow.Show(person_win, myString);
		} else {
			currentPerson = new Person (entry1.Text, sex, dateFull, (int) spinbutton_height.Value,
						(int) spinbutton_weight.Value, textview2.Buffer.Text, sessionID);
		
			PersonAddWindowBox.person_win.Hide();
			PersonAddWindowBox = null;
		}
	}
	
	public Button Button_accept 
	{
		set {
			button_accept = value;	
		}
		get {
			return button_accept;
		}
	}
	
	public Person CurrentPerson 
	{
		get {
			return currentPerson;
		}
	}

}

public class PersonModifyWindow
{
	
	[Widget] Gtk.Window person_win;
	[Widget] Gtk.Entry entry1;
	[Widget] Gtk.RadioButton radiobutton_man;
	[Widget] Gtk.RadioButton radiobutton_woman;
	[Widget] Gtk.TextView textview2;
	[Widget] Gtk.SpinButton spinbutton_day;
	[Widget] Gtk.SpinButton spinbutton_month;
	[Widget] Gtk.SpinButton spinbutton_year;
	[Widget] Gtk.SpinButton spinbutton_height;
	[Widget] Gtk.SpinButton spinbutton_weight;
	
	[Widget] Gtk.Button button_accept;
	
	static PersonModifyWindow PersonModifyWindowBox;
	Gtk.Window parent;
	ErrorWindow errorWin;

	private Person currentPerson;
	private int sessionID;
	private int uniqueID;
	private string sex = "M";
	
	
	PersonModifyWindow (Gtk.Window parent, int sessionID) {
		Glade.XML gladeXML = Glade.XML.FromAssembly ("chronojump.glade", "person_win", null);
		
		gladeXML.Autoconnect(this);
		this.parent = parent;
		this.sessionID = sessionID;

		person_win.Title =  Catalog.GetString ("Edit jumper");
	}
	
	void on_entries_required_changed (object o, EventArgs args)
	{
		if(entry1.Text.ToString().Length > 0 && (int) spinbutton_weight.Value > 0) {
			button_accept.Sensitive = true;
		}
		else {
			button_accept.Sensitive = false;
		}
	}
		
	void on_radiobutton_man_toggled (object o, EventArgs args)
	{
		sex = "M";
	}
	
	void on_radiobutton_woman_toggled (object o, EventArgs args)
	{
		sex = "F";
	}
	
	//static public PersonModifyWindow Show (Gtk.Window parent, int sessionID)
	static public PersonModifyWindow Show (Gtk.Window parent, int sessionID, int personID)
	{
		if (PersonModifyWindowBox == null) {
			PersonModifyWindowBox = new PersonModifyWindow (parent, sessionID);
		}
		PersonModifyWindowBox.person_win.Show ();
		
		PersonModifyWindowBox.fillDialog (personID);
		
		return PersonModifyWindowBox;
	}

	private void fillDialog (int personID)
	{
		Person myPerson = SqlitePersonSession.PersonSelect(personID.ToString()); 
		
		entry1.Text = myPerson.Name;
		if (myPerson.Sex == "M") {
			radiobutton_man.Active = true;
		} else {
			radiobutton_woman.Active = true;
		}

		string [] dateFull = myPerson.DateBorn.Split(new char[] {'/'});
		spinbutton_day.Value = Convert.ToDouble ( dateFull[0] );	
		spinbutton_month.Value = Convert.ToDouble ( dateFull[1] );	
		spinbutton_year.Value = Convert.ToDouble ( dateFull[2] );	
		
		spinbutton_height.Value = myPerson.Height;
		spinbutton_weight.Value = myPerson.Weight;

		TextBuffer tb = new TextBuffer (new TextTagTable());
		tb.SetText(myPerson.Description);
		textview2.Buffer = tb;
			
		uniqueID = personID;
	}
		
	
	void on_button_cancel_clicked (object o, EventArgs args)
	{
		PersonModifyWindowBox.person_win.Hide();
		PersonModifyWindowBox = null;
	}
	
	//void on_person_modify_delete_event (object o, EventArgs args)
	void on_person_win_delete_event (object o, EventArgs args)
	{
		PersonModifyWindowBox.person_win.Hide();
		PersonModifyWindowBox = null;
	}
	
	void on_button_accept_clicked (object o, EventArgs args)
	{
		bool personExists = SqlitePersonSession.PersonExistsAndItsNotMe (uniqueID, Util.RemoveTilde(entry1.Text));
		if(personExists) {
			//string myString =  Catalog.GetString ("Jumper: '") + Util.RemoveTilde(entry1.Text) +  Catalog.GetString ("' exists. Please, use another name");
			string myString = string.Format(Catalog.GetString("Person: '{0}' exists. Please, use another name"), Util.RemoveTildeAndColonAndDot(entry1.Text) );
			errorWin = ErrorWindow.Show(person_win, myString);
		} else {
			//separate by '/' for not confusing with the ':' separation between the other values
			string dateFull = spinbutton_day.Value.ToString() + "/" + 
				spinbutton_month.Value.ToString() + "/" + spinbutton_year.Value.ToString(); 
			
			currentPerson = new Person (uniqueID, entry1.Text, sex, dateFull, (int) spinbutton_height.Value,
						(int) spinbutton_weight.Value, textview2.Buffer.Text);

			SqlitePerson.Update (currentPerson); 
		
			PersonModifyWindowBox.person_win.Hide();
			PersonModifyWindowBox = null;
		}
	}
	
	public Button Button_accept 
	{
		set {
			button_accept = value;	
		}
		get {
			return button_accept;
		}
	}
	
	public Person CurrentPerson 
	{
		get {
			return currentPerson;
		}
	}
	
}

//new persons multiple (10)
public class PersonAddMultipleWindow {
	
	[Widget] Gtk.Window person_add_multiple;
	
	[Widget] Gtk.Entry entry1;
	[Widget] Gtk.Entry entry2;
	[Widget] Gtk.Entry entry3;
	[Widget] Gtk.Entry entry4;
	[Widget] Gtk.Entry entry5;
	[Widget] Gtk.Entry entry6;
	[Widget] Gtk.Entry entry7;
	[Widget] Gtk.Entry entry8;
	[Widget] Gtk.Entry entry9;
	[Widget] Gtk.Entry entry10;
	
	[Widget] Gtk.RadioButton r_1_m;
	[Widget] Gtk.RadioButton r_1_f;
	[Widget] Gtk.RadioButton r_2_m;
	[Widget] Gtk.RadioButton r_2_f;
	[Widget] Gtk.RadioButton r_3_m;
	[Widget] Gtk.RadioButton r_3_f;
	[Widget] Gtk.RadioButton r_4_m;
	[Widget] Gtk.RadioButton r_4_f;
	[Widget] Gtk.RadioButton r_5_m;
	[Widget] Gtk.RadioButton r_5_f;
	[Widget] Gtk.RadioButton r_6_m;
	[Widget] Gtk.RadioButton r_6_f;
	[Widget] Gtk.RadioButton r_7_m;
	[Widget] Gtk.RadioButton r_7_f;
	[Widget] Gtk.RadioButton r_8_m;
	[Widget] Gtk.RadioButton r_8_f;
	[Widget] Gtk.RadioButton r_9_m;
	[Widget] Gtk.RadioButton r_9_f;
	[Widget] Gtk.RadioButton r_10_m;
	[Widget] Gtk.RadioButton r_10_f;

	[Widget] Gtk.SpinButton spinbutton1;
	[Widget] Gtk.SpinButton spinbutton2;
	[Widget] Gtk.SpinButton spinbutton3;
	[Widget] Gtk.SpinButton spinbutton4;
	[Widget] Gtk.SpinButton spinbutton5;
	[Widget] Gtk.SpinButton spinbutton6;
	[Widget] Gtk.SpinButton spinbutton7;
	[Widget] Gtk.SpinButton spinbutton8;
	[Widget] Gtk.SpinButton spinbutton9;
	[Widget] Gtk.SpinButton spinbutton10;
	
	[Widget] Gtk.Button button_accept;
	
	static PersonAddMultipleWindow PersonAddMultipleWindowBox;
	Gtk.Window parent;
	ErrorWindow errorWin;

	private Person currentPerson;
	int sessionID;
	int personsCreatedCount;
	string errorExistsString;
	string errorWeightString;
	
	PersonAddMultipleWindow (Gtk.Window parent, int sessionID) {
		Glade.XML gladeXML = Glade.XML.FromAssembly ("chronojump.glade", "person_add_multiple", null);

		gladeXML.Autoconnect(this);
		this.parent = parent;
		this.sessionID = sessionID;
	}
	
	static public PersonAddMultipleWindow Show (Gtk.Window parent, int sessionID)
	{
		if (PersonAddMultipleWindowBox == null) {
			PersonAddMultipleWindowBox = new PersonAddMultipleWindow (parent, sessionID);
		}
		PersonAddMultipleWindowBox.person_add_multiple.Show ();
		
		return PersonAddMultipleWindowBox;
	}
	
	void on_button_cancel_clicked (object o, EventArgs args)
	{
		PersonAddMultipleWindowBox.person_add_multiple.Hide();
		PersonAddMultipleWindowBox = null;
	}
	
	void on_delete_event (object o, EventArgs args)
	{
		PersonAddMultipleWindowBox.person_add_multiple.Hide();
		PersonAddMultipleWindowBox = null;
	}
	
	void on_button_accept_clicked (object o, EventArgs args)
	{
		errorExistsString = "";
		errorWeightString = "";
		personsCreatedCount = 0;
		
		int count = 1;
		checkEntries(count++, entry1.Text.ToString(), (int) spinbutton1.Value);
		checkEntries(count++, entry2.Text.ToString(), (int) spinbutton2.Value);
		checkEntries(count++, entry3.Text.ToString(), (int) spinbutton3.Value);
		checkEntries(count++, entry4.Text.ToString(), (int) spinbutton4.Value);
		checkEntries(count++, entry5.Text.ToString(), (int) spinbutton5.Value);
		checkEntries(count++, entry6.Text.ToString(), (int) spinbutton6.Value);
		checkEntries(count++, entry7.Text.ToString(), (int) spinbutton7.Value);
		checkEntries(count++, entry8.Text.ToString(), (int) spinbutton8.Value);
		checkEntries(count++, entry9.Text.ToString(), (int) spinbutton9.Value);
		checkEntries(count++, entry10.Text.ToString(), (int) spinbutton10.Value);

		string combinedErrorString = "";
		combinedErrorString = readErrorStrings();
		
		if (combinedErrorString.Length > 0) {
			errorWin = ErrorWindow.Show(person_add_multiple, combinedErrorString);
		} else {
			prepareAllNonBlankRows();
		
			PersonAddMultipleWindowBox.person_add_multiple.Hide();
			PersonAddMultipleWindowBox = null;
		}
	}
		
	void checkEntries(int count, string name, int weight) {
		if(name.Length > 0) {
			bool personExists = SqlitePersonSession.PersonExists (Util.RemoveTilde(name));
			if(personExists) {
				errorExistsString += "[" + count + "] " + name + "\n";
			}
			if(weight == 0) {
				errorWeightString += "[" + count + "] " + name + "\n";
			}
		}
	}
	
	string readErrorStrings() {
		if (errorExistsString.Length > 0) {
			errorExistsString = "ERROR This person(s) exists in the database:\n" + errorExistsString;
		}
		if (errorWeightString.Length > 0) {
			errorWeightString = "\nERROR weight of this person(s) cannot be 0:\n" + errorWeightString;
		}
		
		return errorExistsString + errorWeightString;
	}

	//inserts all the rows where name is not blank
	//all this names doesn't match with other in the database, and the weights are > 0 ( checked in checkEntries() )
	void prepareAllNonBlankRows() 
	{
		//the last is the first for having the first value inserted as currentPerson
		
		if( entry10.Text.ToString().Length > 0 ) { 
			insertPerson (entry10.Text.ToString(), r_10_m.Active, (int) spinbutton10.Value);
		}
		if( entry9.Text.ToString().Length > 0 ) { 
			insertPerson (entry9.Text.ToString(), r_9_m.Active, (int) spinbutton9.Value);
		}
		if( entry8.Text.ToString().Length > 0 ) { 
			insertPerson (entry8.Text.ToString(), r_8_m.Active, (int) spinbutton8.Value);
		}
		if( entry7.Text.ToString().Length > 0 ) { 
			insertPerson (entry7.Text.ToString(), r_7_m.Active, (int) spinbutton7.Value);
		}
		if( entry6.Text.ToString().Length > 0 ) { 
			insertPerson (entry6.Text.ToString(), r_6_m.Active, (int) spinbutton6.Value);
		}
		if( entry5.Text.ToString().Length > 0 ) { 
			insertPerson (entry5.Text.ToString(), r_5_m.Active, (int) spinbutton5.Value);
		}
		if( entry4.Text.ToString().Length > 0 ) { 
			insertPerson (entry4.Text.ToString(), r_4_m.Active, (int) spinbutton4.Value);
		}
		if( entry3.Text.ToString().Length > 0 ) { 
			insertPerson (entry3.Text.ToString(), r_3_m.Active, (int) spinbutton3.Value);
		}
		if( entry2.Text.ToString().Length > 0 ) { 
			insertPerson (entry2.Text.ToString(), r_2_m.Active, (int) spinbutton2.Value);
		}
		if( entry1.Text.ToString().Length > 0 ) { 
			insertPerson (entry1.Text.ToString(), r_1_m.Active, (int) spinbutton1.Value);
		}
	}

	void insertPerson (string name, bool male, int weight) 
	{
		string sex = "F";
		if(male) { sex = "M"; }
		
		currentPerson = new Person ( name, sex, "0/0/1900", 
				0, weight, 		//height, weight	
				"", sessionID		//description, sessionID
				);

		personsCreatedCount ++;
	}
	
	
	public Button Button_accept 
	{
		set {
			button_accept = value;	
		}
		get {
			return button_accept;
		}
	}

	public int PersonsCreatedCount 
	{
		get { return personsCreatedCount; }
	}
	
	public Person CurrentPerson 
	{
		get {
			return currentPerson;
		}
	}

}
