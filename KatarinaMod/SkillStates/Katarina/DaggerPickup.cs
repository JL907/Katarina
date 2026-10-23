using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace KatarinaMod
{
    public class DaggerPickup : MonoBehaviour
    {
        private float stopwatch;
        public Rigidbody rigidbody;
        public GameObject pickupEffect;
        public bool collided = false;
        private bool alive = true;
        private SphereCollider sphereCollider;
        private ProjectileController projectileController;
        private Vector3 eulerAngleVelocity;
        private bool stuck;
        private Vector3 transformPositionWhenContacted;
        public AnimationCurve embedDistanceCurve;

        internal static Vector3? pendingLanding;
        internal const float LeagueLandTime = 1f;
        internal const float PickupRadius = 3f;
        private const float LandingArcHeight = 8f;
        private bool scriptedFlight;
        private Vector3 flightStart;
        private Vector3 flightEnd;
        private float apexHeight;
        private float apexTime;
        private Vector3 landingGround;
        private const float MidAirPickupDelay = 0.25f;

        private void Awake()
        {
            this.rigidbody = this.GetComponent<Rigidbody>();
            this.sphereCollider = this.GetComponent<SphereCollider>();
            this.sphereCollider.radius = PickupRadius;
            this.sphereCollider.isTrigger = true;
            this.sphereCollider.center = Vector3.zero;
            this.projectileController = base.GetComponent<ProjectileController>();
            if (this.sphereCollider) this.sphereCollider.enabled = false;
            this.stopwatch = 0f;
            this.rigidbody.velocity = Vector3.zero;
            eulerAngleVelocity = new Vector3(15f, 0, 0);

            if (NetworkServer.active && pendingLanding.HasValue)
            {
                this.scriptedFlight = true;
                this.flightStart = base.transform.position;
                this.landingGround = pendingLanding.Value;
                this.flightEnd = pendingLanding.Value + Vector3.up * this.RestHeight();
                this.SetupArc();
                pendingLanding = null;

                this.rigidbody.isKinematic = true;
                this.rigidbody.useGravity = false;
            }
        }

        private float RestHeight()
        {
            CapsuleCollider capsule = base.GetComponent<CapsuleCollider>();
            if (!capsule) return 0f;
            return capsule.height * 0.5f * Mathf.Abs(base.transform.lossyScale.y);
        }

        private void SetupArc()
        {
            this.apexHeight = Mathf.Max(this.flightStart.y, this.flightEnd.y) + LandingArcHeight;
            float rise = Mathf.Sqrt(this.apexHeight - this.flightStart.y);
            float fall = Mathf.Sqrt(this.apexHeight - this.flightEnd.y);
            this.apexTime = rise / (rise + fall);
        }

        private float ArcY(float t)
        {
            float d = t - this.apexTime;
            float a = (this.apexHeight - this.flightStart.y) / (this.apexTime * this.apexTime);
            return this.apexHeight - a * d * d;
        }

        private void ScriptedFlightStep()
        {
            float t = Mathf.Clamp01(this.stopwatch / LeagueLandTime);
            Vector3 position = Vector3.Lerp(this.flightStart, this.flightEnd, t);
            position.y = this.ArcY(t);

            if (this.stopwatch >= MidAirPickupDelay) this.sphereCollider.enabled = true;

            this.rigidbody.MovePosition(position);
            this.rigidbody.MoveRotation(Quaternion.identity);

            if (t >= 1f)
            {
                this.scriptedFlight = false;
                this.stuck = true;
                this.transformPositionWhenContacted = this.flightEnd;
                base.transform.SetPositionAndRotation(this.flightEnd, Quaternion.identity);
                this.rigidbody.rotation = Quaternion.identity;
                this.sphereCollider.enabled = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!NetworkServer.active || !this.alive) return;

            CharacterBody characterBody = other.GetComponent<CharacterBody>();
            if (!characterBody) return;

            GameObject owner = this.projectileController ? this.projectileController.owner : null;
            if (!owner || characterBody.gameObject != owner) return;

            this.alive = false;
            if (this.scriptedFlight) Modules.DaggerLandingPreview.CancelForOwner(owner, this.landingGround);
            Modules.DaggerPickupMessage.NotifyPickup(characterBody);
            NetworkServer.Destroy(base.gameObject);
        }

        private void Update()
        {
            if (this.stuck)
            {
                base.transform.position = this.transformPositionWhenContacted;
            }
        }

        private void FixedUpdate()
        {
            this.stopwatch += Time.fixedDeltaTime;
            if (this.scriptedFlight)
            {
                if (this.alive) this.ScriptedFlightStep();
                return;
            }
            if (this.stopwatch < 0.5f && alive)
            {
                this.sphereCollider.enabled = false;
                this.rigidbody.velocity = Vector3.up * 15f;
                Quaternion deltaRotation = Quaternion.Euler(eulerAngleVelocity * 1.5f);
                this.rigidbody.MoveRotation(this.rigidbody.rotation * deltaRotation);
            }
            if (this.stopwatch > 0.5f && alive)
            {
                this.sphereCollider.enabled = true;
                this.rigidbody.AddForce(Vector3.down * 5f, ForceMode.VelocityChange);
                this.rigidbody.rotation = Quaternion.Lerp(this.rigidbody.rotation, Quaternion.identity, this.stopwatch / 1f);
            }
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (this.stuck || this.rigidbody.isKinematic)
            {
                return;
            }
            if (collision.transform.gameObject.layer != LayerIndex.world.intVal)
            {
                return;
            }
            if (stopwatch > 0.5f)
            {
                this.stuck = true;
                this.transformPositionWhenContacted = base.transform.position;
                this.rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                this.rigidbody.detectCollisions = true;
                this.rigidbody.isKinematic = true;
                this.rigidbody.velocity = Vector3.zero;
            }
        }
    }
}
