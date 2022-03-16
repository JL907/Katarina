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
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(Assets.mainAssetBundle.LoadAsset<GameObject>("KatarinaWeapon"));
                weaponInstance = gameObject;
                weaponInstance.transform.Find("Trigger").gameObject.AddComponent<DaggerPickup>().baseObject = weaponInstance;
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
                weaponInstance.transform.position = this.target.transform.position;
                Rigidbody component2 = weaponInstance.GetComponent<Rigidbody>();
                if (component2)
                {
                    
                }
                NetworkServer.Spawn(weaponInstance);
            }
        }
    }
}
