using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace cube_game.scan_line
{
    public class Scan_line_control : MonoBehaviour
    {
        [Header("speed")]
        public float speed;

        [Header("destory time")]
        public float delay_destory_time;

        [Header("Initialize scale")]
        public Vector3 init_scale=new Vector3(1,1,1);

        [Header("Initialize position")]
        public Vector3 init_position=new Vector3(0,0,0);

        // Start is called before the first frame update
        void Start()
        {
            this.transform.localScale = this.init_scale;
            this.transform.position = this.init_position;


            Invoke("destory_self", this.delay_destory_time);
        }

        // Update is called once per frame
        void Update()
        {
            Vector3 v3 = this.transform.localScale;
            float temp = this.speed * Time.deltaTime;
            this.transform.localScale = new Vector3(v3.x + temp, v3.y + temp, v3.z + temp);
        }

        private void destory_self()
        {
            Destroy(this.gameObject);
        }
    }
}