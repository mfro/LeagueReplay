using System;
using System.Collections.Generic;
using System.IO;
using MFroehlich.Parsing.DynamicJSON;
using MFroehlich.Parsing.MFro;

namespace LeagueReplay {
  public class MFroReplay {
    public JSONObject MetaData {
      get {
        byte[] raw = new byte[meta.Length];
        file.Seek(meta.Offset, SeekOrigin.Begin);
        file.Read(raw, 0, raw.Length);
        return MFroFormat.Deserialize(raw);
      }
    }
    public JSONObject Combine {
      get {
        byte[] raw = new byte[combine.Length];
        file.Seek(combine.Offset, SeekOrigin.Begin);
        file.Read(raw, 0, raw.Length);
        return MFroFormat.Deserialize(raw);
      }
    }
    public long GameId {
      get {
        return Combine["gameId"];
      }
    }
    public long SummonerId { get; set; }

    public const byte VERSION = 4;
    private FileStream file;
    private Position[] chunks, frames;
    private Position combine;
    private Position meta;

    public readonly byte version;

    public MFroReplay(FileInfo src) {
      this.file = src.OpenRead();
      this.version = (byte) file.ReadByte();

      combine = new Position(ReadInt(), ReadInt());
      meta = new Position(ReadInt(), ReadInt());
      int chunksOffset = ReadInt();
      chunks = new Position[ReadInt()];
      int framesOffset = ReadInt();
      frames = new Position[ReadInt()];
      if (version < 4) SummonerId = 0;
      else SummonerId = ReadLong();

      file.Seek(chunksOffset, SeekOrigin.Begin);
      for (int i = 0; i < chunks.Length; i++) chunks[i] = new Position(ReadInt(), ReadInt());
      file.Seek(framesOffset, SeekOrigin.Begin);
      for (int i = 0; i < frames.Length; i++) frames[i] = new Position(ReadInt(), ReadInt());
    }

    public static byte[] Flip(byte[] bytes) {
      byte[] ret = new byte[bytes.Length];
      for (int i = 0; i < bytes.Length; i++) ret[bytes.Length - 1 - i] = bytes[i];
      return ret;
    }

    private int ReadInt() {
      byte[] data = new byte[4];
      file.Read(data, 0, 4);
      if (BitConverter.IsLittleEndian) data = Flip(data);
      return BitConverter.ToInt32(data, 0);
    }

    private long ReadLong() {
      byte[] data = new byte[8];
      file.Read(data, 0, 8);
      if (BitConverter.IsLittleEndian) data = Flip(data);
      return BitConverter.ToInt64(data, 0);
    }

    public byte[] GetChunk(int chunkId) {
      byte[] data = new byte[chunks[chunkId - 1].Length];
      file.Seek(chunks[chunkId - 1].Offset, SeekOrigin.Begin);
      for (int i = 0; i < data.Length; i++) i += file.Read(data, 0, data.Length - i);
      return data;
    }

    public byte[] GetFrame(int frameId) {
      byte[] data = new byte[frames[frameId - 1].Length];
      file.Seek(frames[frameId - 1].Offset, SeekOrigin.Begin);
      for (int i = 0; i < data.Length; i++) i += file.Read(data, 0, data.Length - i);
      return data;
    }

    public static void Pack(FileStream tmp, FileInfo outFile, List<Position> chunks, List<Position> frames,
      JSONObject combine, JSONObject meta, long summId) {
      byte[] metaRaw = MFroFormat.Serialize(meta);
      byte[] combineRaw = MFroFormat.Serialize(combine);

      using(var output = outFile.OpenWrite())
      using (tmp) {
        int offset = 0;
        output.WriteByte(VERSION);

        output.WriteInt(offset += 41);
        output.WriteInt(combineRaw.Length);

        output.WriteInt(offset += combineRaw.Length);
        output.WriteInt(metaRaw.Length);

        output.WriteInt(offset += metaRaw.Length);
        output.WriteInt((int) meta["lastChunkId"]);

        output.WriteInt(offset += meta["lastChunkId"] * 8);
        output.WriteInt((int) meta["lastKeyFrameId"]);

        output.WriteLong(summId);

        offset += meta["lastKeyFrameId"] * 8;

        output.Write(combineRaw, 0, combineRaw.Length);
        output.Write(metaRaw, 0, metaRaw.Length);

        for (int i = 0; i < meta["lastChunkId"]; i++) {
          int len = chunks[i].Length;
          output.WriteInt(offset);
          output.WriteInt(len);
          offset += len;
        }

        for (int i = 0; i < meta["lastKeyFrameId"]; i++) {
          int len = frames[i].Length;
          output.WriteInt(offset);
          output.WriteInt(len);
          offset += len;
        }

        for (int i = 0; i < meta["lastChunkId"]; i++) {
          tmp.Seek(chunks[i].Offset, SeekOrigin.Begin);
          byte[] chunk = new byte[chunks[i].Length];
          tmp.ReadFully(chunk, 0, chunk.Length);
          output.Write(chunk, 0, chunk.Length);
        }

        for (int i = 0; i < meta["lastKeyFrameId"]; i++) {
          tmp.Seek(frames[i].Offset, SeekOrigin.Begin);
          byte[] frame = new byte[frames[i].Length];
          tmp.ReadFully(frame, 0, frame.Length);
          output.Write(frame, 0, frame.Length);
        }
      }
    }
  }

  public class Position {
    public int Length { get; set; }
    public int Offset { get; set; }

    public Position(int off, int len) {
      this.Length = len;
      this.Offset = off;
    }

    public override string ToString() {
      return "{" + Offset + "," + Length + "}";
    }
  }
}
