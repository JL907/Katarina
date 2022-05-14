using RoR2;
using UnityEngine;

namespace KatarinaMod
{
    public class MenuSound : MonoBehaviour
    {
        private uint playID;

        private void OnDestroy()
        {
            if (this.playID != 0) AkSoundEngine.StopPlayingID(this.playID);
        }

        private void OnEnable()
        {
            this.Invoke("PlayEffect", 0.05f);
        }

        private void PlayEffect()
        {
            if (Modules.Config.voiceLines.Value) this.playID = Util.PlaySound("KatarinaMenu", base.gameObject);
        }
    }
}