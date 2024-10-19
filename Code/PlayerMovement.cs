using Sandbox;
using Sandbox.Citizen;
using System;
using System.Numerics;

public sealed class PlayerMovement : Component
{
	[Property] public float GroundControl { get; set; } = 4.0f;
	[Property] public float AirControl { get; set; } = 0.1f;
	[Property] public float MaxForce { get; set; } = 50f;
	[Property] public float Speed { get; set; } = 160f;
	[Property] public float RunSpeed { get; set; } = 290f;
	[Property] public float CrounchSpeed { get; set; } = 90f;
	[Property] public float JumpForce { get; set; } = 400f;

	//Object
	[Property] public GameObject Head { get; set; }
	[Property] public GameObject Body { get; set; }

	//member
	public Vector3 WishVelocity = Vector3.Zero;

	public bool IsCrouching = false;
	public bool IsSprinting = false;

	private CharacterController characterController;
	private CitizenAnimationHelper citizenAnimationHelper;


	protected override void OnAwake()
	{
		characterController = Components.Get<CharacterController>();
		citizenAnimationHelper = Components.Get<CitizenAnimationHelper>();
	}


	protected override void OnUpdate()
	{

		UpdateCrounch();
		IsSprinting = Input.Down( "Run" );
		if(Input.Pressed("Jump")) Jump(); 

		RotateBody();
		UpdateAnimation();
	}

	protected override void OnFixedUpdate()
	{
		BuildWishVelocity();
		Move();
	
	}

	void BuildWishVelocity()
	{
		WishVelocity = 0;

		var rot = Head.WorldTransform.Rotation;
		if ( Input.Down( "Forward" ) ) WishVelocity += rot.Forward;
		if ( Input.Down( "Backward" ) ) WishVelocity += rot.Backward;
		if ( Input.Down( "Left" ) ) WishVelocity += rot.Left;
		if ( Input.Down( "Right" ) ) WishVelocity += rot.Right;

		WishVelocity = WishVelocity.WithZ( 0 );
		if ( !WishVelocity.IsNearZeroLength ) WishVelocity = WishVelocity.Normal;

		if ( IsCrouching ) WishVelocity *= CrounchSpeed;
		else if ( IsSprinting ) WishVelocity *= RunSpeed;
		else WishVelocity *= Speed;
	}

	void Move()
	{
		//Gravity
		var gravity = Scene.PhysicsWorld.Gravity;

		if ( characterController.IsOnGround )
		{
			//Acceleration
			characterController.Velocity = characterController.Velocity.WithZ( 0 );
			characterController.Accelerate( WishVelocity );
			characterController.ApplyFriction( GroundControl );
		}
		else
		{
			//Air control gravity
			characterController.Velocity += gravity * Time.Delta * 0.5f;
			characterController.Accelerate( WishVelocity.ClampLength( MaxForce ) );
			characterController.ApplyFriction( AirControl );
		}

		// Move player
		characterController.Move();

		if ( !characterController.IsOnGround )
		{
			characterController.Velocity += gravity * Time.Delta * 0.5f;
		}
		else
		{
			characterController.Velocity = characterController.Velocity.WithZ( 0 );
		}
	}

	void RotateBody()
	{
		if ( Body is null ) return;

		var targetAngle = new Angles( 0, Head.LocalTransform.Rotation.Yaw(), 0 ).ToRotation();
		float rotateDifference = Body.LocalTransform.Rotation.Distance( targetAngle );

		if ( rotateDifference > 50f || characterController.Velocity.Length > 10f )
		{
			Body.Transform.Rotation = Rotation.Lerp( Body.Transform.Rotation, targetAngle, Time.Delta * 2f );
		}
	}

	void Jump()
	{
		if ( !characterController.IsOnGround ) return;

		characterController.Punch( Vector3.Up * JumpForce );
		citizenAnimationHelper?.TriggerJump();
	}

	void UpdateAnimation()
	{
		if(citizenAnimationHelper is null) return;

		citizenAnimationHelper.WithWishVelocity( WishVelocity );
		citizenAnimationHelper.WithVelocity(characterController.Velocity );
		citizenAnimationHelper.AimAngle = Head.Transform.Rotation;
		citizenAnimationHelper.IsGrounded = characterController.IsOnGround;
		citizenAnimationHelper.WithLook( Head.Transform.Rotation.Forward, 1f, 0.75f, 0.5f );
		citizenAnimationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Run;
		citizenAnimationHelper.DuckLevel = IsCrouching ? 1f : 0f; 
	}

	void UpdateCrounch()
	{
		if(characterController is null) return;

		if(Input.Pressed("Crouch") && !IsCrouching)
		{
			IsCrouching = true;
			characterController.Height /= 2f;
		}

		if ( Input.Released( "Crouch" ) && IsCrouching )
		{
			IsCrouching = false;
			characterController.Height *= 2f;
		}
	}
}
