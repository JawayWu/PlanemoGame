using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour {
    private AudioSource aus;

    [SerializeField]
    [Tooltip("List of soundtracks")]
    private AudioClip[] tracks;

    private const float MIN_VOLUME = 0, MAX_VOLUME = 0.6f;

    private int currTrack;
    private float time, endTime, nextTime;
    private float easeStart, easeEnd;
    private bool playing;

    #region Unity_functions
    public void Awake() {
        aus = GetComponent<AudioSource>();
        currTrack = 0;
        time = -21f;
        endTime = 0;
        nextTime = 0;
        easeStart = 0;
        easeEnd = 0;
        playing = false;
    }

    public void Update() {
        if (!playing && time > nextTime) {
            aus.volume = 0;
            playing = true;
            AudioClip auc = tracks[currTrack];
            aus.clip = auc;
            aus.Play();
            easeStart = time;
            easeEnd = time + 10;
            endTime = time + auc.length;
            nextTime = endTime + 30;
        } else if (playing && time > endTime) {
            playing = false;
            aus.Stop();
            currTrack = (currTrack + 1) % tracks.Length;
        } else if (playing && easeStart <= time && time <= easeEnd) {
            aus.volume = MIN_VOLUME + (MAX_VOLUME - MIN_VOLUME) * (time - easeStart) / (easeEnd - easeStart);
        }
        time += Time.deltaTime;
    }
    #endregion
}
