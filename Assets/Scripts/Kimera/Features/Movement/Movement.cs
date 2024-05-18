using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Features
{
    public class Movement : MonoBehaviour, IActivable, IFeatureSetup, IFeatureFixedUpdate, ISubcontroller
    {
        //Configuration
        [Header("Settings")]
        public Settings settings;
        //Control
        [Header("Control")]
        [SerializeField] private bool active;
        //States
        [Header("States")]
        public Vector3 speed;
        //Properties
        [Header("Properties")]
        public float maxSpeed;
        public float acceleration;
        //References
        [Header("References")]
        [SerializeField] private List<TerrainModifier> terrains;
        [SerializeField] private Rotation rotation;
        [SerializeField] private Jump jump;
        [SerializeField] private Dash dash;
        //Componentes
        [Header("Components")]
        [SerializeField] private Rigidbody cmp_rigidbody;

        private void Awake()
        {
            //Setup References
            terrains = new List<TerrainModifier>(GetComponents<TerrainModifier>());
            terrains.Sort(TerrainModifier.CompareByOrder);

            rotation = GetComponent<Rotation>();
            jump = GetComponent<Jump>();
            dash = GetComponent<Dash>();

            //Setup Components
            cmp_rigidbody = GetComponent<Rigidbody>();
        }

        public void SetupFeature(Controller controller)
        {
            settings = controller.settings;

            //Setup Properties
            maxSpeed = settings.Search("maxSpeed");
            acceleration = settings.Search("acceleration");

            ToggleActive(true);
        }

        public void FixedUpdateFeature(Controller controller)
        {
            if(!active) return;

            KineticEntity kinetic = controller as KineticEntity;
            if(kinetic != null) kinetic.currentSpeed = speed.magnitude;

            InputEntity input = controller as InputEntity;
            if(input == null) return;
            Vector2 direction = input.inputDirection;

            Move(direction, kinetic, input);
        }

        public void Move(Vector2 direction, KineticEntity kinetic, InputEntity input)
        {
            if(!active) return;
            if(cmp_rigidbody == null) return;

            Vector3 movement = ProjectOnCameraFlattenPlane(new Vector3(direction.x, 0f, direction.y), input.playerCamera);
            
            if(input != null) input.playerForward = movement.normalized;

            terrains.Sort(TerrainModifier.CompareByOrder);
            if (terrains.Count > 0)
            {
                foreach (TerrainModifier terrain in terrains)
                {
                    if (!terrain.OnTerrain) continue;

                    movement = terrain.ProjectOnTerrain(movement);
                }
            }

            if (movement != Vector3.zero) cmp_rigidbody.AddForce(movement.normalized * acceleration * 10f);

            LimitSpeed();

            speed = cmp_rigidbody.velocity;
            kinetic.currentSpeed = speed.magnitude;
        }

        private void LimitSpeed()
        {
            Vector3 velocity = cmp_rigidbody.velocity;

            if (velocity == Vector3.zero) return;

            terrains.Sort(TerrainModifier.CompareByOrder);
            if (terrains.Count > 0) if (terrains[terrains.Count - 1].OnTerrain) velocity = terrains[terrains.Count - 1].ProjectOnTerrain(velocity);

            Vector3 diffVelocity = cmp_rigidbody.velocity - velocity;

            if (velocity.magnitude > maxSpeed)
            {
                cmp_rigidbody.velocity = velocity.normalized * maxSpeed + diffVelocity;
            }
        }

        private Vector3 ProjectOnCameraFlattenPlane(Vector3 direction, Camera camera)
        {
            if(camera == null) return Vector3.zero;

            Vector3 cameraPosition = camera.transform.position;
            Vector3 cameraForward = (transform.position - cameraPosition).normalized;
            Vector3 cameraRight = Vector3.Cross(Vector3.up, cameraForward);

            // Calculate the movement direction based on the camera's forward direction
            Vector3 projection = cameraForward * direction.x + cameraRight * direction.z;

            projection.y = 0;
            projection.Normalize();

            return projection;
        }

        public bool GetActive()
        {
            return active;
        }

        public void ToggleActive(bool active)
        {
            this.active = active;

            if (active) return;
            if(cmp_rigidbody.velocity == Vector3.zero) return;

            cmp_rigidbody.velocity = Vector3.zero;
            speed = Vector3.zero;
        }

        public void ToggleActiveSubcontroller(bool active)
        {
            if (rotation != null) rotation.ToggleActive(active);
            if (jump != null) jump.ToggleActive(active);
            if (dash != null) dash.ToggleActive(active);

            ToggleActive(active && this.active);
        }
    }
}

