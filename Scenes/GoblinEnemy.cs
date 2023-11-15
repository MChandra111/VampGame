using Godot;
using System;
using System.Collections;

public partial class GoblinEnemy : CharacterBody2D
{

	public const float Speed = 40.0f;
	private int Recovery = 5;
	private PlayerController Player;
	private int Health = 3;
	public bool isTakingDamage = false;
	private Vector2 velocity = new Vector2(0, 0);
	private bool isChasing;
	private bool AbleToChase;
	private int facingDirection = 1;
	private bool Active;
	private bool isAttacking = false;
	private bool playerIsInAttackRange = false;
	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle() / 2;

	public override void _Ready()
	{
		base._Ready();
	}

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
					facingDirection = -1;
				}
				else
				{
					GetNode<AnimatedSprite2D>("AnimatedSprite2D").FlipH = false;
					facingDirection = 1;
				}

				if (!isTakingDamage && isChasing)
				{
					var SpaceState = GetWorld2D().DirectSpaceState;
					var query = PhysicsRayQueryParameters2D.Create(this.Position, Player.Position, this.CollisionMask);
					Godot.Collections.Dictionary result = SpaceState.IntersectRay(query);
					if (result != null)
					{
						if (result.ContainsKey("collider"))
						{
							if (result["collider"].AsGodotObject() == Player)
							{
								isChasing = true;
								if (GetNode<AnimatedSprite2D>("AnimatedSprite2D").FlipH == false)
								{
									if (!playerIsInAttackRange && !isTakingDamage)
									{
										velocity.X = Speed;
									}
								}
								if (GetNode<AnimatedSprite2D>("AnimatedSprite2D").FlipH == true)
								{
									if (!playerIsInAttackRange && !isTakingDamage)
									{
										velocity.X = -Speed;
									}
								}
							}
							else
							{
								isChasing = false;
							}
						}
					}
				}
				if (!isAttacking && !isTakingDamage)
				{
					GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("Running");
				}
			}
			else
			{
				if (!isChasing && !isTakingDamage)
				{
					GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("Idle");
				}
			}

			if (GetNode<Timer>("AttackTimer").IsStopped())
			{
				isAttacking = false;
			}

			if (GetNode<Timer>("AttackLengthTimer").IsStopped())
			{
				GetNode<CollisionShape2D>("AttackHitbox/CollisionShape2D").Disabled = true;
			}
			else if (!GetNode<Timer>("AttackLengthTimer").IsStopped() && !isTakingDamage)
			{
				GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("Attacking");
			}

			if (facingDirection == 1)
			{
				GetNode<CollisionShape2D>("AttackHitbox/CollisionShape2D").Position = new Vector2(12, 0);
			}
			else if (facingDirection == -1)
			{
				GetNode<CollisionShape2D>("AttackHitbox/CollisionShape2D").Position = new Vector2(-12, 0);
			}

			if (playerIsInAttackRange && GetNode<Timer>("AttackTimer").IsStopped() && !isTakingDamage)
			{
				Attack();
			}

			if (!isChasing || isTakingDamage)
			{
				velocity.X = Mathf.Lerp(Velocity.X, 0, (float)delta * Recovery);
			}

			if (GetNode<Timer>("AttackedTimer").IsStopped())
			{
				isTakingDamage = false;
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsOnFloor())
			velocity.Y += gravity * (float)delta;

		if (velocity.Y > gravity)
		{
			velocity.Y = gravity;
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	//Detecting player entering detection radius
	public void _on_detection_radius_body_entered(Node2D body)
	{
		if (body is PlayerController)
		{
			Player = body as PlayerController;
			Active = true;
			isChasing = true;
		}
	}
	//Detecting player exiting detection radius (stops chasing player)
	public void _on_detection_radius_body_exited(Node2D body)
	{
		if (body is PlayerController)
		{
			Active = false;
			isChasing = false;
		}
	}

	//Detecting player entering attack radius (attacks player)
	public void _on_attack_radius_body_entered(Node2D body)
	{
		if (body is PlayerController)
		{
			playerIsInAttackRange = true;
		}
	}
	//Detecting player exiting attack radius (starts chasing instead of attacking nothing)
	public void _on_attack_radius_body_exited(Node2D body)
	{
		if (body is PlayerController)
		{
			playerIsInAttackRange = false;
		}
	}

	//Detecting player in attack hitbox and beating him up
	public void _on_attack_hitbox_body_entered(Node2D body)
	{
		if (body is PlayerController)
		{
			if (Health > 0)
			{
				Player.TakeDamage();
			}
		}
	}

	//Detecting if player touches enemy
	public void _on_hurt_box_body_entered(Node2D body)
	{
		if (body is PlayerController)
		{
			if (Health > 0)
			{
				Player.TakeDamage();
			}
		}
	}

	//Code to attack player
	private void Attack()
	{
		GetNode<Timer>("AttackTimer").Start();
		GetNode<Timer>("AttackLengthTimer").Start();
		GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("Attacking");
		GetNode<CollisionShape2D>("AttackHitbox/CollisionShape2D").Disabled = false;
		isAttacking = true;
	}

	//Take Damage
	public void TakeDamage(int swordDamage)
	{
		if (Health > 0)
		{
			GetNode<Timer>("AttackedTimer").Start();
			isTakingDamage = true;
			Health -= swordDamage;
			GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("TakeDamage");
			velocity = new Vector2(100f * -facingDirection, -100);
			MoveAndSlide();
			if (Health <= 0)
			{
				Health = 0;
				GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("Death");
			}
		}
	}

	public void _on_animated_sprite_2d_animation_finished()
	{
		if (GetNode<AnimatedSprite2D>("AnimatedSprite2D").Animation == "Death")
		{
			GetNode<AnimatedSprite2D>("AnimatedSprite2D").Stop();
			Hide();
			QueueFree();
		}
	}
}
