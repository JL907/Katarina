using KatarinaMod.SkillStates.Katarina;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace KatarinaMod.Modules
{
    internal class DaggerPickupMessage : INetMessage
    {
        private NetworkInstanceId bodyNetId;

        public DaggerPickupMessage() { }

        public DaggerPickupMessage(NetworkInstanceId bodyNetId)
        {
            this.bodyNetId = bodyNetId;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(this.bodyNetId);
        }

        public void Deserialize(NetworkReader reader)
        {
            this.bodyNetId = reader.ReadNetworkId();
        }

        public void OnReceived()
        {
            EnterVoracity(Util.FindNetworkObject(this.bodyNetId));
        }

        internal static void NotifyPickup(CharacterBody body)
        {
            if (!NetworkServer.active || !body) return;

            NetworkIdentity identity = body.GetComponent<NetworkIdentity>();
            if (!identity) return;

            if (Util.HasEffectiveAuthority(identity))
            {
                EnterVoracity(body.gameObject);
            }
            else if (identity.clientAuthorityOwner != null)
            {
                new DaggerPickupMessage(identity.netId).Send(identity.clientAuthorityOwner);
            }
        }

        private static void EnterVoracity(GameObject bodyObject)
        {
            if (!bodyObject) return;
            if (!Util.HasEffectiveAuthority(bodyObject.GetComponent<NetworkIdentity>())) return;

            EntityStateMachine bodyMachine = EntityStateMachine.FindByCustomName(bodyObject, "Body");
            if (!bodyMachine) bodyMachine = bodyObject.GetComponent<EntityStateMachine>();
            if (bodyMachine) bodyMachine.SetNextState(new Voracity());
        }
    }
}
