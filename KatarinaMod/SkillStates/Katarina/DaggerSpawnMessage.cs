using R2API.Networking.Interfaces;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace KatarinaMod
{
    class DaggerSpawnMessage : INetMessage
    {
        private CharacterBody attackerBody;
        private Vector3 location;

        public DaggerSpawnMessage() { }

        public DaggerSpawnMessage(CharacterBody attackerBody, Vector3 location)
        {
            this.attackerBody = attackerBody;
            this.location = location;
        }
        public void Deserialize(NetworkReader reader)
        {
            var netObj = Util.FindNetworkObject(reader.ReadNetworkId());
            attackerBody = netObj.GetComponent<CharacterBody>();
            location = reader.ReadVector3();
        }

        public void OnReceived()
        {
            attackerBody.SpawnDagger(location);
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(attackerBody.networkIdentity.netId);
            writer.Write(location);
        }
    }
}
