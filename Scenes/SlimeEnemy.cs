using Godot;
using System;

public partial class SlimeEnemy : CharacterBody2D
{
	private int Health = 3;
	public bool isTakingDamage = false;
	Sprite2D sprite;
	RayCast2D bottomLeft;
	RayCast2D bottomRight;
	RayCast2D LeftMiddle;
	RayCast2D RightMiddle;
	private Vector2 velocity = new Vector2(0, 0);
	private int speed = 30;
	private PlayerController Player;
	private bool Active;
	private bool isChasing;
	private AnimationPlayer AnimPlayer;
	private int facingDirection = 1;
	public bool AbleToChase;


	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	public override void _Ready()
	{
		sprite = GetNode<Sprite2D>("Sprite2D");
		bottomLeft = GetNode<RayCast2D>("RayCastLeft");
		bottomRight = GetNode<RayCast2D>("RayCastRight");
		AnimPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		LeftMiddle = GetNode<RayCast2D>("LeftRay");
		RightMiddle = GetNode<RayCast2D>("RightRay");
		GetNode<AnimatedSprite2D>("AnimatedSprite2D").Hide();
		velocity.X = speed;
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y += gravity * (float)delta;

		if (velocity.Y > gravity)
		{
			velocity.Y = gravity;
		}

		if (!bottomRight.IsColliding())
		{
			velocity.X = -speed;
			sprite.FlipH = false;
			facingDirection = -1;
		}
		else if (!bottomLeft.IsColliding())
		{
			velocity.X = speed;
			sprite.FlipH = true;
			facingDirection = 1;
		}
		else if (RightMiddle.IsColliding())
		{
			velocity.X = -speed;
			sprite.FlipH = false;
			facingDirection = -1;
		}
		else if (LeftMiddle.IsColliding())
		{
			velocity.X = speed;
			sprite.FlipH = true;
			facingDirection = 1;
		}

		if (facingDirection == -1 && isChasing && !isTakingDamage)
		{
			velocity.X = -speed;
		}
		else if (facingDirection == 1 && isChasing && !isTakingDamage)
		{
			velocity.X = speed;
		}

		if (!AnimPlayer.IsPlaying())
		{
			AnimPlayer.Play("Move");
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	public override void _Process(double delta)
	{

		if (isChasing)
		{
			speed = 50;
		}
		if (!isChasing)
		{
			speed = 30;
		}

		if (Health > 0)
		{
			if (Active && isChasing && !isTakingDamage)
			{
				var angle = GlobalPosition.AngleToPoint(Player.GlobalPosition);
				if (Math.Abs(angle) > Mathf.Pi / 2)
				{
					sprite.FlipH = false;
					velocity.X = -speed;
					facingDirection = -1;
				}
				else
				{
					sprite.FlipH = true;
					velocity.X = speed;
					facingDirection = 1;
				}

				if (isTakingDamage)
				{
					isChasing = false;
				}
			}

			if (GetNode<Timer>("AttackedTimer").IsStopped())
			{
				isTakingDamage = false;
			}
		}
	}
	public void _on_area_2d_body_entered(Node2D body)
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

	public void _on_detection_radius_body_entered(Node2D body)
	{
		if (body is PlayerController)
		{
			Player = body as PlayerController;
			Active = true;
			isChasing = true;
		}
	}

	public void _on_detection_radius_body_exited(Node2D body)
	{
		if (body is PlayerController)
		{
			Active = false;
			isChasing = false;
		}
	}

	public void TakeDamage(int swordDamage)
	{
		if (Health > 0)
		{
			GetNode<Timer>("AttackedTimer").Start();
			isTakingDamage = true;
			Health -= swordDamage;

			Velocity = new Vector2(100f * -facingDirection, -100);

			MoveAndSlide();
			if (Health <= 0)
			{
				Health = 0;
				sprite.Hide();
				GetNode<AnimatedSprite2D>("AnimatedSprite2D").Show();
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
