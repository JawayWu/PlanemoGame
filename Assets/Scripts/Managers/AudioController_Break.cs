using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController_Break : MonoBehaviour {
    private AudioSource aus;

    [SerializeField]
    [Tooltip("List of soundtracks")]
    private AudioClip[] tracks;

    [SerializeField]
    [Tooltip("Player")]
    private PlayerMovement pm;

    private Targeting tgt;

    private const float VOLUME = 0.6f;

    private int currTrack;
    private float time, endTime, nextTime;
    private bool playing;

    #region Unity_functions
    public void Awake() {
        tgt = GetComponent<Targeting>();
        aus = GetComponent<AudioSource>();
        aus.volume = VOLUME;
        currTrack = 0;
        time = 0;
        endTime = 0;
        nextTime = 0;
        playing = false;
    }

    public void Update() {
        try {
            Chunk.TileType tt = tgt.chunk.GetChunkTileType(tgt.target, tgt.tilemap);
            if (pm.selected == 10 && tt != Chunk.TileType.AIR && Input.GetMouseButton(0)) {
                aus.UnPause();
                if (!playing && time > nextTime) {
                    playing = true;
                    AudioClip auc = tracks[currTrack];
                    aus.clip = auc;
                    aus.Play();
                    endTime = time + auc.length;
                    nextTime = endTime;
                } else if (playing && time > endTime) {
                    playing = false;
                    aus.Stop();
                    currTrack = (currTrack + 1) % tracks.Length;
                }
                time += Time.deltaTime;
            } else {
                aus.Pause();
            }
        } catch {
            aus.Pause();
        }
    }
    #endregion
}
