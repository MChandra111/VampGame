using Godot;
using System;

public partial class InterfaceManager : CanvasLayer
{
	public static ProgressBar HealthBar;
	public static ProgressBar DashBar;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		HealthBar = GetNode("MainInterface/HealthBar") as ProgressBar;
		DashBar = GetNode("MainInterface/DashBar") as ProgressBar;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public static void UpdateHealth(float maxHealth, float health)
	{
		HealthBar.Value = health / maxHealth * HealthBar.MaxValue;
	}

	public static void UpdateDashBar(float maxTime, float timeLeft)
	{
		DashBar.Value = timeLeft / maxTime * DashBar.MaxValue;
	}
}
