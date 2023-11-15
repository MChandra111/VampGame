using Godot;
using System;

public partial class TurretEnemy : CharacterBody2D
{
	private PlayerController Player;
	private int Speed = 2;
	private bool Active;
	private bool AbleToShoot;
	private float ShootTimer = 1.3f;
	private float ShootTimerReset = 1.3f;
	private int Health = 3;
	private bool isTakingDamage = false;
	[Export]
	public PackedScene Arrow;

	public bool isShooting = false;

	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;
		if (!IsOnFloor())
		{
			velocity.Y += gravity * (float)delta;
		}

		velocity.X = Mathf.Lerp(Velocity.X, 0, (float)delta * Speed);

		Velocity = velocity;
		MoveAndSlide();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Health > 0)
		{
			if (Active)
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
				if (AbleToShoot && !isTakingDamage)
				{
					var SpaceState = GetWorld2D().DirectSpaceState;
					var query = PhysicsRayQueryParameters2D.Create(this.Position, Player.Position, this.CollisionMask);
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
				if (!isTakingDamage){
					GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("Shooting");
					isShooting = true;
				}
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

	private void _on_detection_radius_body_entered(Node2D body)
	{
		if (body is PlayerController)
		{
			Player = body as PlayerController;
			Active = true;
		}
	}

	private void _on_detection_radius_body_exited(Node2D body)
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
			if (GetNode<AnimatedSprite2D>("AnimatedSprite2D").FlipH == true)
			{
				Velocity = new Vector2(100f, -100);
			}
			if (GetNode<AnimatedSprite2D>("AnimatedSprite2D").FlipH == false)
			{
				Velocity = new Vector2(-100f, -100);
			}

			MoveAndSlide();
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

	private void _on_area_2d_body_entered(Node2D body)
	{
		if (body is CharacterBody2D)
		{
			if (body is PlayerController)
			{
				if (Health > 0)
				{
					PlayerController pc = body as PlayerController;
					pc.TakeDamage();
				}
			}
		}
	}
}
