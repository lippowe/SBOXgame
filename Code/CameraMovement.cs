using Sandbox;


public sealed class CameraMovement : Component
{

	//Properties

	[Property] public PlayerMovement Player { get; set; }
	[Property] public GameObject Body { get; set; }
	[Property] public GameObject Head { get; set; }
	[Property] public float Distance { get; set; } = 0f;

	public bool isFirstPerson => Distance == 0f;

	private Vector3 CurrentOffset = Vector3.Zero;

	private CameraComponent Camera;

	private ModelRenderer bodyRenderer;
	protected override void OnAwake()
	{
		Camera = Components.Get<CameraComponent>();
		bodyRenderer = Body.Components.Get<ModelRenderer>();
	}

	protected override void OnUpdate()
	{
		//mouse moviement
		var eyeAngles = Head.WorldTransform.Rotation.Angles();
		eyeAngles.pitch += Input.MouseDelta.y * 0.1f;
		eyeAngles.yaw -= Input.MouseDelta.x * 0.1f;
		eyeAngles.roll = 0f;
		eyeAngles.pitch = eyeAngles.pitch.Clamp( -89.9f, 89.9f );
		Head.Transform.Rotation = eyeAngles.ToRotation();

		//curent camera offset
		var targetOffset = Vector3.Zero;
		if ( Player.IsCrouching ) targetOffset += Vector3.Down * 32f;
		CurrentOffset = Vector3.Lerp( CurrentOffset, targetOffset, Time.Delta * 10f );

		if ( Camera is not null )
		{
			var camPos = Head.WorldTransform.Position + CurrentOffset;
			if(!isFirstPerson)
			{
				var camForward = eyeAngles.ToRotation().Forward;
				var camTrace = Scene.Trace.Ray(camPos, camPos - (camForward * Distance) )
					.WithoutTags( "player", "trigger" )
					.Run();
					
				if ( camTrace.Hit )
				{
					camPos = camTrace.HitPosition + camTrace.Normal;
				}
				else
				{
					camPos = camTrace.EndPosition;
				}

				bodyRenderer.RenderType = ModelRenderer.ShadowRenderType.On;
			}
			else
			{
				bodyRenderer.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
			}

			// Show body in the first person
			


			//Calculeted position
			Camera.Transform.Position = camPos;
			Camera.Transform.Rotation = eyeAngles.ToRotation();
		}
	}
}
