using KatarinaMod.SkillStates.Katarina;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace KatarinaMod.Modules
{
    // Sent by the server to the player who controls a Katarina when she picks up one of her daggers.
    // Skill states run on the controlling player's machine, so that is where Voracity has to start.
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

        // Server only: start Voracity on whoever controls this body.
        internal static void NotifyPickup(CharacterBody body)
        {
            if (!NetworkServer.active || !body) return;

            NetworkIdentity identity = body.GetComponent<NetworkIdentity>();
            if (!identity) return;

            if (Util.HasEffectiveAuthority(identity))
            {
                // host's own Katarina (or a server-controlled one)
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
