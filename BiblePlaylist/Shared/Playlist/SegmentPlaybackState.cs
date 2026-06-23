public class SegmentPlaybackState
    {
        public double LastTime { get; set; }
        public double LastDuration { get; set; }

        public void OnSegmentEnd(double currentTime, double duration)
        {
            LastTime = currentTime;
            LastDuration = duration;
            // Logic for highlighting/state update can go here
        }
    }