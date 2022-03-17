using Godot;

public enum LaunchType
{
	MouseButtonDown,
	TriggerVolume,
	OnStart
}

[Tool]
public class LaunchMRE : Spatial
{
	private LaunchType launchType = LaunchType.OnStart;

	[Export]
	public LaunchType LaunchType
	{
		get => launchType;
		set
		{
			if (launchType == value)
				return;
			launchType = value;
			UpdateEditorSceneWithLaunchType();
		}
	}

	public MREComponent MREComponent { get; set; } = new MREComponent();

	[Export]
	public bool StopAppOnExit = true;

	private bool _running = false;

	//MREComponent Properties
	[Export]
	public string MREURL;

	[Export]
	public string SessionID;

	[Export]
	public string AppID;

	[Export]
	public string EphemeralAppID;

	[Export]
	public bool AutoStart = false;

	[Export]
	public bool AutoJoin = true;

	[Export]
	public NodePath UserNode;

	private void UpdateEditorSceneWithLaunchType()
	{
		if (!IsInsideTree())
			return;

		var area = FindNode("LaunchArea*", false);
		switch (LaunchType)
		{
			case LaunchType.MouseButtonDown:
			case LaunchType.TriggerVolume:
				if (area == null)
				{
					area = new Area() { Name = "LaunchArea" };
					AddChild(area);
					area.Owner = GetTree().EditedSceneRoot;

					var CollisionShape = new CollisionShape();
					area.AddChild(CollisionShape);
					CollisionShape.Owner = GetTree().EditedSceneRoot;
				}
				break;
			case LaunchType.OnStart:
				if (area != null)
				{
					area.QueueFree();
					area = null;
				}
				break;
		}
	}

	private void InitializeLaunchArea()
	{
		var area = FindNode("LaunchArea*", false);
		if (area != null)
		{
			if (launchType == LaunchType.MouseButtonDown)
			{
				area.Connect("input_event", this, nameof(OnInputEvent));
			}
			else if (launchType == LaunchType.TriggerVolume)
			{
				area.Connect("area_entered", this, nameof(OnAreaEntered));
				area.Connect("area_exited", this, nameof(OnAreaExited));
			}
		}
	}

	private void InitializeMREComponent()
	{
		MREComponent.Name = "MREComponent";
		MREComponent.MREURL = MREURL;
		MREComponent.SessionID = SessionID;
		MREComponent.AppID = AppID;
		MREComponent.EphemeralAppID = EphemeralAppID;
		MREComponent.AutoStart = AutoStart;
		MREComponent.AutoJoin = AutoJoin;
		MREComponent.GrantedPermissions = (MixedRealityExtension.Core.Permissions)(-1);
		MREComponent.UserProperties = new MREComponent.UserProperty[0];
		MREComponent.UserNode = GetNode(UserNode + "/MainCamera");
		MREComponent.DialogFactory = GetNode<DialogFactory>("Player/DialogFactory");
		AddChild(MREComponent);
	}

	private void StartApp()
	{
		if (Engine.EditorHint) return;
		GD.Print("Starting MRE app.");
		MREComponent?.EnableApp();
		_running = true;
	}

	private void StopApp()
	{
		MREComponent?.DisableApp();
		_running = false;
	}

	private void OnInputEvent(Godot.Object camera, InputEvent inputEvent, Vector3 clickPosition, Vector3 clickNormal, int shapeIdx)
	{
		if ((inputEvent is InputEventMouseButton e) && e.IsPressed())
		{
			if (LaunchType == LaunchType.MouseButtonDown && MREComponent != null)
			{
				var area = FindNode("LaunchArea*", false);
				if (area != null)
				{
					area.QueueFree();
					area = null;
				}
				StartApp();
			}
		}
	}
	private void OnAreaEntered(Node area)
	{
		if (LaunchType == LaunchType.TriggerVolume && area.Name == "PlayerArea")
		{
			StartApp();
		}
	}
	private void OnAreaExited(Node area)
	{
		if (StopAppOnExit)
		{
			if (LaunchType == LaunchType.TriggerVolume && area.Name == "PlayerArea")
			{
				StopApp();
			}
		}
	}

	public override void _Ready()
	{
		InitializeLaunchArea();
		InitializeMREComponent();
	}

	public override void _Process(float delta)
	{
		if (!_running && LaunchType == LaunchType.OnStart)
		{
			StartApp();
		}
	}
}
