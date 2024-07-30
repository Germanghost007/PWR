using Godot;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using System;
using System.Linq;

public partial class Reactor : VBoxContainer
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//Initialize values
		steamGens = CreateSteamGens(); //Must be before ReactorMatrix
		rcps = CreateRCPs(); //Must be before ReactorMatrix
		reactorMatrix = CreateReactorSections(8, 1);

		idlePower = 1 / numberOfReactorSections;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		CalcWaterflows();
		updatePower();
	}

	//"Constants", don't change after initialization
	double[,] BaseTransferMatrix = GetBaseTransferMatrix();
	double[,] BaseReflectionMatrix = GetBaseTransferMatrix();
	double ratedReactorPower = 3200;
	double heatingMultiplier = 0.7548875;
	double idlePower = 0;
	int numberOfReactorSections = 0; //Incremented when creating ReactorSections
	object[,] reactorMatrix;
	SteamGen[] steamGens;
	RCP[] rcps;


	public void updatePower() {

		for(int i = 0; i < reactorMatrix.GetLength(0); i++) {
			for(int j = 0; j < reactorMatrix.GetLength(1); j++) {
				if(reactorMatrix[i,j] is ReactorSection reactorSection) {
				
					reactorSection.power = CalcPower(reactorSection);
					
				}
			}
		}
	}

	public double CalcFactors(ReactorSection reactorSection) {
		double RodPos = reactorSection.rods.Average();
		return 2 * RodPos / 100;
	}

	public double CalcPower(ReactorSection reactorSection) {
		double power = (idlePower + reactorSection.prevPower) * CalcFactors(reactorSection);
		
		return power;
	}

	public double CalcOutletTemp(ReactorSection reactorSection) {
		double heatingPower = reactorSection.power * heatingMultiplier;
		double tempRaise = ( (heatingPower*1000) / (reactorSection.waterflow/1000) ) / 4200;
		return reactorSection.sendingSteamGen.primaryouttemp + tempRaise;
	}

	public void CalcWaterflows() {
		for(int i = 0; i < reactorMatrix.GetLength(0); i++) {
			for(int j = 0; j < reactorMatrix.GetLength(1); j++) {
				if(reactorMatrix[i,j] is ReactorSection reactorSection) {
				
					if(i < ((reactorMatrix.GetLength(0) - 1) / 2 )) {
						reactorSection.waterflow = reactorSection.rcps[0].waterflow + reactorSection.rcps[1].waterflow;
					} else {
						reactorSection.waterflow = reactorSection.rcps[2].waterflow + reactorSection.rcps[3].waterflow;
					}
				}
			}
		}
	}

	public double[,] RandomizeMatrix() {
		
		double[,] array = BaseTransferMatrix;
		
		double stddev = array.Cast<double>().Sum();

		for (int i = 0; i <  array.GetLength(0); i++)
		{
			for (int j = 0; j <  array.GetLength(1); j++)
			{
				array[i,j] = MathNet.Numerics.Distributions.Normal.Sample(array[i, j], stddev);
				if(array[i,j] < array[i,j] - 4*stddev) array[i,j] = array[i,j] - 4*stddev;
				else if(array[i,j] > array[i,j] + 4*stddev) array[i,j] = array[i,j] + 4*stddev;
			}
		}

		return array;
	}

	public static double[,] GetBaseTransferMatrix() {
		double[,] array = new double[3, 3];

		
		int rows = array.GetLength(0);
		int cols = array.GetLength(1);

		for (int i = 0; i < rows; i++)
		{
			for (int j = 0; j < cols; j++)
			{
				double temp = Math.Sqrt(Math.Pow((i - 2), 2) + Math.Pow((j - 2), 2));
				temp += 1;
				temp = 1 / Math.Pow(temp, 2);
				array[i, j] = temp;
			}
		}

		return array;
	}

	public object[,] CreateReactorSections(int diameter, int neutronTransferDistance) {
			object[,] reactorSections = new object[diameter + 2*neutronTransferDistance, diameter + 2*neutronTransferDistance];
			double center = (reactorSections.GetLength(0) - 1) / 2;

			for (int i = 0; i < reactorSections.GetLength(0); i++) {
				for (int j = 0; j < reactorSections.GetLength(1); j++) {
					
					double distance = Math.Sqrt((i - center) + (j - center));
					if(distance <= diameter / 2) {
						
						SteamGen receivingGen;
						SteamGen sendingGen;
						RCP[] pumps = Array.Empty<RCP>();
						//Determine which Quadrant the section is (and the corresponding SteamGen)
						if(i < center) {

        	                _ = pumps.Append(rcps[0]);
            	            _ = pumps.Append(rcps[1]);

							if(j < center) {
								//top left
								receivingGen = steamGens[0];
								sendingGen = steamGens[1];
							}
							else {
								//bottom left
								receivingGen = steamGens[1];
								sendingGen = steamGens[0];
							}
						}
						else {

                        	_ = pumps.Append(rcps[2]);
                        	_ = pumps.Append(rcps[3]);

							if(j < center) {
								//top right
								receivingGen = steamGens[2];
								sendingGen = steamGens[3];
							}
							else {
								//bottom right
								receivingGen = steamGens[3];
								sendingGen = steamGens[2];
							}
						}


						reactorSections[i,j] = new ReactorSection(sendingGen, receivingGen, rcps);
						numberOfReactorSections++;
						}
					else reactorSections[i,j] = new ReflectorSection();
				}
			}

			return reactorSections;
	}

	public SteamGen[] CreateSteamGens() {
		SteamGen[] gens = new SteamGen[4];
		
		for (int i = 0; i < gens.Length; i++) gens[i] = new SteamGen();

		return gens;
	}

	public RCP[] CreateRCPs() {
		RCP[] rcps = new RCP[4];
		
		for (int i = 0; i < rcps.Length; i++) rcps[i] = new RCP();

		rcps[0].otherRCP = rcps[1];
		rcps[1].otherRCP = rcps[0];
		rcps[2].otherRCP = rcps[3];
		rcps[3].otherRCP = rcps[2];

		return rcps;
	}

}
