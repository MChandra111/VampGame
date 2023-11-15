using Godot;
using System;
//using System.Numerics;

public partial class PlayerController : CharacterBody2D
{
	public enum PlayerState
	{
		Idle,
		Running,
		Dashing,
		TakingDamage,
		Dead,
	}
	public PlayerState CurrentState = PlayerState.Idle;
	private const float Speed = 200.0f;
	private const float JumpVelocity = -400.0f;
	private const float DoubleJumpVelocity = -300.0f;
	private int JumpCount = 0;
	private int JumpMax = 1;
	private float dashSpeed = 400.0f;
	private bool isDashing = false;
	private float dashTimer = .2f;
	private float dashTimerReset = .2f;
	private bool isDashAvailable = true;
	private bool isWallJumping = false;
	private float wallJumpTimer = .45f;
	private float wallJumpTimerReset = .45f;
	public float maxHealth = 3;
	public float Health;
	private Vector2 velocity = Vector2.Zero;
	private int facingDirection = 0;
	private bool isTakingDamage = false;
	private bool isAttacking = false;
	private bool invul = false;
	private AnimatedSprite2D Anim;
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
	public int swordDamage = 1;

	[Signal]
	public delegate void DeathEventHandler();

	public override void _Ready()
	{
		Anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		Health = maxHealth;
	}

	public override void _PhysicsProcess(double delta)
	{
		velocity = Velocity;
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "Jump", "ui_down");

		InterfaceManager.UpdateHealth(maxHealth, Health);
		InterfaceManager.UpdateDashBar((float)GetNode<Timer>("DashTimer").WaitTime, (float)GetNode<Timer>("DashTimer").TimeLeft);

