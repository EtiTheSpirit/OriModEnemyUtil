using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SeinModloader;
using UnityEngine;

/** From Xan#1760 - This mod removes enemies from the screen via a hotkey. 
 * 
 * It also has modes to prevent enemies from spawning entirely.
 * Additionally, if AUTO_PERSISTENCE is on (see controls at the bottom of the code, they are in console too), any enemies removed will stay removed
 *	until the mode is turned off.
 * 
 */
namespace OriEnemyModNew {
	public class OriEnemyModNew : Mod {
		public override string Name { get { return "OriEnemyOverride"; } }         // Internal name for your mod
		public override string DisplayName { get { return "Ori Enemy Override Utility"; } }  // Display name for your mod, shown in console, mod list and possibly keybindings
		public override string Author { get { return "Xan"; } }             // Your username
		public override Version Version { get { return new Version(1, 0, 0); } } // Your mod's version
		public override string VersionExtra { get { return ""; } }               // An extra string to tack on as needed
		public override string GameVersion { get { return "DE"; } }              // The game's version. OatBF doesn't show its version or build, so just use DE/Original
		public override bool Preload { get { return false; } }                   // Whether your mod should be loaded before all other non-preload mods
		public override bool CanBeUnloaded { get { return false; } }             // Whether your mod can be unloaded. Most cases it can't unless you design it to.
		

		public override void OnLoad() {
			// Code to run when the mod is loaded
			GameObject modObj = new GameObject();
			modObj.name = "OriEnemyOverrideObject";
			modObj.AddComponent<OriMod>();
			UnityEngine.Object.DontDestroyOnLoad(modObj);
		}

		public override void OnUnload() {
			// Code to run when the mod is unloaded, or the game is closing

		}
	}

	public class OriMod : MonoBehaviour {
		protected bool isDown = false;
		protected bool click = false;
		protected bool autoRemove = false;
		protected bool killsPersistent = false;
		protected int lastEnemyCount = 0;
		protected List<MoonGuid> deadGuids = new List<MoonGuid>();
		protected List<Enemy> enemiesWithDamageHook = new List<Enemy>();
		
		protected void xLog(object o) {
			Debug.Log("[OriEnemyOverride]: " + o.ToString());
		}

		protected Enemy[] getAllLoadedEnemies() {
			Enemy[] enemies = FindObjectsOfType<Enemy>();
			return enemies;
		}

		protected void removeEnemy(Enemy e) {
			MoonGuid id = e.MoonGuid;
			if (!deadGuids.Contains(id) && killsPersistent) {
				deadGuids.Add(id);
				xLog("Perma-killed " + e);
				xLog("Enemy GUID: " + id);
			}
			e.DamageReciever.SetHealth(0);
			e.DamageReciever.UpdateActive();
		}

		protected void removeAllEnemies() {
			Enemy[] enemies = getAllLoadedEnemies();
			foreach (Enemy e in enemies) {
				removeEnemy(e);
			}
		}

		protected void removeAllEnemiesDamaged() {
			Enemy[] enemies = getAllLoadedEnemies();
			foreach (Enemy e in enemies) {
				if (e.DamageReciever.Health < e.DamageReciever.MaxHealth) {
					removeEnemy(e);
				}
			}
		}

		protected void removeAllPersistentlyKilledEnemies() {
			Enemy[] enemies = getAllLoadedEnemies();
			foreach (Enemy e in enemies) {
				if (deadGuids.Contains(e.MoonGuid)) {
					bool wasDamaged = (e.DamageReciever.Health < e.DamageReciever.MaxHealth);
					if (wasDamaged && killsPersistent) {
						xLog("Auto-killed Enemy GUID (Via kill persistence): " + e.MoonGuid);
					} else {
						xLog("Auto-killed Enemy GUID: " + e.MoonGuid);
					}
					removeEnemy(e);
				}
			}
		}

		protected void detectDeath() {
			Enemy[] enemies = getAllLoadedEnemies();
			foreach (Enemy e in enemies) {
				if (!enemiesWithDamageHook.Contains(e)) {
					enemiesWithDamageHook.Add(e);
					e.DamageReciever.OnDeathEvent.Add(delegate (Damage dmg) {
						if (killsPersistent) {
							MoonGuid id = e.MoonGuid;
							deadGuids.Add(id);
							xLog("Perma-killed " + e);
							xLog("Enemy GUID: " + id);
						}
					});
				}
			}
		}

		

		void Start() {
			xLog("Welcome to OriEnemyOverride. Here's the list of keybinds:\n" +
				"Numpad 1: Seamlessly remove all enemies from the screen.\n" +
				"Numpad 2: Remove all enemies that have recieved damage from Sein (It's a good way to filter what to remove and not remove).\n" +
				"Numpad 3: Toggle AUTO-REMOVE - If this is on, any enemies that load in will instantly be unloaded.\n" +
				"Numpad 4: Toggle KILL-PERSISTENCE - If this is on, any enemies that are removed (or killed) will permanently be unloaded.\n" +
				"Numpad 5: Display data. Will show if AUTO-REMOVE or KILL-PERSISTENCE are enabled.\n" +
				"Numpad 0: Undo ALL kill persistence tags, allowing enemies to respawn again.");
		}

		void Update(float delta) {
			if (Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.Keypad4) || Input.GetKeyDown(KeyCode.Keypad5) || Input.GetKeyDown(KeyCode.Keypad0)) {
				if (!isDown) {
					isDown = true;
					if (Input.GetKeyDown(KeyCode.Keypad1)) {
						xLog("Enemy removal requested within screen area.");
						removeAllEnemies();
					} else if (Input.GetKeyDown(KeyCode.Keypad2)) {
						xLog("Enemy removal requested within screen area (Damaged enemies only).");
						removeAllEnemiesDamaged();
					} else if (Input.GetKeyDown(KeyCode.Keypad3)) {
						autoRemove = !autoRemove;
						xLog("AUTO-REMOVE: " + (autoRemove ? "ON" : "OFF"));
					} else if (Input.GetKeyDown(KeyCode.Keypad4)) {
						killsPersistent = !killsPersistent;
						xLog("PERSISTENT KILLS: " + (killsPersistent ? "ON" : "OFF"));
					} else if (Input.GetKeyDown(KeyCode.Keypad5)) {
						xLog("AUTO-REMOVE: " + (autoRemove ? "ON" : "OFF"));
						xLog("PERSISTENT KILLS: " + (killsPersistent ? "ON" : "OFF"));
					} else if (Input.GetKeyDown(KeyCode.Keypad0)) {
						xLog("All kill persistence data has been cleared.");
						deadGuids.Clear();
						enemiesWithDamageHook.Clear(); //Intially I thought I didn't have to clear this but if any enemies respawn they are reset.
					}
				}
			} else {
				isDown = false;
			}

			Enemy[] enemies = getAllLoadedEnemies();
			if (enemies.Length > 0) {
				if (lastEnemyCount != enemies.Length) {
					detectDeath();
					lastEnemyCount = enemies.Length;
				}
			}

			if (autoRemove) {
				removeAllEnemies();
			}
			if (deadGuids.Count > 0) {
				removeAllPersistentlyKilledEnemies();
			}
		}
	}
}
