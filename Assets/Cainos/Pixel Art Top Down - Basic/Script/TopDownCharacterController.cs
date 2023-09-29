using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cainos.PixelArtTopDown_Basic
{
    public class TopDownCharacterController : MonoBehaviour
    {
        public float speed;

        private Animator animator;

        private void Start()
        {
            animator = GetComponent<Animator>();
        }


        private void Update()
        {
            Vector2 dir = Vector2.zero;
            if (Input.GetKey(KeyCode.A))
            {
                dir.x = -1;
                animator.SetInteger("Direction", 3);
            }
            else if (Input.GetKey(KeyCode.D))
            {
                dir.x = 1;
                animator.SetInteger("Direction", 2);
            }

            if (Input.GetKey(KeyCode.W))
            {
                dir.y = 1;
                animator.SetInteger("Direction", 1);
            }
            else if (Input.GetKey(KeyCode.S))
            {
                dir.y = -1;
                animator.SetInteger("Direction", 0);
            }

            dir.Normalize();
            animator.SetBool("IsMoving", dir.magnitude > 0);

            GetComponent<Rigidbody2D>().velocity = speed * dir;

            
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                transform.position = new Vector3Int((int)(transform.position.x - 1f), (int)transform.position.y);
                animator.SetInteger("Direction", 3);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                transform.position = new Vector3Int((int)(transform.position.x + 1f), (int)transform.position.y);
                animator.SetInteger("Direction", 2);
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                transform.position = new Vector3Int((int)transform.position.x, (int)(transform.position.y + 1f));
                animator.SetInteger("Direction", 1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                transform.position = new Vector3Int((int)transform.position.x, (int)(transform.position.y - 1f));
                animator.SetInteger("Direction", 0);
            }
        }
    }
}
