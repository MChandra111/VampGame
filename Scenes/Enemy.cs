using Godot;
using System;

public partial class Enemy : RigidBody2D
{
	private PlayerController Player;
	private bool Active;
	private bool AbleToShoot;
	private float ShootTimer = 0.7f;
	private float ShootTimerReset = 0.7f;
	private int Health = 3;
	private bool isTakingDamage = false;
	[Export]
	public PackedScene Arrow;

	public bool isShooting = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Health > 0)
		{
			if (Active && !isTakingDamage)
			{
				var angle = GlobalPosition.AngleToPoint(Player.GlobalPosition);
				if (Math.Abs(angle) > Mathf.Pi / 2)
				{
					GetNode<AnimatedSprite2D>("AnimatedSprite2D").FlipH = true;
				}
				else
				{
					GetNode<AnimatedSprite2D>("AnimatedSprite2D").FlipH = false;
				}
				if (AbleToShoot)
				{
					var SpaceState = GetWorld2D().DirectSpaceState;
					var query = PhysicsRayQueryParameters2D.Create(this.Position, Player.Position);
					Godot.Collections.Dictionary result = SpaceState.IntersectRay(query);
					if (result != null)
					{
						if (result.ContainsKey("collider"))
						{
							this.GetNode<Marker2D>("Projectile").LookAt(Player.Position);
							if (result["collider"].AsGodotObject() == Player)
							{
								Arrow arrow = Arrow.Instantiate() as Arrow;
								Owner.AddChild(arrow);
								arrow.GlobalTransform = this.GetNode<Marker2D>("Projectile").GlobalTransform;
								AbleToShoot = false;
								ShootTimer = ShootTimerReset;
							}
						}
					}
				}
				GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("Shooting");
				isShooting = true;
			}
			else
			{
				if (!isShooting && !isTakingDamage)
				{
					GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("Idle");
				}
			}

			if (GetNode<Timer>("AttackedTimer").IsStopped())
			{
				isTakingDamage = false;
			}

			if (ShootTimer <= 0)
			{
				AbleToShoot = true;
			}
			else
			{
				ShootTimer -= (float)delta;
			}
		}
	}

	private void _on_detection_radius_body_entered(CharacterBody2D body)
	{
		if (body is PlayerController)
		{
			Player = body as PlayerController;
			Active = true;
		}
	}

	private void _on_detection_radius_body_exited(CharacterBody2D body)
	{
		if (body is PlayerController)
		{
			Active = false;
			isShooting = false;
		}
	}

	public void TakeDamage(int swordDamage)
	{
		if (Health > 0)
		{
			GetNode<Timer>("AttackedTimer").Start();
			isTakingDamage = true;
			Health -= swordDamage;
			GD.Print("ENEMY HEALTH" + Health);
			GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("TakeDamage");
			if (Health <= 0)
			{
				Health = 0;
				GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("Death");
			}
		}
	}

	private void _on_animated_sprite_2d_animation_finished()
	{
		if (GetNode<AnimatedSprite2D>("AnimatedSprite2D").Animation == "Death")
		{
			GetNode<AnimatedSprite2D>("AnimatedSprite2D").Stop();
			Hide();
			QueueFree();
		}
	}
}
