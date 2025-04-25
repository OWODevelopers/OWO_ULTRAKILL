using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

namespace OWO_ULTRAKILL
{
    [BepInPlugin("org.bepinex.plugins.OWO_ULTRAKILL", "OWO_ULTRAKILL", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
#pragma warning disable CS0109
        internal static new ManualLogSource Log;
#pragma warning restore CS0109

        public static OWOSkin owoSkin;

        public static bool startDodging;


        private void Awake()
        {
            Log = Logger;
            Logger.LogMessage("OWO_ULTRAKILL plugin is loaded!");

            owoSkin = new OWOSkin();


            var harmony = new Harmony("owo.patch.ultrakill");
            harmony.PatchAll();
        }

        #region Movement

        [HarmonyPatch(typeof(GroundCheck), "OnTriggerEnter")]
        public class OnGroundCheck
        {
            [HarmonyPostfix]
            public static void Postfix(GroundCheck __instance)
            {
                NewMovement nmov = Traverse.Create(__instance).Field("nmov").GetValue<NewMovement>();
                float fallSpeed = Traverse.Create(nmov).Field("fallSpeed").GetValue<float>();
                if (!__instance.touchingGround)  return;
                if (fallSpeed == 0) return;
                if (fallSpeed <= -92)
                {
                    owoSkin.Feel($"Stomp");
                }
                else
                {
                    owoSkin.Feel("Landing");
                }
            }
        }

        [HarmonyPatch(typeof(NewMovement), "StartSlide")]
        public class OnStartSlide
        {
            [HarmonyPostfix]
            public static void Postfix(NewMovement __instance)
            {
                 owoSkin.LOG($"NewMovement StartSlide");
            }
        }

        [HarmonyPatch(typeof(NewMovement), "StopSlide")]
        public class OnStopSlide
        {
            [HarmonyPostfix]
            public static void Postfix(NewMovement __instance)
            {
                 owoSkin.LOG($"NewMovement StopSlide");
            }
        }

        [HarmonyPatch(typeof(NewMovement), "Jump")]
        public class OnJump
        {
            [HarmonyPostfix]
            public static void Postfix(NewMovement __instance)
            {
                owoSkin.StartUltraSpeed();
                if (__instance.modNoJump || (bool)__instance.groundProperties)
                {
                    if (__instance.modNoJump || !__instance.groundProperties.canJump) return;
                }
                owoSkin.Feel("Jump");
            }
        }

        [HarmonyPatch(typeof(NewMovement), "WallJump")]
        public class OnWallJump
        {
            [HarmonyPostfix]
            public static void Postfix(NewMovement __instance)
            {
                owoSkin.Feel("Jump");
            }
        }
        
        [HarmonyPatch(typeof(NewMovement), "Update")]
        public class OnDodge
        {
            [HarmonyPostfix]
            public static void Postfix(NewMovement __instance)
            {
                if (__instance.modNoDashSlide) return;

                float boostLeft = Traverse.Create(__instance).Field("boostLeft").GetValue<float>();

                //owoSkin.LOG($"VELOCITY - VELOCITY NORMALIZED: {__instance.rb.velocity.normalized}--SPEED:{__instance.rb.velocity.magnitude} - PLAYER LOOK: {__instance.transform.forward} - ANGLE: {Vector3.SignedAngle(__instance.rb.velocity.normalized, __instance.transform.forward, Vector3.up)}");

                Vector3 normalizedSpeed = __instance.rb.velocity.normalized;
                Vector3 playerForward = __instance.transform.forward;
                int speed = Mathf.FloorToInt(__instance.rb.velocity.magnitude);
                float angle = Vector3.SignedAngle(__instance.rb.velocity.normalized, __instance.transform.forward, Vector3.up) + 180;

                owoSkin.isPlayerActive = __instance.activated;


                if (__instance.activated)
                {
                    owoSkin.UpdateUltraSpeed(angle, speed);
                }

                if (MonoSingleton<InputManager>.Instance.InputSource.Dodge.WasPerformedThisFrame && __instance.activated && !__instance.slowMode && !GameStateManager.Instance.PlayerInputLocked && boostLeft >= 100f)
                {
                    startDodging = true;                    
                }
            }
        } 
        
        [HarmonyPatch(typeof(NewMovement), "Dodge")]
        public class OnDodgea
        {
            [HarmonyPostfix]
            public static void Postfix(NewMovement __instance)
            {
                if (__instance.modNoDashSlide) return;

                if (!startDodging) return;

                startDodging = false;

                Vector3 movementDirection2 = Traverse.Create(__instance).Field("movementDirection2").GetValue<Vector3>();

                if (__instance.dodgeDirection == __instance.transform.forward)
                {
                    owoSkin.LOG($"NewMovement Dodge Forward - Movement Direction: {movementDirection2} - Dodge Direction: {__instance.dodgeDirection} ");
                }
                else if (__instance.dodgeDirection == __instance.transform.forward * -1f)
                {
                    owoSkin.LOG($"NewMovement Dodge Backward");
                }
                else if (__instance.dodgeDirection == __instance.transform.right)
                {
                    owoSkin.LOG($"NewMovement Dodge Right");
                }
                else
                {
                    owoSkin.LOG($"NewMovement Dodge Left");
                }
            }
        }

        [HarmonyPatch(typeof(NewMovement), "Launch")]
        public class OnLaunch
        {
            [HarmonyPostfix]
            public static void Postfix(NewMovement __instance)
            {
                owoSkin.StopUltraSpeed();

                if (__instance.modNoDashSlide) return;
                owoSkin.LOG($"NewMovement Launch");
            }
        }
        
        [HarmonyPatch(typeof(Punch), "PunchSuccess")]
        public class OnPunchSuccess
        {
            [HarmonyPrefix]
            public static void Prefix(Vector3 point, Transform target)
            {
                owoSkin.LOG($"Punch PunchSuccess - {point} - {target}");
            }
        }

        [HarmonyPatch(typeof(Punch), "Parry")]
        public class OnParry2
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                owoSkin.LOG($"Punch Parry");
            }
        }

