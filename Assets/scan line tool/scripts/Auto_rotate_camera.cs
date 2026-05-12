using UnityEngine;

namespace cube_game.scan_line
{
    public class Auto_rotate_camera : MonoBehaviour
    {
        public Transform target;
        public float distance = 10.0f;
        public float orbitSpeed = 20.0f;
        public Vector3 offset = Vector3.zero;

        void Update()
        {
            if (target != null)
            {
                transform.RotateAround(target.position, Vector3.up, orbitSpeed * Time.deltaTime);

                Vector3 desiredPosition = (transform.position - target.position).normalized * distance + target.position + offset;
                transform.position = desiredPosition;

                transform.LookAt(target.position + offset);
            }
        }
    }
}