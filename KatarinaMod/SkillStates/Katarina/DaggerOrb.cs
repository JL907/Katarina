using KatarinaMod.Modules;
using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace KatarinaMod.Orb
{
    public class DaggerOrb : LightningOrb
    {
        private GameObject weaponInstance;
        public override void Begin()
        {
            base.Begin();
            if (!weaponInstance)
            {
                weaponInstance = UnityEngine.Object.Instantiate<GameObject>(Assets.mainAssetBundle.LoadAsset<GameObject>("KatarinaWeapon"));
                weaponInstance.AddComponent<DaggerPickup>().baseObject = weaponInstance;
                weaponInstance.AddComponent<DestroyOnTimer>().duration = 12;
                weaponInstance.AddComponent<NetworkIdentity>();
            }
        }

        public override void OnArrival()
        { 
            base.OnArrival();
            TossDagger();

        }
        private void TossDagger()
        {
            if(NetworkServer.active)
            {
                Vector3 position = this.target.transform.position + Vector3.up * 1.5f;
                Vector3 toTarget = (this.target.transform.position - this.attacker.gameObject.transform.position).normalized;
                Vector3 upVector = Vector3.up * 20 + toTarget * 3f;
                weaponInstance.transform.position = position;
                Rigidbody component2 = weaponInstance.GetComponent<Rigidbody>();
                component2.velocity = upVector;
                component2.AddTorque(UnityEngine.Random.Range(150f, 120f) * UnityEngine.Random.onUnitSphere);
                NetworkServer.Spawn(weaponInstance);
            }
        }
    }
}
