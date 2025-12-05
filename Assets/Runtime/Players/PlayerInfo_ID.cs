using Unity.Collections;
using System;

/// <summary>
/// 玩家唯一标识符（不可变值类型，零 GC 分配）
/// </summary>
public readonly struct PlayerInfo_ID : IEquatable<PlayerInfo_ID>
{
    public readonly FixedString32Bytes uid;

    public PlayerInfo_ID(string uidString)
    {
        if (string.IsNullOrEmpty(uidString))
            throw new ArgumentException("UID cannot be null or empty", nameof(uidString));

        // FixedString32Bytes 最大支持 32 字节（UTF-8 编码）
        // 对于 ASCII 字符：32 个字符
        // 对于中文字符（UTF-8 通常 3 字节）：约 10-11 个字符
        const int maxBytes = 32;
        if (System.Text.Encoding.UTF8.GetByteCount(uidString) > maxBytes)
        {
            throw new ArgumentException(
                $"UID length exceeds maximum {maxBytes} bytes. Current length: {System.Text.Encoding.UTF8.GetByteCount(uidString)} bytes",
                nameof(uidString));
        }

        uid = new FixedString32Bytes(uidString);
    }

    public bool Equals(PlayerInfo_ID other)
    {
        return uid.Equals(other.uid);
    }

    public override bool Equals(object obj)
    {
        return obj is PlayerInfo_ID other && Equals(other);
    }

    public override int GetHashCode()
    {
        return uid.GetHashCode();
    }

    public static bool operator ==(PlayerInfo_ID left, PlayerInfo_ID right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PlayerInfo_ID left, PlayerInfo_ID right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return uid.ToString();
    }

    // 隐式转换（可选，如果觉得方便可以启用）
    // public static implicit operator PlayerInfo_ID(string uidString)
    // {
    //     return new PlayerInfo_ID(uidString);
    // }
}