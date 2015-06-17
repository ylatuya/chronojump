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
 * Copyright (C) 2004-2015   Xavier de Blas <xaviblas@gmail.com> 
 */

using System;
using Gtk;
using Gdk;
using Glade;
using System.Collections; //ArrayList
using System.IO; 

public class PersonSelectWindow 
{
	[Widget] Gtk.Window person_select_window;
	[Widget] Gtk.Table table1;
	
	static PersonSelectWindow PersonSelectWindowBox;
	Gtk.Window parent;
	
	private ArrayList persons;
	public Person SelectedPerson;
	public Gtk.Button FakeButtonDone;

	
	PersonSelectWindow (Gtk.Window parent) {
		Glade.XML gladeXML;
		gladeXML = Glade.XML.FromAssembly (Util.GetGladePath() + "chronojump.glade", "person_select_window", "chronojump");
		gladeXML.Autoconnect(this);
		
		//put an icon to window
		UtilGtk.IconWindow(person_select_window);
		
		FakeButtonDone = new Gtk.Button();
	}
	
	static public PersonSelectWindow Show (Gtk.Window parent, ArrayList persons)
	{
		if (PersonSelectWindowBox == null) {
			PersonSelectWindowBox = new PersonSelectWindow (parent);
		}
		
		PersonSelectWindowBox.parent = parent;
		PersonSelectWindowBox.persons = persons;
		
		PersonSelectWindowBox.createTable();
		
		PersonSelectWindowBox.person_select_window.Show ();
		
		return PersonSelectWindowBox;
	}

	private void createTable() 
	{
		uint padding = 8;	
		uint cols = 4; //each row has 4 columns
		uint rows = Convert.ToUInt32(Math.Floor(persons.Count / (1.0 * cols) ) +1);
		int count = 0;
		
		for (int row_i = 0; row_i < rows; row_i ++) {
			for (int col_i = 0; col_i < cols; col_i ++) 
			{
				if(count >= persons.Count)
					return;
				
				Person p = (Person) persons[count ++];

				PersonPhotoButton ppb = new PersonPhotoButton(p);
				Gtk.Button b = ppb.CreateButton();
				
				b.Clicked += new EventHandler(on_button_clicked);
				b.Show();
				
				table1.Attach (b, (uint) col_i, (uint) col_i +1, (uint) row_i, (uint) row_i +1, 
						Gtk.AttachOptions.Fill | Gtk.AttachOptions.Expand, 
						Gtk.AttachOptions.Fill | Gtk.AttachOptions.Expand, 
						padding, padding);
			}
		}
	}
	
	private void on_button_clicked (object o, EventArgs args)
	{
		LogB.Information("Clicked");

		//access the button
		Button b = (Button) o;
	
		int personID = PersonPhotoButton.GetPersonID(b);

		LogB.Information("UniqueID: " + personID.ToString());

		//TODO: now need to process the signal and close
		foreach(Person p in persons)
			if(p.UniqueID == personID) {
				SelectedPerson = p;
				FakeButtonDone.Click();
				close_window();
			}
	}

	private void close_window() {	
		PersonSelectWindowBox.person_select_window.Hide();
		PersonSelectWindowBox = null;
	}
	
	//ESC is enabled
	protected virtual void on_button_close_clicked (object o, EventArgs args) {
		close_window();
	}
	
	//TODO: allow to close with ESC
	private void on_delete_event (object o, DeleteEventArgs args)
	{
		PersonSelectWindowBox.person_select_window.Hide();
		PersonSelectWindowBox = null;
	}
}

//used by PersonSelectWindow
public class PersonPhotoButton
{
	private Person p;

	public PersonPhotoButton (Person p) {
		this.p = p;
	}

	public Gtk.Button CreateButton () {
		Gtk.VBox vbox = new Gtk.VBox();

		Gtk.Image image = new Gtk.Image();
		string photoFile = Util.GetPhotoFileName(true, p.UniqueID);
		if(photoFile != "" && File.Exists(photoFile)) {
			try {
				Pixbuf pixbuf = new Pixbuf (photoFile); //from a file
				image.Pixbuf = pixbuf;
				image.Visible = true;
			}
			catch {
				LogB.Warning("catched while adding photo");
			}
		}

		Gtk.Label label_id = new Gtk.Label(p.UniqueID.ToString());
		label_id.Visible = false; //hide this to the user

		Gtk.Label label_name = new Gtk.Label(p.Name);
		label_name.Visible = true;

		vbox.PackStart(image);
		vbox.PackStart(label_id);
		vbox.PackStart(label_name);

		vbox.Show();

		Button b = new Button(vbox);

		return b;
	}

	public static int GetPersonID (Gtk.Button b) 
	{
		//access the vbox
		Gtk.VBox box = (Gtk.VBox) b.Child;
		
		//access the memebers of vbox
		Array box_elements = box.Children;
		
		//access uniqueID	
		Gtk.Label l = (Gtk.Label) box_elements.GetValue(1); //the ID
		int personID = Convert.ToInt32(l.Text);

		//LogB.Information("UniqueID: " + l.Text.ToString());
		
		//access name
		/*
		l = (Gtk.Label) box_elements.GetValue(2); //the name
		LogB.Information("Name: " + l.Text.ToString());
		*/

		return personID;
	}
}