        #endregion

        #region Impacts

        [HarmonyPatch(typeof(NewMovement), "GetHurt")]
        public class OnGetHurt
        {
            [HarmonyPostfix]
            public static void Postfix(NewMovement __instance, int damage, bool invincible, float scoreLossMultiplier = 1f, bool explosion = false, bool instablack = false, float hardDamageMultiplier = 0.35f, bool ignoreInvincibility = false)
            {
                if (damage <= 0) return;
                if (__instance.hp <= 0) owoSkin.Feel("Destruction");
            }
        }

        //unused?
        [HarmonyPatch(typeof(Wicked), "OnCollisionEnter")]
        public class OnWicked
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, Wicked __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    owoSkin.LOG($"Wicked OnCollisionEnter");
                }
            }
        }

        [HarmonyPatch(typeof(VirtueInsignia), "OnTriggerEnter")]
        public class OnVirtueInsignia
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, VirtueInsignia __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    owoSkin.LOG($"VirtueInsignia OnTriggerEnter - {__instance.transform.position}");
                }
            }
        }

        //unused?
        [HarmonyPatch(typeof(ThrownSword), "OnTriggerEnter")]
        public class OnThrownSword
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, ThrownSword __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    owoSkin.LOG($"ThrownSword OnTriggerEnter - {__instance.transform.position}");
                }
            }
        }

        [HarmonyPatch(typeof(SwingCheck2), "CheckCollision")]
        public class OnSwingCheck2
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, SwingCheck2 __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    owoSkin.LOG($"SwingCheck2 CheckCollision - {__instance.transform.position}");
                }
            }
        }

        [HarmonyPatch(typeof(RevolverBeam), "ExecuteHits")]
        public class OnRevolverBeam
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, RaycastHit currentHit)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    owoSkin.LOG($"RevolverBeam ExecuteHits - {currentHit.point}");
                }
            }
        }

        [HarmonyPatch(typeof(PhysicalShockwave), "CheckCollision")]
        public class OnShockWave
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    owoSkin.LOG($"PhysicalShockwave ChekCollision");
                }
            }
        }

        [HarmonyPatch(typeof(Nail), "OnCollisionEnter")]
        public class OnNail
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, Nail __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    owoSkin.LOG($"Nail OnCollisionEnter - {__instance.transform.position}");

                }
            }
        }

        //unused?
        [HarmonyPatch(typeof(MassSpear), "OnTriggerEnter")]
        public class OnMassSpear
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, Projectile __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    owoSkin.LOG($"MassSpear OnTriggerEnter - {__instance.transform.position}");
                }
            }
        }

        [HarmonyPatch(typeof(HurtZone), "FixedUpdate")]
        public class OnHurtZone
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, HurtZone __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    owoSkin.LOG($"HurtZone FixedUpdate - {__instance.transform.position}");
                }
            }
        }

        [HarmonyPatch(typeof(DeathZone), "GotHit")]
        public class OnDeathZone
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, DeathZone __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    owoSkin.LOG($"DeathZone GotHit - {__instance.transform.position}");

                }
            }
        }

        [HarmonyPatch(typeof(Explosion), "Collide")]
        public class OnExplosion
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, Explosion __instance, Collider other)
            {
                if (MonoSingleton<NewMovement>.Instance.hp < __state)
                {
                    //mmm linear scaling
                    //float distance = Vector3.Distance(other.transform.position, __instance.transform.position);
                    //float intensity = Mathf.Min(Mathf.Max(1 - (distance / 18), 0.1f), 0.75f);
                    //owoSkin.Feel("ExplosionBelly", intensity);

                    owoSkin.LOG($"Explosion Collide");
                }
            }
        }

        //unused?
        [HarmonyPatch(typeof(BeamgunBeam), "Update")]
        public class OnBeamgun
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, BeamgunBeam __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    owoSkin.LOG($"BeamgunBeam Beam Start");
                    LayerMask layerMask = LayerMaskDefaults.Get(LMD.EnemiesAndEnvironment);
                    float playerDamageCooldown = Traverse.Create(__instance).Field("playerDamageCooldown").GetValue<float>();
                    if (__instance.canHitPlayer && (double)playerDamageCooldown <= 0.0)
                        layerMask = LayerMaskDefaults.Get(LMD.EnemiesEnvironmentAndPlayer);
                    RaycastHit hitInfo;
                    if (Physics.Raycast(__instance.transform.position, __instance.transform.forward, out hitInfo, float.PositiveInfinity, (int)layerMask, QueryTriggerInteraction.Ignore))
                    {
                        owoSkin.LOG($"BeamgunBeam Position - {hitInfo.point}");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Projectile), "TimeToDie")]
        public class OnProjectile
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, Projectile __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    owoSkin.LOG($"Projectile TimeToDie hit position - {__instance.transform.position}");
                }
            }
        }

        //unused?
        [HarmonyPatch(typeof(BlackHoleProjectile), "OnTriggerEnter")]
        public class OnBlackHoleProjectile
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    if (MonoSingleton<NewMovement>.Instance.hp < __state)
                    {
                        owoSkin.LOG($"BlackHoleProjectile OnTriggerEnter");

                    }
                }
            }
        }

        //unused?
        [HarmonyPatch(typeof(Coin), "ShootAtPlayer")]
        public class OnCoin
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, Coin __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    owoSkin.LOG($"Coin Shoot at Player - {__instance.transform.position}");

                }
            }
        }

        //unused?
        [HarmonyPatch(typeof(ContinuousBeam), "Update")]
        public class OnContinuousBeam
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, ContinuousBeam __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    owoSkin.LOG($"Continuous Beam Start");
                    Vector3 zero = Vector3.zero;
                    RaycastHit hitInfo;
                    LayerMask environmentMask = Traverse.Create(__instance).Field("enviromentMask").GetValue<LayerMask>();
                    Vector3 vector3 = !Physics.Raycast(__instance.transform.position, __instance.transform.forward, out hitInfo, float.PositiveInfinity, (int)environmentMask) ? __instance.transform.position + __instance.transform.forward * 999f : hitInfo.point;
                    LineRenderer lr = Traverse.Create(__instance).Field("lr").GetValue<LineRenderer>();
                    lr.SetPosition(0, __instance.transform.position);
                    lr.SetPosition(1, vector3);
                    if ((bool)__instance.impactEffect)
                        __instance.impactEffect.transform.position = vector3;
                    LayerMask hitMask = Traverse.Create(__instance).Field("hitMask").GetValue<LayerMask>();
                    RaycastHit[] raycastHitArray = Physics.SphereCastAll(__instance.transform.position + __instance.transform.forward * 0.35f, 0.35f, __instance.transform.forward, Vector3.Distance(__instance.transform.position, vector3) - 0.35f, (int)hitMask);
                    if (raycastHitArray != null && raycastHitArray.Length != 0)
                    {
                        float playerCooldown = Traverse.Create(__instance).Field("playerCooldown").GetValue<float>();
                        for (int index = 0; index < raycastHitArray.Length; ++index)
                        {
                            if (raycastHitArray[index].collider.gameObject.tag == "Player" && __instance.canHitPlayer && (double)playerCooldown <= 0.0)
                            {
                                owoSkin.LOG($"Coninuous beam impact position - {raycastHitArray[index].point}");
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(FireZone), "OnTriggerStay")]
        public class OnFireZone
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, DeathZone __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    owoSkin.LOG($"FireZone OnTriggerStay - {__instance.transform.position}");

                }
            }
        }

        #endregion
        
        #region Weapon

        #region Revolver
        [HarmonyPatch(typeof(Revolver), "Shoot")]
        public class OnRevolverShoot
        {
            [HarmonyPostfix]
            public static void Postfix(Revolver __instance, int shotType)
            {

                owoSkin.LOG($"Revolver Shoot - Variation: {__instance.gunVariation} Typo - {shotType} ");
            }
        }
        #endregion

        #region Shotgun

        [HarmonyPatch(typeof(Shotgun), "Shoot")]
        public class OnShotgunShoot
        {
            [HarmonyPostfix]
            public static void Postfix(Shotgun __instance)
            {
                
                owoSkin.LOG($"Shotgun Shoot - Variation: {__instance.variation} - PrimaryCharge: {__instance.primaryCharge}");
            }
        }
        
        [HarmonyPatch(typeof(Shotgun), "ShootSinks")]
        public class OnShotgunShootSpecial0
        {
            [HarmonyPostfix]
            public static void Postfix(Shotgun __instance)
            {
                owoSkin.LOG($"Shotgun Shoot SP - Variation: {__instance.variation} - PrimaryCharge: {__instance.primaryCharge}");
            }
        }
        
        [HarmonyPatch(typeof(Shotgun), "ShootSaw")]
        public class OnShotgunShootSpecial2
        {
            [HarmonyPostfix]
            public static void Postfix(Shotgun __instance)
            {
                owoSkin.LOG($"Shotgun Shoot SP - Variation: {__instance.variation} - PrimaryCharge: {__instance.primaryCharge}");

            }
        }

        #endregion

        #region NailGun
        [HarmonyPatch(typeof(Nailgun), "Shoot")]
        public class OnNailgunShoot
        {
            [HarmonyPostfix]
            public static void Postfix(Nailgun __instance)
            {

                owoSkin.LOG($"Nailgun Shoot - {__instance.variation}");
            }
        }
        
        [HarmonyPatch(typeof(Nailgun), "ShootZapper")]
        public class OnNailgunShootV0
        {
            [HarmonyPostfix]
            public static void Postfix(Nailgun __instance)
            {
                owoSkin.LOG($"Nailgun ShootZapper - {__instance.variation}");
            }
        }

        [HarmonyPatch(typeof(Nailgun), "SuperSaw")]
        public class OnNailgunShootV1
        {
            [HarmonyPostfix]
            public static void Postfix(Nailgun __instance)
            {
                owoSkin.LOG($"Nailgun SuperSaw - {__instance.variation}");
            }
        }

        [HarmonyPatch(typeof(Nailgun), "BurstFire")]
        public class OnNailgunShootV2
        {
            [HarmonyPostfix]
            public static void Postfix(Nailgun __instance)
            {
                owoSkin.LOG($"Nailgun BurstFire - {__instance.variation}");
            }
        }

        [HarmonyPatch(typeof(Nailgun), "ShootMagnet")]
        public class OnNailgunShootV3
        {
            [HarmonyPostfix]
            public static void Postfix(Nailgun __instance)
            {
                owoSkin.LOG($"Nailgun ShootMagnet - {__instance.variation}");
            }
        }
        #endregion

        #region RailCannon
        [HarmonyPatch(typeof(Railcannon), "Shoot")]
        public class OnRailcannonShoot
        {
            [HarmonyPostfix]
            public static void Postfix(Railcannon __instance)
            {

                owoSkin.LOG($"Railcannon Shoot  - {__instance.variation}");
            }
        }
        #endregion

        #region RocketLauncher
        [HarmonyPatch(typeof(RocketLauncher), "Shoot")]
        public class OnRocketLauncherShoot
        {
            [HarmonyPostfix]
            public static void Postfix(RocketLauncher __instance)
            {

                owoSkin.LOG($"RocketLauncher Shoot - {__instance.variation}");
            }
        }
        
        [HarmonyPatch(typeof(RocketLauncher), "ShootCannonball")]
        public class OnRocketLauncherShootV0
        {
            [HarmonyPostfix]
            public static void Postfix(RocketLauncher __instance)
            {

                owoSkin.LOG($"RocketLauncher ShootCannonball - {__instance.variation}");
            }
        }
        
        [HarmonyPatch(typeof(RocketLauncher), "ShootNapalm")]
        public class OnRocketLauncherShootV1
        {
            [HarmonyPostfix]
            public static void Postfix(RocketLauncher __instance)
            {

                owoSkin.LOG($"RocketLauncher ShootNapalm - {__instance.variation}");
            }
        }
        #endregion


        #endregion

        [HarmonyPatch(typeof(DualWieldPickup), "PickedUp")]
        public class OnPickedUp
        {
            [HarmonyPostfix]
            public static void Postfix(NewMovement __instance)
            {
                owoSkin.Feel("DualWield");
            }
        }

        [HarmonyPatch(typeof(NewMovement), "SuperCharge")]
        public class OnSuperCharge
        {
            [HarmonyPostfix]
            public static void Postfix(NewMovement __instance)
            {
                owoSkin.LOG("SuperHeal");
            }
        }

        [HarmonyPatch(typeof(NewMovement), "Respawn")]
        public class OnRespawn
        {
            [HarmonyPostfix]
            public static void Postfix(NewMovement __instance)
            {
                owoSkin.Feel("Loading Up");
            }
        }


    }
}





