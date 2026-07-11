using System;
using System.Collections;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Zodiac.Util;
using Random = UnityEngine.Random;

namespace Zodiac.Core
{
    public class GunController : MonoBehaviour
    {
        [Serializable]
        public enum BurstMode
        {
            None,
            Burst,
            Shotgun,
        }

        [Serializable]
        public enum FireMode
        {
            SemiAuto,
            FullAuto,
        }

        [Header("Stats")]
        [SerializeField] private float damage = 10f;
        [SerializeField] private float range = 10f;
        [SerializeField] private float shotDelay = 0.1f;

        [SerializeField] private Vector2 spread = new(5f, 5f);
        [SerializeField] private Vector2 adsSpread = new(2f, 2f);

        [SerializeField] private FireMode fireMode = FireMode.SemiAuto;
        [SerializeField] private BurstMode burstMode = BurstMode.None;
        [SerializeField] private float burstDelay = 0.01f;
        [SerializeField] private int burstCount = 1;

        [SerializeField] private float recoilAngle = 5f;
        [SerializeField] private float recoilResetTime = 0.2f;
        [SerializeField] private float adsRecoilAngle = 2f;
        [SerializeField] private float adsRecoilResetTime = 0.2f;

        [SerializeField] private float adsTransitionTime = 0.2f;
        [SerializeField] private float adsZoom = 1.2f;
        [HideInInspector] public float hipfireZoom;

        [Header("Debugging")]
        [SerializeField] private Color debugHitColor;
        [SerializeField] private Color debugMissColor;
        [SerializeField] private float debugDuration;

        [Header("References")]
        [SerializeField] private Player player;
        [SerializeField] private Transform head;
        [SerializeField] private Transform recoil;
        [SerializeField] private Camera cam;

        private float timeLastShot;

        //Interpolations
        private float recoilTransition;
        private float recoilVelocity;
        private float adsTransition;
        private float adsVelocity;

        private Vector2 Spread => Vector2.Lerp(spread, adsSpread, adsTransition);
        private float RecoilAngle => Mathf.LerpAngle(recoilAngle, adsRecoilAngle, adsTransition);
        private float RecoilResetTime => Mathf.Lerp(recoilResetTime, adsRecoilResetTime, adsTransition);
        private float Zoom => Mathf.Lerp(1, adsZoom, adsTransition);

        private void Awake()
        {
            timeLastShot = -shotDelay;
        }

        private void Start()
        {
            hipfireZoom = cam.fieldOfView;
        }

        private void Update()
        {
            recoilTransition = Mathf.SmoothDamp(recoilTransition, 0, ref recoilVelocity, RecoilResetTime);
            recoil.localRotation = Quaternion.Euler(-recoilTransition, 0, 0);

            adsTransition = Mathf.SmoothDamp(adsTransition,
                player.Input.Aim.IsPressed() ? 1 : 0, ref adsVelocity, adsTransitionTime);

            cam.fieldOfView = hipfireZoom / Zoom;

            ShootCheck();
        }

        private void ShootCheck()
        {
            if (Time.time - timeLastShot < shotDelay) return;

            if (player.Input.Shoot.WasPressedThisDynamicUpdate() || player.Input.Shoot.IsPressed() && fireMode == FireMode.FullAuto)
            {
                Shoot();
            }
        }

        private void ApplyRecoil()
        {
            Vector3 dir = recoil.forward;
            head.rotation = Quaternion.LookRotation(dir, Vector3.up);
            recoilTransition = RecoilAngle;
            recoilVelocity = 0;
            recoil.localRotation = Quaternion.Euler(-recoilTransition, 0, 0);
        }

        private void Shoot()
        {
            switch (burstMode)
            {
                case BurstMode.None:
                    timeLastShot = Time.time;
                    FireBullet(RandomConicalDirection(Spread) * cam.transform.forward);
                    ApplyRecoil();
                    break;
                case BurstMode.Shotgun:
                    timeLastShot = Time.time;
                    for (int i = 0; i < burstCount; i++)
                    {
                        FireBullet(RandomConicalDirection(Spread) * cam.transform.forward);
                    }
                    ApplyRecoil();
                    break;
                case BurstMode.Burst:
                    StartCoroutine(BurstFireRoutine());
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void FireBullet(Vector3 direction)
        {
            Ray ray = new(cam.transform.position, direction);
            if (Physics.Raycast(ray, out var info, range))
            {
                Debug.DrawLine(ray.origin, info.point, debugHitColor, debugDuration);
                if (info.collider.TryGetComponent<HealthController>(out var health)) health.Damage(damage);
            }
            else
            {
                Debug.DrawRay(ray.origin, ray.direction * range, debugMissColor, debugDuration);
            }
        }

        private IEnumerator BurstFireRoutine()
        {
            timeLastShot = Time.time + (burstCount * burstDelay);
            FireBullet(RandomConicalDirection(spread) * recoil.forward);
            ApplyRecoil();
            for (int i = 1; i < burstCount; i++)
            {
                FireBullet(RandomConicalDirection(spread) * recoil.forward);
                ApplyRecoil();
                yield return new WaitForSeconds(burstDelay);
            }

            timeLastShot = Time.time;
        }

        public static Quaternion RandomConicalDirection(Vector2 apexes)
        {
            Vector2 point = Vector2.Scale(Random.insideUnitCircle, apexes);
            return Quaternion.Euler(point.y, point.x, 0f);
        }
    }
}