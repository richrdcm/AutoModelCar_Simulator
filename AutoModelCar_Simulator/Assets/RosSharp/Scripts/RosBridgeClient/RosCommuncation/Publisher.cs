﻿/*
© Siemens AG, 2017-2018
Author: Dr. Martin Bischoff (martin.bischoff@siemens.com)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
<http://www.apache.org/licenses/LICENSE-2.0>.
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    //[RequireComponent(typeof(RosConnector))]
    public abstract class Publisher<T> : MonoBehaviour where T: Message
    {
        public RosConnector Connection;
        public string Topic;
        private string publicationId;

        protected virtual void Start()
        {
            if(!Connection) Connection = Globals.Instance.Connection;
            //publicationId = GetComponent<RosConnector>().RosSocket.Advertise<T>(Topic);
            publicationId = Connection.RosSocket.Advertise<T>(Topic);
        }

        protected void Publish(T message)
        {
            //GetComponent<RosConnector>().RosSocket.Publish(publicationId, message);
            Connection.RosSocket.Publish(publicationId, message);
        }

        public void changeTopicName(string newname) {
            if(newname==Topic) return;
            Connection.RosSocket.Unadvertise(publicationId);
            Topic = newname;
            publicationId = Connection.RosSocket.Advertise<T>(Topic);
        }
    }
}