using Godot;
using System;
using System.Security.Cryptography.X509Certificates;

public partial class GameManager : Node2D
{
	[Export]
	public Marker2D RespawnPoint;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		RespawnPoint = GetNode<Marker2D>("RespawnPoint");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	public void RespawnPlayer()
	{
		PlayerController pc = GetNode<PlayerController>("Player");
		pc.GlobalPosition = RespawnPoint.GlobalPosition;
		pc.RespawnPlayer();
	}

	private void _on_player_death()
	{
		RespawnPlayer();
	}
}
