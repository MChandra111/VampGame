using Godot;
using System;

public partial class PlayerController : CharacterBody2D
{
	private const float Speed = 200.0f;
	private const float JumpVelocity = -400.0f;
	private const float DoubleJumpVelocity = -300.0f;
	private int JumpCount = 0;
	private int JumpMax = 1;
	private float dashSpeed = 800.0f;
	private bool isDashing = false;
	private float dashTimer = .2f;
	private float dashTimerReset = .2f;
	private bool isDashAvailable = false;
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
		Vector2 velocity = Velocity;
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "Jump", "ui_down");

		if (Health > 0){
			if (!IsOnFloor()){
				velocity.Y += gravity * (float)delta;
			}

			// Handle Jump
			if (JumpCount < JumpMax){
				if (Input.IsActionJustPressed("Jump")){
					velocity.Y = JumpVelocity;
					JumpCount += 1;

					//Double Jump
					if (Input.IsActionJustPressed("Jump")  && !IsOnFloor() && JumpCount >= 1){
						velocity.Y = DoubleJumpVelocity;
					}
				}
			}
			

			//Resetting jump count
			if(IsOnFloor() && JumpCount != 0){
				JumpCount = 0;
			}

			//Dash Reset
			if(IsOnFloor()){
				isDashAvailable = true;
			}

			//WallJumping
			if (Input.IsActionJustPressed("Jump") && GetNode<RayCast2D>("RayCastLeft").IsColliding() && !IsOnFloor()){
				velocity.Y = JumpVelocity;
				velocity.X = -JumpVelocity/2;
				isWallJumping = true;
				Anim.FlipH = false;
				Anim.Play("Jump");
			} else if (Input.IsActionJustPressed("Jump") && GetNode<RayCast2D>("RayCastRight").IsColliding() && !IsOnFloor()){
				velocity.Y = JumpVelocity;
				velocity.X = JumpVelocity/2;
				isWallJumping = true;
				Anim.FlipH = true;
				Anim.Play("Jump");
			}

			if (isWallJumping){
				wallJumpTimer -= (float)delta;
				if (wallJumpTimer <= 0){
					isWallJumping = false;
					wallJumpTimer = wallJumpTimerReset;
				}
			}

			// Horizontal movement handling
			//Movement only allowed if not dashing
			if (!isDashing && !isWallJumping && !isTakingDamage){
				if (direction != Vector2.Zero)
				{
						velocity.X = direction.X * Speed;
						if (Input.IsActionPressed("ui_right")){
							Anim.FlipH = false;
							if (Input.IsActionJustPressed("Dash")){
								Anim.Play("Dash");
							}
							Anim.Play("Run");
							facingDirection = 1;
						}
						if (Input.IsActionPressed("ui_left")){
							Anim.FlipH = true;
							Anim.Play("Run");
							facingDirection = -1;
						}
				}
				else
				{
					if(IsOnFloor()){
						Anim.Play("Idle");
					}
					velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
				}
			}


			if((isTakingDamage && velocity.Y == 0) || Input.IsActionJustPressed("Jump") || Input.IsActionJustPressed("Dash")){
				isTakingDamage = false;
			}

			//Logic for falling/jumping animations (comes after horizontal movement, 
			// since vertical animations look better in the air while falling)
			if (!isTakingDamage){
				if (velocity.Y > 0){
					Anim.Play("Fall");
				}
				if (velocity.Y < 0 && JumpCount == 0){
					Anim.Play("Jump");
				}
				if (velocity.Y < 0 && JumpCount == 1 && !GetNode<RayCast2D>("RayCastLeft").IsColliding() && !GetNode<RayCast2D>("RayCastRight").IsColliding()){
					Anim.Play("DoubleJump");
				}
			}

			//Dashing
			if (isDashAvailable){
				if (Input.IsActionJustPressed("Dash")){
					if (Input.IsActionPressed("ui_right")){
						velocity.X = dashSpeed;
						isDashing = true;
					}
					if (Input.IsActionPressed("ui_left")){
						velocity.X = -dashSpeed;
						isDashing = true;
					}

					dashTimer = dashTimerReset;
					isDashAvailable = false;
				}
			}

			if(isDashing){
				dashTimer -= (float)delta;
				if(dashTimer <= 0){
					isDashing = false;
					velocity = new Vector2(0, 0);
				}
			}

			Velocity = velocity;
			MoveAndSlide();
		}
	}

	//Taking Damage code
	public void TakeDamage(){
		if (Health > 0){
			Health -= 1;
			GD.Print(Health);
			Velocity = new Vector2(100f * -facingDirection, -300);
			MoveAndSlide();
			isTakingDamage = true;
			Anim.Play("TakeDamage");
			if (Health <= 0){
				Health = 0;
				Anim.Play("Death");
			}
		}
	}

	//Stopping death animation
	private void _on_animated_sprite_2d_animation_finished(){
		if(Anim.Animation == "Death"){
			Anim.Stop();
			Hide();
			EmitSignal(SignalName.Death);
		}
	}

	//Respawning player code
	public void RespawnPlayer(){
		Show();
		Health = 3;
		Velocity = new Vector2(0, 0);
	}
}
