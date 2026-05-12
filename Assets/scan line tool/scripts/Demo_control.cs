using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace cube_game.scan_line
{
    public class Demo_control : MonoBehaviour
    {
        [Header("Audio Source")]
        public AudioSource audio_source;

        [Header("scan_line prefab array")]
        public GameObject[] array_scan_line_prefab;

        [Header("title")]
        public Text text_title;

        private int index=0;

        private int fingerID = -1;

        void Awake()
        {
#if !UNITY_EDITOR
                this.fingerID = 0; 
#endif
        }


        // Update is called once per frame
        void Update()
        {
            // is the touch on the GUI
            if (EventSystem.current.IsPointerOverGameObject(this.fingerID))   
                // GUI Action
                return;

            if (Input.GetMouseButtonDown(0))
            {
                if (this.index == this.array_scan_line_prefab.Length-1)
                {
                    GameObject.Instantiate(this.array_scan_line_prefab[this.index]);

                    //play audio
                    this.audio_source.Play();
                }
                else
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit))
                    {

                        if (hit.collider.gameObject.name == "ground")
                        {
                            GameObject game_obj_scan_line = Instantiate(this.array_scan_line_prefab[this.index]);
                            game_obj_scan_line.GetComponent<Scan_line_control>().init_position= hit.point;
                            //game_obj_scan_line.transform.position = hit.point;

                            //play audio
                            this.audio_source.Play();
                        }
                    }
                }
            }


            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                this.index--;
                if (this.index < 0)
                    this.index = 3;

                this.text_title.text = this.array_scan_line_prefab[this.index].name;
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                this.index++;
                if (this.index >=this.array_scan_line_prefab.Length)
                    this.index = 0;

                this.text_title.text = this.array_scan_line_prefab[this.index].name;
            }
        }
    }
}
