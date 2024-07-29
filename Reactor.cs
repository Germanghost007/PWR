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
		ReactorMatrix = GetReactorSections(8, 1);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}


	double[,] BaseTransferMatrix = GetBaseTransferMatrix();
	double[,] BaseReflectionMatrix = GetBaseTransferMatrix();
	double ratedReactorPower = 3200;
	double heatingMultiplier = 0.7548875;
	double idlePower = 1;


	object[,] ReactorMatrix;

	public double CalcFactors(double RodPos) {
		return 2 * RodPos / 100;
	}

	public double CalcPower(double Factors, double reactorPower, double prevReactorPower) {
		double power = (idlePower + prevReactorPower) * Factors;
		prevReactorPower = reactorPower;

		return power;
	}

	public double CalcOutletTemp(double power, double waterflow, double waterinlettemp) {
		double heatingPower = power * heatingMultiplier;
		double tempRaise = ( (heatingPower*1000) / (waterflow/1000) ) / 4200;
		return waterinlettemp + tempRaise;
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

	public object[,] GetReactorSections(int diameter, int neutronTransferDistance) {
			object[,] reactorSections = new object[diameter + 2*neutronTransferDistance, diameter + 2*neutronTransferDistance];

			for (int i = 0; i < reactorSections.GetLength(0); i++) {
				for (int j = 0; j < reactorSections.GetLength(1); j++) {
					double distance = Math.Sqrt((i - (reactorSections.GetLength(0) - 1) / 2) + (j - (reactorSections.GetLength(1) - 1) / 2));
					if(distance <= diameter / 2) reactorSections[i,j] = new ReactorSection();
					else reactorSections[i,j] = new ReflectorSection();
				}
			}

			return reactorSections;
	}

}
