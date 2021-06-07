using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace TOGFireArrows
{
    public class TOGFireArrowsPatch : MonoBehaviour
    {
        private Harmony _harmony;

        public void Inject()
        {
            try
            {
                _harmony = new Harmony("FireArrows");
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                Debug.Log("Fire Arrows Loaded");
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }
        }

        [HarmonyPatch(typeof(VRCharController))]
        [HarmonyPatch("EnableAvatar")]
        // ReSharper disable once UnusedType.Local
        private static class VRCharControllerEnableAvatarPatch
        {
            [HarmonyPostfix]
            private static void Postfix(VRCharController __instance)
            {
                __instance.config.gameObject.AddComponent<FireArrowController>();
            }
        }

        [HarmonyPatch(typeof(BowScript))]
        [HarmonyPatch("Update")]
        // ReSharper disable once UnusedType.Local
        private static class BowScriptUpdatePatch
        {
            private static IEnumerator ShowMessage(Stats stats, string message)
            {
                var isActive = stats.config.orderManager.UMsg.activeSelf;
                var uiText = stats.config.orderManager.UMsg.transform.GetChild(0).GetComponent<Text>();
                var oldText = uiText.text;
                if (!isActive)
                    stats.config.orderManager.UMsg.SetActive(true);
                uiText.text = message;
                yield return new WaitForSeconds(0.5f);
                if (!isActive)
                {
                    stats.config.orderManager.UMsg.SetActive(false);
                }
                else
                {
                    if (uiText.text == message)
                        uiText.text = oldText;
                }

                yield return null;
            }

            [HarmonyPostfix]
            private static void Postfix(BowScript __instance)
            {
                BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                                         | BindingFlags.Static;

                Stats stats =
                    __instance.GetType().GetField("stats", bindFlags).GetValue(__instance) as Stats;

                if (__instance.wp && __instance.wp.isHeld)
                {
                    ControllerInput Controllers =
                        __instance.GetType().GetField("Controllers", bindFlags).GetValue(__instance) as ControllerInput;

                    var playerController = __instance.wp.isHeldRight
                        ? Controllers.RightController
                        : Controllers.LeftController;

                    var fireArrowController = stats.config.gameObject.GetComponent<FireArrowController>();
                    if (!fireArrowController.triggerPressed && playerController.triggerPressed)
                    {
                        //function
                        fireArrowController.isOnFire ^= true;

                        stats.config.StartCoroutine(ShowMessage(stats,
                            "Fire Arrow: " +
                            (stats.config.GetComponent<FireArrowController>()
                                .isOnFire
                                ? "On"
                                : "Off")));
                    }

                    fireArrowController.triggerPressed = playerController.triggerPressed;

                    if (__instance.isLoaded)
                    {
                        var projectileFireScript = __instance.Arrow.GetComponentInChildren<ProjectileFireScript>();
                        if (fireArrowController.isOnFire != projectileFireScript.isOnFire)
                        {
                            if (fireArrowController.isOnFire)
                                projectileFireScript.SetFireOn();
                            else
                                projectileFireScript.SetFireOff();
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BowScript))]
        [HarmonyPatch("FireArrow")]
        // ReSharper disable once UnusedType.Local
        private static class BowScriptFireArrowPatch
        {
            [HarmonyPrefix]
            private static void Prefix(BowScript __instance)
            {
                BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                                         | BindingFlags.Static;

                Stats stats =
                    __instance.GetType().GetField("stats", bindFlags).GetValue(__instance) as Stats;


                __instance.Arrow.GetComponentInChildren<ProjectileFireScript>().isOnFire =
                    stats.config.gameObject.GetComponent<FireArrowController>().isOnFire;
            }
        }
    }
}