using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using UnityEngine.Networking;
using KatarinaMod.Modules.LeagueVfx;

namespace KatarinaMod
{
    public class DaggerOrb : Orb
    {
        public float speed = 100f;
        public float damageValue;
        public GameObject attacker;
        public GameObject inflictor;
        public int bouncesRemaining;
        public List<HealthComponent> bouncedObjects;
        public TeamIndex teamIndex;
        public bool isCrit;
        public ProcChainMask procChainMask;
        public float procCoefficient = 1f;
        public DamageColorIndex damageColorIndex;
        public float range = 20f;
        public float damageCoefficientPerBounce = 1f;
        public DamageType damageType;
        public bool tossDaggerOnArrival;
        public float landingDistance = 15f;

        private const float MinFlightTime = 0.1f;
        private Vector3 lastTargetPosition;
        private BullseyeSearch search;

        public override void Begin()
        {
            base.duration = Mathf.Max(base.distanceToTarget / this.speed, MinFlightTime);
            this.lastTargetPosition = this.target ? this.target.transform.position : this.origin;
            DaggerOrbEffect.Spawn(this.origin, this.target, base.duration, this.attacker);
        }

        public override void OnArrival()
        {
            HealthComponent healthComponent = this.target ? this.target.healthComponent : null;
            Vector3 hitPosition = this.target ? this.target.transform.position : this.lastTargetPosition;

            if (healthComponent && healthComponent.alive)
            {
                DamageInfo damageInfo = new DamageInfo();
                damageInfo.damage = this.damageValue;
                damageInfo.attacker = this.attacker;
                damageInfo.inflictor = this.inflictor;
                damageInfo.force = Vector3.zero;
                damageInfo.crit = this.isCrit;
                damageInfo.procChainMask = this.procChainMask;
                damageInfo.procCoefficient = this.procCoefficient;
                damageInfo.position = hitPosition;
                damageInfo.damageColorIndex = this.damageColorIndex;
                damageInfo.damageType = this.damageType;
                healthComponent.TakeDamage(damageInfo);
                GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
                GlobalEventManager.instance.OnHitAll(damageInfo, healthComponent.gameObject);
            }

            if (this.bouncedObjects == null) this.bouncedObjects = new List<HealthComponent>();
            if (healthComponent && !this.bouncedObjects.Contains(healthComponent)) this.bouncedObjects.Add(healthComponent);

            HurtBox next = this.bouncesRemaining > 0 ? this.PickNextTarget(hitPosition) : null;
            if (!next)
            {
                if (this.tossDaggerOnArrival)
                {
                    this.tossDaggerOnArrival = false;
                    this.TossDagger(hitPosition);
                }
                return;
            }

            DaggerOrb bounce = new DaggerOrb();
            bounce.search = this.search;
            bounce.origin = hitPosition;
            bounce.target = next;
            bounce.attacker = this.attacker;
            bounce.inflictor = this.inflictor;
            bounce.teamIndex = this.teamIndex;
            bounce.damageValue = this.damageValue * this.damageCoefficientPerBounce;
            bounce.bouncesRemaining = this.bouncesRemaining - 1;
            bounce.isCrit = this.isCrit;
            bounce.bouncedObjects = this.bouncedObjects;
            bounce.procChainMask = this.procChainMask;
            bounce.procCoefficient = this.procCoefficient;
            bounce.damageColorIndex = this.damageColorIndex;
            bounce.damageCoefficientPerBounce = this.damageCoefficientPerBounce;
            bounce.speed = this.speed;
            bounce.range = this.range;
            bounce.damageType = this.damageType;
            bounce.landingDistance = this.landingDistance;
            bounce.tossDaggerOnArrival = this.tossDaggerOnArrival;
            OrbManager.instance.AddOrb(bounce);
        }

        private void TossDagger(Vector3 hitPosition)
        {
            if (!NetworkServer.active || !this.attacker) return;

            Vector3 start = hitPosition + Vector3.up * 2f;
            CharacterBody attackerBody = this.attacker.GetComponent<CharacterBody>();
            Vector3 katarinaPosition = attackerBody ? attackerBody.corePosition : this.attacker.transform.position;
            Vector3? landing = FindLandingPoint(hitPosition, katarinaPosition, this.landingDistance);
            if (landing.HasValue && Modules.Projectiles.knifePrefab)
            {
                float pickupRadius = DaggerPickup.PickupRadius * Mathf.Abs(Modules.Projectiles.knifePrefab.transform.localScale.x);
                Modules.DaggerLandingPreview.Spawn(landing.Value, pickupRadius, DaggerPickup.LeagueLandTime, this.attacker);
            }

            DaggerPickup.pendingLanding = landing;
            try
            {
                ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                {
                    projectilePrefab = Modules.Projectiles.knifePrefab,
                    position = start,
                    rotation = Quaternion.identity,
                    owner = this.attacker,
                    damage = 0,
                    force = 0,
                    crit = false,
                    speedOverride = 0f
                });
            }
            finally
            {
                DaggerPickup.pendingLanding = null;
            }
        }

        private static Vector3? FindLandingPoint(Vector3 hitPosition, Vector3 katarinaPosition, float distance)
        {
            int world = LayerIndex.world.mask;

            Vector3 direction = hitPosition - katarinaPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.forward;
            direction.Normalize();

            Vector3 start = hitPosition + Vector3.up;
            float travel = Mathf.Max(0f, distance);
            if (travel > 0f && Physics.SphereCast(start, 0.3f, direction, out RaycastHit wall, travel, world, QueryTriggerInteraction.Ignore))
            {
                travel = Mathf.Max(0f, wall.distance - 0.5f);
            }

            Vector3 above = start + direction * travel;
            if (Physics.Raycast(above, Vector3.down, out RaycastHit ground, 200f, world, QueryTriggerInteraction.Ignore))
            {
                return ground.point;
            }
            if (Physics.Raycast(start, Vector3.down, out ground, 200f, world, QueryTriggerInteraction.Ignore))
            {
                return ground.point;
            }
            return null;
        }

        public HurtBox PickNextTarget(Vector3 position)
        {
            if (this.search == null) this.search = new BullseyeSearch();
            this.search.searchOrigin = position;
            this.search.searchDirection = Vector3.zero;
            this.search.teamMaskFilter = TeamMask.allButNeutral;
            this.search.teamMaskFilter.RemoveTeam(this.teamIndex);
            this.search.filterByLoS = true;
            this.search.filterByDistinctEntity = true;
            this.search.sortMode = BullseyeSearch.SortMode.Distance;
            this.search.maxDistanceFilter = this.range;
            this.search.RefreshCandidates();

            HurtBox hurtBox = this.search.GetResults().FirstOrDefault(v =>
                v && v.healthComponent && v.healthComponent.alive && !this.bouncedObjects.Contains(v.healthComponent));
            if (hurtBox) this.bouncedObjects.Add(hurtBox.healthComponent);
            return hurtBox;
        }
    }
}
