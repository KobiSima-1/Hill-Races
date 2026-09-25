using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Applies the two gameplay inputs to the buggy:
/// on the ground they drive the wheel motors, in the air they rotate the body.
/// Nothing here sets position or velocity directly. all motion comes from motors, torque and gravity.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class VehicleController : MonoBehaviour
{
    // WheelJoint2D motors spin clockwise (= forward, to the right) for a NEGATIVE motorSpeed.
    // If the buggy drives backwards on throttle in your setup, flip this to +1.
    private const float ForwardSign = -1f;

    [SerializeField] private VehicleConfig _config;
    [SerializeField] private WheelJoint2D _rearWheelJoint;
    [SerializeField] private WheelJoint2D _frontWheelJoint;
    [SerializeField] private LayerMask _groundLayer;

    private Rigidbody2D _body;
    private Collider2D _rearWheelCollider;
    private Collider2D _frontWheelCollider;

    // Input is read in Update and cached here, then consumed in FixedUpdate.
    private bool _throttleHeld;
    private bool _brakeHeld;

    private float _currentMotorSpeed;
    private bool _motorsEngaged;

    public bool IsGrounded { get; private set; }
    public bool InputEnabled { get; set; } = true;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _rearWheelCollider = _rearWheelJoint.connectedBody.GetComponent<Collider2D>();
        _frontWheelCollider = _frontWheelJoint.connectedBody.GetComponent<Collider2D>();
        ApplyConfig();
    }

    private void ApplyConfig()
    {
        _body.mass = _config.BodyMass;
        _body.centerOfMass = _config.CenterOfMassOffset;
        ConfigureWheel(_rearWheelJoint);
        ConfigureWheel(_frontWheelJoint);
    }

    private void ConfigureWheel(WheelJoint2D joint)
    {
        joint.connectedBody.mass = _config.WheelMass;

        // JointSuspension2D is a struct: copy, modify, assign back.
        JointSuspension2D suspension = joint.suspension;
        suspension.frequency = _config.SuspensionFrequency;
        suspension.dampingRatio = _config.SuspensionDampingRatio;
        joint.suspension = suspension;
    }

    private void Update()
    {
        ReadInput();
    }

    private void FixedUpdate()
    {
        IsGrounded = _rearWheelCollider.IsTouchingLayers(_groundLayer)
                  || _frontWheelCollider.IsTouchingLayers(_groundLayer);

        if (IsGrounded)
        {
            Drive();
        }
        else
        {
            RotateInAir();
        }
    }

    private void ReadInput()
    {
        if (!InputEnabled)
        {
            _throttleHeld = false;
            _brakeHeld = false;
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;

        _throttleHeld = (keyboard != null && (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed))
                     || (gamepad != null && (gamepad.rightTrigger.isPressed || gamepad.buttonSouth.isPressed));

        _brakeHeld = (keyboard != null && (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed))
                  || (gamepad != null && (gamepad.leftTrigger.isPressed || gamepad.buttonWest.isPressed));
    }

    private void Drive()
    {
        bool anyInput = _throttleHeld || _brakeHeld;

        if (!anyInput)
        {
            // Released: motors off so the buggy rolls freely instead of braking.
            SetMotors(false);
            return;
        }

        if (!_motorsEngaged)
        {
            // Start the ramp from the wheels' current spin, so re-engaging while rolling doesn't jolt.
            _currentMotorSpeed = _rearWheelJoint.jointSpeed * ForwardSign;
        }

        float step = _config.MotorRampRate * Time.fixedDeltaTime;
        _currentMotorSpeed = Mathf.MoveTowards(_currentMotorSpeed, GetTargetMotorSpeed(), step);
        SetMotors(true);
    }

    private float GetTargetMotorSpeed()
    {
        // Brake wins when both are held.
        if (_brakeHeld)
        {
            return -_config.MaxMotorSpeed * _config.ReverseFraction;
        }

        return _config.MaxMotorSpeed;
    }

    private void RotateInAir()
    {
        // Motors off in the air so the buggy doesn't land with wheels at full spin.
        SetMotors(false);

        float direction = 0f;
        if (_brakeHeld)
        {
            direction = -1f; // nose down (clockwise)
        }
        else if (_throttleHeld)
        {
            direction = 1f;  // nose up (counter-clockwise)
        }

        _body.AddTorque(direction * _config.AirTorque);
    }

    private void SetMotors(bool engaged)
    {
        _motorsEngaged = engaged;
        ApplyMotor(_rearWheelJoint, engaged);
        ApplyMotor(_frontWheelJoint, engaged);
    }

    private void ApplyMotor(WheelJoint2D joint, bool engaged)
    {
        joint.useMotor = engaged;
        if (!engaged)
        {
            return;
        }

        // JointMotor2D is a struct: copy, modify, assign back.
        JointMotor2D motor = joint.motor;
        motor.motorSpeed = _currentMotorSpeed * ForwardSign;
        motor.maxMotorTorque = _config.MotorTorque;
        joint.motor = motor;
    }
}
