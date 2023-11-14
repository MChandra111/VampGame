using Godot;
using System;
//using System.Numerics;

public partial class PlayerController : CharacterBody2D
{
	public enum PlayerState
	{
		Idle,
		Running,
		Attacking,
		Dashing,
		TakingDamage,
		Dead,
		WallJump
	}
	public PlayerState CurrentState = PlayerState.Idle;
	private const float Speed = 200.0f;
	private const float JumpVelocity = -400.0f;
	private const float DoubleJumpVelocity = -300.0f;
	private int JumpCount = 0;
	private int JumpMax = 1;
	private float dashSpeed = 800.0f;
	private bool isDashing = false;
	private float dashTimer = .2f;
	private float dashTimerReset = .2f;
	private bool isDashAvailable = true;
	private bool isWallJumping = false;
	private float wallJumpTimer = .45f;
	private float wallJumpTimerReset = .45f;
	public int Health = 3;
	private Vector2 velocity = Vector2.Zero;
	private int facingDirection = 0;
	private bool isTakingDamage = false;
	private AnimatedSprite2D Anim;
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	[Signal]
	public delegate void DeathEventHandler();

	public override void _Ready()
	{
		Anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	}

	public override void _PhysicsProcess(double delta)
	{
		velocity = Velocity;
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "Jump", "ui_down");

		inputManager(direction, delta);
		switch (CurrentState)
		{
			case PlayerState.Idle:
				idle(direction, delta);
				break;
			case PlayerState.Attacking:
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
		if (!IsOnFloor()){
			velocity.Y += gravity * (float)delta;
		}

		if (isTakingDamage){
			CurrentState = PlayerState.TakingDamage;
		}

		if (Health <= 0){
			CurrentState = PlayerState.Dead;
		}

		if (Health > 0 && !isTakingDamage){
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

			if (Input.IsActionPressed("Attack"))
			{
				CurrentState = PlayerState.Attacking;
			}
			if (Input.IsActionPressed("ui_right"))
			{
				if (!isWallJumping){
					Anim.FlipH = false;
				}
				Anim.Play("Run");
				facingDirection = 1;
				CurrentState = PlayerState.Running;
			}
			if (Input.IsActionPressed("ui_left"))
			{
				if (!isWallJumping){
					Anim.FlipH = true;
				}
				Anim.Play("Run");
				facingDirection = -1;
				CurrentState = PlayerState.Running;
			}
			if (Input.IsActionPressed("Dash"))
			{
				CurrentState = PlayerState.Dashing;
			}

			processWallJump(delta);

			if (direction == Vector2.Zero){
				CurrentState = PlayerState.Idle;
			}

			if (!isTakingDamage){
				if (velocity.Y > 0){
					Anim.Play("Fall");
				}
				if (velocity.Y < 0 && JumpCount == 0){
					Anim.Play("Jump");
				}
				if (velocity.Y < 0 && JumpCount == 1){
					Anim.Play("DoubleJump");
				}
			}

			if (IsOnFloor() && !isDashing){
				isDashAvailable = true;
			}

			if (isDashing)
			{
				dashTimer -= (float)delta;
				if (dashTimer <= 0)
				{
					isDashing = false;
					velocity = new Vector2(0, 0);
				}
			}
		}
	}

	private void idle(Vector2 direction, double delta)
	{
		if (direction == Vector2.Zero)
		{
			if (IsOnFloor())
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

			dashTimer = dashTimerReset;
			isDashAvailable = false;
		}
	}

	private void processTakingDamage()
	{
		if ((isTakingDamage && Velocity.Y == 0) || Input.IsActionJustPressed("Jump") || Input.IsActionJustPressed("Dash"))
		{
			isTakingDamage = false;
		}
	}

	//Taking Damage code
	public void TakeDamage()
	{
		if (Health > 0)
		{
			Health -= 1;
			GD.Print(Health);
			Velocity = new Vector2(100f * -facingDirection, -300);
			MoveAndSlide();
			isTakingDamage = true;
			Anim.Play("TakeDamage");
			if (Health <= 0)
			{
				Health = 0;
				Anim.Play("Death");
			}
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
	}

	//Respawning player code
	public void RespawnPlayer()
	{
		Show();
		Health = 3;
		Velocity = new Vector2(0, 0);
	}
}
