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
 *  Copyright (C) 2019   Xavier de Blas <xaviblas@gmail.com> 
 */

using System;
using System.Collections.Generic; //List<T>

public abstract class ForceSensorDynamics
{
	//Position Speed Accel Power, boolena to know if we have to gather these lists
	public bool CalculedElasticPSAP;

	/*
	 * Used for elastic. Minimum has to be 1
	 * values to be removed at the beginning, and also at the end:
	 * 10 will be 10 at start, 10 at end.
	 */
	public int RemoveNValues = 0;

	protected List<int> time_micros_l;
	protected List<double> force_l;
	protected ForceSensor.CaptureOptions fsco;
	protected ForceSensorExercise fse;
	protected double stiffness;
	protected double totalMass;

	protected List<double> time_l;

	protected void initialize(List<double> force_l, 
			ForceSensor.CaptureOptions fsco, ForceSensorExercise fse,
			double personMass, double stiffness)
	{
		this.force_l = force_l;
		this.fsco = fsco;
		this.fse = fse;
		this.stiffness = stiffness;

		totalMass = 0;
		if(fse.PercentBodyWeight > 0 && personMass > 0)
			totalMass = fse.PercentBodyWeight * personMass / 100.0;

		CalculedElasticPSAP = false;
	}

	//first value has to be removed also on not elastic because time between first and second is so short
	//so is better to remove it
	protected virtual void removeFirstValue()
	{
		force_l.RemoveAt(0);
	}

	protected double calculeForceWithCaptureOptions(double force)
	{
		if(fsco == ForceSensor.CaptureOptions.ABS)
			return Math.Abs(force);
		if(fsco == ForceSensor.CaptureOptions.INVERTED)
			return -1 * force;

		return force;
	}
	protected List<double> calculeForceWithCaptureOptionsFullSet()
	{
		for(int i = 0 ; i < force_l.Count; i ++)
			force_l[i] = calculeForceWithCaptureOptions(force_l[i]);

		return force_l;
	}

	public virtual List<double> GetForces()
	{
		return force_l;
	}

	//this 4 are only implemented on elastic
	public virtual List<double> GetPositions()
	{
		return new List<double>();
	}

	public virtual List<double> GetSpeeds()
	{
		return new List<double>();
	}

	public virtual List<double> GetAccels()
	{
		return new List<double>();
	}

	public virtual List<double> GetPowers()
	{
		return new List<double>();
	}
}
	
public class ForceSensorDynamicsNotElastic : ForceSensorDynamics
{
	List<double> position_l;
	List<double> speed_l;
	List<double> accel_l;
	List<double> power_l;

	public ForceSensorDynamicsNotElastic (List<int> time_micros_l, List<double> force_l, 
			ForceSensor.CaptureOptions fsco, ForceSensorExercise fse,
			double personMass, double stiffness)
	{
		initialize(force_l, fsco, fse, personMass, stiffness);
		removeFirstValue();

		if(! fse.ForceResultant)
		{
			calculeForceWithCaptureOptionsFullSet();
			return;
		}

		calcule();
	}

	//forces are updated, so do not Add to the list
	private void calcule()
	{
		double accel = 0;

		for (int i = 0 ; i < force_l.Count; i ++)
		{
			double force = Math.Sqrt(
					Math.Pow(Math.Cos(fse.AngleDefault * Math.PI / 180.0) * (force_l[i] + totalMass * accel), 2) +                  //Horizontal
					Math.Pow(Math.Sin(fse.AngleDefault * Math.PI / 180.0) * (force_l[i] + totalMass * accel) + totalMass * 9.81, 2) //Vertical
					);
			force_l[i] = calculeForceWithCaptureOptions(force);
		}
	}
}

public class ForceSensorDynamicsElastic : ForceSensorDynamics
{
	List<double> position_l;
	List<double> speed_l;
	List<double> accel_l;
	List<double> power_l;

	public ForceSensorDynamicsElastic (List<int> time_micros_l, List<double> force_l, 
			ForceSensor.CaptureOptions fsco, ForceSensorExercise fse,
			double personMass, double stiffness)
	{
		if(! fse.ForceResultant)
		{
			calculeForceWithCaptureOptionsFullSet();
			return;
		}

		RemoveNValues = 10;
		initialize(force_l, fsco, fse, personMass, stiffness);
		convertTimeToSeconds(time_micros_l);
		removeFirstValue();
		calcule();
		CalculedElasticPSAP = true;
	}

	//time comes in microseconds
	private void convertTimeToSeconds(List<int> time_micros_l)
	{
		time_l = new List<double>();
		for (int i = 0 ; i < time_micros_l.Count; i ++)
		{
			time_l.Add(time_micros_l[i] / 1000000.0);
			//LogB.Information(string.Format("i: {0}, time_micros_l[i]: {1}, time_l: {2}", i, time_micros_l[i], time_l[i]));
		}
	}

