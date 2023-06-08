using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementController : MonoBehaviour
{
    private float playerSpeed = 4.0f;
    private float deadzone = 0.1f;
    private float gravityValue = -9.81f;

    private CharacterController characterController;
    private Animator animator;

    private Vector2 movementInput = Vector2.zero;
    private Vector3 playerVelocity;

    void Start()
    {
        characterController = gameObject.GetComponent<CharacterController>();
        animator = gameObject.GetComponentInChildren<Animator>();
    }

    public void OnMove(InputAction.CallbackContext context) {
        movementInput = context.ReadValue<Vector2>();
    }

    public void OnMove(Vector2 input) {
        movementInput = input;
    }

    void Update()
    {
        playerVelocity.y += gravityValue * Time.deltaTime;
        if (characterController.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        Vector3 move = new Vector3(movementInput.x, 0, movementInput.y);

        if (move.magnitude > deadzone)
        {
            characterController.Move(move * Time.deltaTime * playerSpeed);
            gameObject.transform.forward = move;
            if (animator != null) {
                animator.Play("Walk");
            }
        }
        else
        {
            if (animator != null) {
                animator.Play("Idle_A");
            }
        }

        characterController.Move(playerVelocity * Time.deltaTime);
    }
}
