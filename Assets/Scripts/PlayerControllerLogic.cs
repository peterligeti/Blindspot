using UnityEngine;

public class PlayerControllerLogic : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] float rotationThreshold = 1f; // The minimum distance to not rotate
    [SerializeField] float rotationSpeed = 20f; // Speed of rotation towards the mouse
    
    private Rigidbody2D rb;
    private Vector2 moveInput;
    
    [SerializeField] private LineRenderer aimLine;
    [SerializeField] private LayerMask wallLayer; 

    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null)
        {
            Debug.LogError("No SpriteRenderer attached to PlayerControllerLogic");
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        if (moveX != 0 || moveY != 0)
        {
            moveInput = new Vector2(moveX, moveY).normalized;  // normalize for consistent speed
        }
        else
        {
            moveInput = Vector2.zero;  // stop movement when no input is detected
        }
        
        DrawAimLine();
    }

    void FixedUpdate()
    {
        rb.velocity = moveInput * moveSpeed;
        RotatePlayerTowardsMouse();
    }

    void RotatePlayerTowardsMouse()
    {
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 directionToMouse = (mouseWorldPosition - transform.position).normalized;
        float distanceToMouse = Vector2.Distance(transform.position, mouseWorldPosition);
        
        if (distanceToMouse > rotationThreshold)
        {
            float angle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;
            
            Quaternion targetRotation = Quaternion.Euler(new Vector3(0, 0, angle));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    void DrawAimLine()
    {
        Vector3 start = transform.position;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mouseWorldPos - start).normalized;

        RaycastHit2D hit = Physics2D.Raycast(start, direction, Mathf.Infinity, wallLayer);

        Vector3 end = hit.collider != null ? (Vector3)hit.point : mouseWorldPos;
        end.z = 0f; // Ensure it's on the same plane

        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, end);
    }
}

public static class InputUtility
{
    private const string leftTriggerName = "LTrigger";
    private const string rightTriggerName = "RTrigger";
    private static bool used;

    public static bool LTriggerPulled
    {
        get
        {
            if (!used && Input.GetAxis(leftTriggerName) > 0)
            {
                used = true;
                return true;
            }
            else if (used && Input.GetAxis(leftTriggerName) == 0)
            {
                used = false;
            }
            return false;
        }
    }

    public static bool RTriggerPulled
    {
        get
        {
            if (!used && Input.GetAxis(rightTriggerName) > 0)
            {
                used = true;
                return true;
            }
            else if (used && Input.GetAxis(rightTriggerName) == 0)
            {
                used = false;
            }
            return false;
        }
    }
}
