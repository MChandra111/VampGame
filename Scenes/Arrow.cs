using Godot;
using System;

public partial class Arrow : Node2D
{
	private int speed = 150;
	private float lifeSpan = 20;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Position += Transform.X * (float)delta * speed;
		lifeSpan -= (float)delta;
		if (lifeSpan < 0){
			QueueFree();
		}
	}

	private void _on_area_2d_body_entered(CharacterBody2D body){
		QueueFree();
		if(body is CharacterBody2D){
			if(body is PlayerController){
				PlayerController pc = body as PlayerController;
				pc.TakeDamage();
			}
		}
	}
	
	private void _on_area_2d_body_entered_by_tilemap(TileMap body){
		QueueFree();
	}
}
