# Changelog

## 1.8.3
* Multiplayer fixes for daggers
   * Dagger pickups are now handled by the host, so every player sees the same result and a dagger can't be picked up twice.
   * Bouncing Blades daggers now drop correctly for every player, not just the host.
   * Fixed duplicate Bouncing Blades effects in multiplayer.
* Each Katarina can only pick up her own daggers.
* Daggers are now only visible to the Katarina who threw them.
* Landed daggers now show a League-style ground circle marking their pickup range.
* Landed daggers now show an on-screen marker (with an edge-of-screen arrow when off-screen) so they're easier to spot.
* Added [Runes](https://thunderstore.io/package/lemonlust/Runes/) as a dependency.

## 1.8.2
* Updated for Risk of Rain 2 version 1.5.0
   * Fixed Katarina failing to load.
   * Fixed thrown daggers showing the Commando grenade model.
* Updated dependencies to their latest versions.

## 1.8.0
* SOTS Update

## 1.7.0
* R2API Conversion

## 1.6.2
* Adjusted the animation speed on Katarina's sprinting animation.
* Bouncing Blades is now disabled when you have no target.
* Fixed skill descriptions.

## 1.6.1
After much discussion in the discord we have concluded that Katarina's damage was a little bit too high after the recent buffs. Targeted Shunpo also recently received a change to apply damage to the target further increasing her damage output. Considering the mechanical difficulty with Katarina we still thought her damage output was still too much. To keep her in line with other survivors we have adjusted the following values.

Please delete any old Katarina configuration file to update to the latest changes.

* Voracity damage decreased to 450% (down from 600%)
* Bouncing Blades damage decreased to 320% (down from 400%)
* Death Lotus Proc decreased to 0.25 (down from 0.5)
* Death Lotus CD decreased to 12 seconds (down from 16 seconds)

Skill descriptions will now reflect any changes made to the configuration file.

## 1.6.0
* Targetted Shunpo will now deal 210% damage on the target.

## 1.5.1
* Bouncing Blades now has fixed duration and is no longer affected by attack speed.
   * This fixes an issue where Bouncing Blades wouldn't produce a dagger at high attack speeds.

## 1.5.0
* Voracity damage increased to 600% (up from 400%)
* Bouncing Blades damage increased to 400% (up from 200%)
* Death Lotus damage increased to 200% per dagger (up from 100%)
* Death Lotus Proc coefficient increased to 0.5 (up from 0.25)

## 1.4.5
* Added Dynamic Bones on Katarina's hair.

## 1.4.4
* Voicelines can now be disabled in configuration because people complained

## 1.4.3
* Added High Noon Katarina Skin

## 1.4.2
* Updated to use Latest R2API
* Updated to use Latest BepinExPack
* Fixed an issue where Bouncing Blade Dagger was spawning before it hit enemies.
* Bouncing Blade Dagger Travel Speed reduced by 33%
* Bouncing Blade Bounce Range increased to 20m up from 15m
* Added configuration options to adjust Bouncing Blades Travel Speed and Bounce Range

## 1.4.1
* Added ability cooldowns to configuration
* Added Death Lotus radius to configuration
* M2 and Targetted Shunpo distance increased to 40 meters (up from 30 meters)
* Untargetted Shunpo distance increased
* Death Lotus radius increased from 20 meters to 30 meters
* Voracity radius increased to 15 meters (up from 10 meters)

## 1.4.0
* BetterUI support

## 1.3.2
* Updated to use the latest R2API

## 1.3.1
* Voracity Damage increased to 400% up from 300%
* Death Lotus Proc Coefficient reduced to 0.25 down from 0.5
* Added configuration 
* Fixed an issue with Voracity while player had Death Mark

## 1.3.0
* Added Mercenary Katarina Skin
* Added Red Card Katarina Skin
* Added Bilgewater Katarina Skin
* Added Kitty Cat Katarina Skin
* Added High Command Katarina Skin
* Added Sand Storm Katarina Skin
* Added Slay Belle Katarina Skin
* Added Warring Kingdoms Katarina Skin
* Added Project: Katarina Skin
* Added Battle Academia Katarina Skin
* Added Blood Moon Katarina Skin

## 1.2.0
* Fixed an issue where Voracity would cast multiple times when picking up daggers
* Fixed an issue where Daggers colliding with enemies would make them float in the air
* Fixed an issue where Bouncing Blades would go on cooldown when there were no valid targets in multiplayer
* Fixed an issue where Bouncing Blades sounds and animations weren't playing in multiplayer
* Dagger Physics adjusted
* Bouncing Blades now place dagger after first enemy hit
* Bouncing Blades visual effect speed increased

## 1.1.2
* Katarina sound attenuation lowered to 100 meters down from 200 meters
* Shunpo will blink towards Katarina's target only if the basic attack button is held
* Shunpo now refunds 75% of it's total cooldown instead of half of its remaining cooldown

## 1.1.1
* Shunpo will now reset half of its remaining duration when picking up daggers

## 1.1.0
* Katarina base damage reduced to 12 down from 15
* Katarina base damage per level reduced to 2.4 down from 3
* Shunpo now only throws 1 dagger at the start of the ability
* Shunpo's cooldown no longer resets after retrieving daggers
* Shunpo's cooldown reduced to 6 seconds down from 8
* Bouncing Blades duration reduced from 1 second down to 0.5 seconds
* Bouncing Blades cooldown reduced to 4 seconds down from 6
* Bouncing Blades invalid target lockout now matches the duration of the ability.
    * Bouncing Blades will go on cooldown on invalid target but will quickly refresh to match lockout duration.
    * Bonus attack speed reduce the duration and lock out of Bouncing Blades
* Death Lotus cooldown reduced to 16 from 20
* Death Lotus damage per blade reduced to 100% down from 150%
* Voracity kill rewards now only reduce current cooldowns by 1 second down from 2
* Voracity dagger retrieval now only does 300% damage down from 400%

## 1.0.1
* Shunpo can target enemies. If shunpo has no target it will blink a fixed distance in front of katarina.
* Death Lotus damage per dagger is now 150% down from 200% 
* Voracity damage is now 400% down from 600%

## 1.0.0
* Initial Release
