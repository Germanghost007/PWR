using Godot;
using System;

public partial class ReactorSection : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		prevPower = power;
	}

	public ReactorSection(SteamGen sender, SteamGen receiver, RCP[] pumps) {
		sendingSteamGen = sender;
		receivingSteamGen = receiver;
	}

	
	public RCP[] rcps;
	public SteamGen sendingSteamGen;
	public SteamGen receivingSteamGen;

	public double[] rods;
	public double inflowTemp;
	public double outflowTemp;
	public double power;
	public double prevPower;
	public double waterflow;
}