	protected override void removeFirstValue()
	{
		force_l.RemoveAt(0);
		time_l.RemoveAt(0);
	}

	private void calcule()
	{
		//TODO: check minimum length of forces

		position_l = new List<double>();
		speed_l = new List<double>();
		accel_l = new List<double>();
		power_l = new List<double>();

		calculePositions();
		calculeSpeeds();
		calculeAccels();
		calculeForces();
		calculePowers();
	}

	private int smoothFactor = 5; //use odd (impar) values like 5, 7, 9
	/*
	 * A smothFactor == 5, this will use 5 values: 2 previous, current value, 2 post.
	 * the calculated average is assigned to the current
	 */
	private List<double> smoothVariable(List<double> original_l)
	{
		List<double> smoothed_l = new List<double>();

		//a smoothFactor == 5 will iterate two times here:
		for(int i = 0; i < Math.Floor(smoothFactor /2.0); i ++)
			smoothed_l.Add(0);

		for(int i = 2; i < position_l.Count -2; i ++)
			smoothed_l.Add( (original_l[i-2] + original_l[i-1] + original_l[i] + original_l[i+1] + original_l[i+2]) / 5.0 );

		for(int i = 0; i < Math.Floor(smoothFactor /2.0); i ++)
			smoothed_l.Add(0);

		return smoothed_l;
	}

		
	private void calculePositions()
	{
		for (int i = 0 ; i < force_l.Count; i ++)
			position_l.Add(force_l[i] / stiffness);

		position_l = smoothVariable(position_l);
	}

	private void calculeSpeeds()
	{
		for (int i = 0 ; i < time_l.Count; i ++)
		{
			int pre = i - 1;
			int post = i + 1;

			if(i == 0)
				pre = 0;
			else if(i == time_l.Count -1)
				post = i;

			speed_l.Add( (position_l[post] - position_l[pre]) / (time_l[post] - time_l[pre]) );
		}

		speed_l = smoothVariable(speed_l);
	}

	private void calculeAccels()
	{
		for (int i = 0 ; i < speed_l.Count; i ++)
		{
			int pre = i - 1;
			int post = i + 1;

			if(i == 0)
				pre = 0;
			else if(i == speed_l.Count -1)
				post = i;

			accel_l.Add( (speed_l[post] - speed_l[pre]) / (time_l[post] - time_l[pre]) );
			//LogB.Information(string.Format("i: {0}, accel_l[i]: {1}", i, accel_l[i]));
		}

		accel_l = smoothVariable(accel_l);
	}

	//forces are updated, so do not Add to the list
	private void calculeForces()
	{
		for (int i = 0 ; i < force_l.Count; i ++)
		{
			//LogB.Information(string.Format("i: {0}, force_l[i]: {1}, force_l.Count: {2}", i, force_l[i], force_l.Count));
			double force = Math.Sqrt(
					Math.Pow(Math.Cos(fse.AngleDefault * Math.PI / 180.0) * (force_l[i] + totalMass * accel_l[i]), 2) +                  //Horizontal
					Math.Pow(Math.Sin(fse.AngleDefault * Math.PI / 180.0) * (force_l[i] + totalMass * accel_l[i]) + totalMass * 9.81, 2) //Vertical
					);
			force_l[i] = calculeForceWithCaptureOptions(force);
			//LogB.Information(string.Format("i: {0}, force_l[i]: {1}", i, force_l[i]));
		}
	}
	
	private void calculePowers()
	{	
		for (int i = 0 ; i < force_l.Count; i ++)
		{
			power_l.Add(
					speed_l[i] * (force_l[i] + totalMass * accel_l[i]) + //Power associated to the acceleration of the mass
					speed_l[i] * (Math.Sin(fse.AngleDefault * Math.PI / 180.0) * totalMass * 9.81) //Power associated to the gravitatory field
				   );
		}
	}

	private List<double> stripStartEnd(List<double> l)
	{
		LogB.Information(string.Format("removeN: {0}, l.Count: {1}", RemoveNValues, l.Count));
		return l.GetRange(RemoveNValues -1, l.Count - 2*RemoveNValues);
	}

	public override List<double> GetForces()
	{
		return stripStartEnd(force_l);
	}

	public override List<double> GetPositions()
	{
		return stripStartEnd(position_l);
	}

	public override List<double> GetSpeeds()
	{
		return stripStartEnd(speed_l);
	}

	public override List<double> GetAccels()
	{
		return stripStartEnd(accel_l);
	}

	public override List<double> GetPowers()
	{
		return stripStartEnd(power_l);
	}
}
