using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace NetWork
{
    public class NetworkedPlayer : MonoBehaviour
    {
        public NetworkMan netMan;

        void Start()
        {
            InvokeRepeating("NetUpdatePosition", 1, 0.03f);
        }
        void NetUpdatePosition()
        {
            netMan.SendPosition(transform.position, transform.rotation.eulerAngles);
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.D))
            {
                transform.Translate(new Vector3(Time.deltaTime * 3, 0, 0));
            }
        }
    }
}
