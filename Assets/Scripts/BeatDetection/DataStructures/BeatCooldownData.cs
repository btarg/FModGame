namespace BeatDetection.DataStructures
{
    public class BeatCooldownData
    {
        public string id { get; }
        public float cooldownStartTime { get; set; }
        public int cooldownBeat { get; set; }

        public BeatCooldownData(string id, float cooldownStartTime, int cooldownBeat)
        {
            this.id = id;
            this.cooldownStartTime = cooldownStartTime;
            this.cooldownBeat = cooldownBeat;
        }
    }
}