using UnityEngine;
namespace cube_game.scan_line
{
    public class Looping_movement : MonoBehaviour
    {
        public Vector3 startPoint = new Vector3(0, 0, 0); 
        public Vector3 endPoint = new Vector3(0, 10, 0); 
        public float speed = 1.0f; 

        void Update()
        {
            float t = Mathf.PingPong(Time.time * speed, 1.0f);

            transform.position = Vector3.Lerp(startPoint, endPoint, t);
        }
    }
}