		//Input / State manager for resets and general logic calculation (Kinda messy but that's how it be)
		inputManager(direction, delta);
		switch (CurrentState)
		{
			case PlayerState.Idle:
				idle(direction, delta);
				break;
			case PlayerState.Running:
				processHorizontalMovement(direction, delta);
				break;
			case PlayerState.Dashing:
				processDash(delta);
				break;
			case PlayerState.TakingDamage:
				processTakingDamage();
				break;
			case PlayerState.Dead:
				velocity = new Vector2(0, 0);
				break;
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private void inputManager(Vector2 direction, double delta)
	{

		if (!IsOnFloor())
		{
			velocity.Y += gravity * (float)delta;
		}

		if (isTakingDamage)
		{
			CurrentState = PlayerState.TakingDamage;
			GetNode<Timer>("InvulTimer").Start();
		}

		if (Health <= 0)
		{
			CurrentState = PlayerState.Dead;
		}

		// Handle Jump
		if (JumpCount < JumpMax)
		{
			if (Input.IsActionJustPressed("Jump"))
			{
				velocity.Y = JumpVelocity;
				JumpCount += 1;
				//Double Jump
				if (Input.IsActionJustPressed("Jump") && !IsOnFloor() && JumpCount >= 1)
				{
					velocity.Y = DoubleJumpVelocity;
				}
			}
		}

		//Resetting jump count
		if (IsOnFloor() && JumpCount != 0)
		{
			JumpCount = 0;
		}

		if (Health > 0 && !isTakingDamage)
		{

			if (Input.IsActionPressed("ui_right"))
			{
				if (!isWallJumping)
				{
					Anim.FlipH = false;
				}
				if (!isAttacking)
				{
					Anim.Play("Run");
				}
				facingDirection = 1;
				CurrentState = PlayerState.Running;
			}
			if (Input.IsActionPressed("ui_left"))
			{
				if (!isWallJumping)
				{
					Anim.FlipH = true;
				}
				if (!isAttacking)
				{
					Anim.Play("Run");
				}
				facingDirection = -1;
				CurrentState = PlayerState.Running;
			}
			if (Input.IsActionJustPressed("Dash"))
			{
				CurrentState = PlayerState.Dashing;
			}

			processWallJump(delta);

			if (direction == Vector2.Zero)
			{
				CurrentState = PlayerState.Idle;
			}

			if (!isTakingDamage && !isAttacking)
			{
				if (velocity.Y > 0)
				{
					Anim.Play("Fall");
				}
				if (velocity.Y < 0 && JumpCount == 0)
				{
					Anim.Play("Jump");
				}
				if (velocity.Y < 0 && JumpCount == 1)
				{
					Anim.Play("DoubleJump");
				}
			}

			if (Input.IsActionJustPressed("Attack") && !isAttacking && !isDashing)
			{
				processAttack(delta);
			}

			if (isDashing)
			{
				Anim.Play("Dash");
				dashTimer -= (float)delta;
				if (dashTimer <= 0)
				{
					isDashing = false;
					velocity = new Vector2(velocity.X, 0);
				}
			}

			if (!GetNode<Timer>("InvulTimer").IsStopped())
			{
				this.SetCollisionMaskValue(3, false);
				this.SetCollisionLayerValue(2, false);
			}
			if (GetNode<Timer>("InvulTimer").IsStopped())
			{
				this.SetCollisionMaskValue(3, true);
				this.SetCollisionLayerValue(2, true);
			}

			if (GetNode<Timer>("DashTimer").IsStopped() && IsOnFloor())
			{
				isDashAvailable = true;
			}

			if (GetNode<Timer>("AttackTimer").IsStopped())
			{
				isAttacking = false;
			}

			if (isAttacking)
			{
				GetNode<CollisionShape2D>("AnimatedSprite2D/Area2D/AttackHitBox").Disabled = false;
			}
			if (!isAttacking)
			{
				GetNode<CollisionShape2D>("AnimatedSprite2D/Area2D/AttackHitBox").Disabled = true;
			}

			if (facingDirection == 1)
			{
				GetNode<CollisionShape2D>("AnimatedSprite2D/Area2D/AttackHitBox").Position = new Vector2(13, 2);
			}
			if (facingDirection == -1)
			{
				GetNode<CollisionShape2D>("AnimatedSprite2D/Area2D/AttackHitBox").Position = new Vector2(-13, 2);
			}
		}
	}

	private void idle(Vector2 direction, double delta)
	{
		if (direction == Vector2.Zero)
		{
			if (IsOnFloor() && !isAttacking)
			{
				Anim.Play("Idle");
			}
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		}
	}

	private void processHorizontalMovement(Vector2 direction, double delta)
	{
		if (!isDashing && !isWallJumping && !isTakingDamage && direction != Vector2.Zero)
		{
			velocity.X = direction.X * Speed;
		}
	}

	private void processWallJump(double delta)
	{
		//WallJumping
		if (Input.IsActionJustPressed("Jump") && GetNode<RayCast2D>("RayCastLeft").IsColliding() && !IsOnFloor())
		{
			velocity.Y = JumpVelocity;
			velocity.X = -JumpVelocity / 2;
			isWallJumping = true;
			Anim.FlipH = false;
			Anim.Play("Jump");
			JumpCount = 0;
		}
		else if (Input.IsActionJustPressed("Jump") && GetNode<RayCast2D>("RayCastRight").IsColliding() && !IsOnFloor())
		{
			velocity.Y = JumpVelocity;
			velocity.X = JumpVelocity / 2;
			isWallJumping = true;
			Anim.FlipH = true;
			Anim.Play("Jump");
			JumpCount = 0;
		}

		if (isWallJumping)
		{
			wallJumpTimer -= (float)delta;
			if (wallJumpTimer <= 0)
			{
				isWallJumping = false;
				wallJumpTimer = wallJumpTimerReset;
			}
		}
	}

	private void processDash(double delta)
	{
		//Dashing
		if (isDashAvailable)
		{
			if (Input.IsActionPressed("ui_right"))
			{
				velocity.X = dashSpeed;
				isDashing = true;
			}
			if (Input.IsActionPressed("ui_left"))
			{
				velocity.X = -dashSpeed;
				isDashing = true;
			}

			GetNode<Timer>("InvulTimer").Start();
			GetNode<Timer>("DashTimer").Start();
			GetNode<Timer>("AttackTimer").Stop();

			dashTimer = dashTimerReset;
			isDashAvailable = false;
		}
	}

	private void processTakingDamage()
	{
		if ((isTakingDamage && Velocity.Y == 0 && IsOnFloor()) || (Input.IsActionJustPressed("Jump") && (JumpCount < JumpMax)))
		{
			isTakingDamage = false;
			GetNode<Timer>("InvulTimer").Stop();
		}
	}

	private void processAttack(double delta)
	{
		if (GetNode<Timer>("AttackTimer").IsStopped())
		{
			GetNode<Timer>("AttackTimer").Start();
			Anim.Play("Attack");
			isAttacking = true;
		}
	}

	//Stopping death animation
	private void _on_animated_sprite_2d_animation_finished()
	{
		if (Anim.Animation == "Death")
		{
			Anim.Stop();
			Hide();
			EmitSignal(SignalName.Death);
		}

		if (Anim.Animation == "Attack")
		{
			Anim.Play("Idle");
		}
	}

	//AttackingLogic
	private void _on_area_2d_body_entered(Node2D body)
	{
		if (body is TurretEnemy)
		{
			TurretEnemy enemy = body as TurretEnemy;
			enemy.TakeDamage(swordDamage);
		}
		if (body is SlimeEnemy)
		{
			SlimeEnemy enemy = body as SlimeEnemy;
			enemy.TakeDamage(swordDamage);
		}
		if (body is GoblinEnemy)
		{
			GoblinEnemy enemy = body as GoblinEnemy;
			enemy.TakeDamage(swordDamage);
		}
		if (body is Arrow){
			Arrow arrow = body as Arrow;
			arrow.DestroyArrow();
		}
	}


	//Public functions that other stuff interacts with

	//Taking Damage code
	public void TakeDamage()
	{
		if (GetNode<Timer>("InvulTimer").IsStopped())
		{
			if (Health > 0)
			{
				Health -= 1;
				Velocity = new Vector2(100f * -facingDirection, -300);
				MoveAndSlide();
				isTakingDamage = true;
				Anim.Play("TakeDamage");
				if (Health <= 0)
				{
					Health = 0;
					GetNode<Timer>("InvulTimer").Start();
					Anim.Play("Death");
				}
				GetNode<Timer>("InvulTimer").Start();
			}
		}
	}

	//Respawning player code
	public void RespawnPlayer()
	{
		Show();
		Health = maxHealth;
		Velocity = new Vector2(0, 0);
		GetTree().ReloadCurrentScene();
	}

	//Healing player code
	public void RestorePlayer()
	{
		Health = maxHealth;
	}
}
