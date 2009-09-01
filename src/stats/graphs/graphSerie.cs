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
using System.Collections; //ArrayList

//using NPlot.Gtk;
//using NPlot;
using System.Drawing;
using System.Drawing.Imaging;

public class GraphSerie
{
	public string Title;
	public bool IsLeftAxis;
	//public Marker SerieMarker;
	public Color SerieColor;
	public ArrayList SerieData;
	public double Avg; //height of avg line

	public GraphSerie() {
		SerieData = new ArrayList();
		Avg = 0;
	}

	//public GraphSerie(string Title, bool IsLeftAxis, Marker SerieMarker, Color SerieColor, ArrayList SerieData) 
	public GraphSerie(string Title, bool IsLeftAxis, Color SerieColor, ArrayList SerieData) 
	{
		this.Title = 		Title;
		this.IsLeftAxis = 	IsLeftAxis;
		this.SerieColor =	SerieColor;
		//this.SerieMarker =	SerieMarker;
		this.SerieData =	SerieData;
		Avg = 0;
	}
}	
