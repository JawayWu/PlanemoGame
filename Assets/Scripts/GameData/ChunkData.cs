using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class ChunkData : MonoBehaviour {
    public static Dictionary<Tuple<int, int>, Chunk.TileType[,]> frontTiletypes, backTiletypes;
    public static int seed = -1;

    public void Awake() {
        frontTiletypes = new Dictionary<Tuple<int, int>, Chunk.TileType[,]>();
        backTiletypes = new Dictionary<Tuple<int, int>, Chunk.TileType[,]>();

        ReadData();
    }

    //Gets the TileType of a tile at given world position
    public static Chunk.TileType GetTileType(Vector3Int position, Chunk.TilemapType tt) {
        int chunkSize = GenerationManager.Instance.chunkSize;
        int chunkX = ((position.x % GenerationManager.Instance.worldWidth) + GenerationManager.Instance.worldWidth)
            % GenerationManager.Instance.worldWidth / chunkSize;
        int chunkY = position.y / chunkSize;
        Tuple<int, int> tuplePos = new Tuple<int, int>(chunkX, chunkY);
        if (tt == Chunk.TilemapType.FRONT_BLOCKS) {
            if (frontTiletypes.ContainsKey(tuplePos)) {
                return frontTiletypes[tuplePos]
                    [((position.x % chunkSize) + chunkSize) % chunkSize, position.y % chunkSize];
            }
        } else {
            if (backTiletypes.ContainsKey(tuplePos)) {
                return backTiletypes[tuplePos]
                    [((position.x % chunkSize) + chunkSize) % chunkSize, position.y % chunkSize];
            }
        }

        return Chunk.TileType.AIR;
    }

    //Returns globalized version (0<=x<=w, 0<=y<=h) of given world position
    public static Vector3Int Global(Vector3Int position) {
        return new Vector3Int(
            ((position.x % GenerationManager.Instance.worldWidth) + GenerationManager.Instance.worldWidth)
                % GenerationManager.Instance.worldWidth,
            position.y,
            0);
    }

    //Returns chunk position (in terms of chunks) given tile position
    public static Vector3Int GetChunkPosition(Vector3Int position) {
        Vector3Int newChunkPosition = new Vector3Int(
            position.x / GenerationManager.Instance.chunkSize,
            position.y / GenerationManager.Instance.chunkSize, 0);

        return newChunkPosition;
    }

    public static void WriteData() {
        StreamWriter sw = new StreamWriter(Application.dataPath + "/" + SceneManager.GetActiveScene().name + "_front.txt");
        using (sw) {
            foreach (Tuple<int, int> t in frontTiletypes.Keys) {
                sw.WriteLine(t.Item1 + " " + t.Item2);
                Chunk.TileType[,] tiletypes = frontTiletypes[t];
                for (int r = 0; r < tiletypes.GetLength(0); r++) {
                    for (int c = 0; c < tiletypes.GetLength(1); c++) {
                        sw.WriteLine((int)tiletypes[r, c]);
                    }
                }
            }

            sw.Close();
        }

        StreamWriter sw2 = new StreamWriter(Application.dataPath + "/" + SceneManager.GetActiveScene().name + "_back.txt");
        using (sw2) {
            foreach (Tuple<int, int> t in backTiletypes.Keys) {
                sw2.WriteLine(t.Item1 + " " + t.Item2);
                Chunk.TileType[,] tiletypes = backTiletypes[t];
                for (int r = 0; r < tiletypes.GetLength(0); r++) {
                    for (int c = 0; c < tiletypes.GetLength(1); c++) {
                        sw2.WriteLine((int)tiletypes[r, c]);
                    }
                }
            }

            sw2.Close();
        }

        StreamWriter sw3 = new StreamWriter(Application.dataPath + "/" + SceneManager.GetActiveScene().name + "_seed.txt");
        using (sw3) {
            sw3.WriteLine(seed);
            sw3.Close();
        }
    }

    public static void ReadData() {
        try {
            StreamReader sr = new StreamReader(Application.dataPath + "/" + SceneManager.GetActiveScene().name + "_front.txt");
            using (sr) {
                for (string line = sr.ReadLine(); line != null; line = sr.ReadLine()) {
                    string[] pos = line.Split(' ');
                    int a = int.Parse(pos[0]), b = int.Parse(pos[1]);
                    Tuple<int, int> tuple = new Tuple<int, int>(a, b);
                    int size = GenerationManager.Instance.chunkSize;
                    frontTiletypes[tuple] = new Chunk.TileType[size, size];
                    for (int r = 0; r < size; r++) {
                        for (int c = 0; c < size; c++) {
                            int k = int.Parse(sr.ReadLine());
                            frontTiletypes[tuple][r, c] = (Chunk.TileType)k;
                        }
                    }
                }

                sr.Close();
            }

            StreamReader sr2 = new StreamReader(Application.dataPath + "/" + SceneManager.GetActiveScene().name + "_back.txt");
            using (sr2) {
                for (string line = sr2.ReadLine(); line != null; line = sr2.ReadLine()) {
                    string[] pos = line.Split(' ');
                    int a = int.Parse(pos[0]), b = int.Parse(pos[1]);
                    Tuple<int, int> tuple = new Tuple<int, int>(a, b);
                    int size = GenerationManager.Instance.chunkSize;
                    backTiletypes[tuple] = new Chunk.TileType[size, size];
                    for (int r = 0; r < size; r++) {
                        for (int c = 0; c < size; c++) {
                            int k = int.Parse(sr2.ReadLine());
                            backTiletypes[tuple][r, c] = (Chunk.TileType)k;
                        }
                    }
                }

                sr2.Close();
            }

            StreamReader sr3 = new StreamReader(Application.dataPath + "/" + SceneManager.GetActiveScene().name + "_seed.txt");
            using (sr3) {
                seed = int.Parse(sr3.ReadLine());
                sr3.Close();
            }
        } catch {
            return;
        }
    }
}
