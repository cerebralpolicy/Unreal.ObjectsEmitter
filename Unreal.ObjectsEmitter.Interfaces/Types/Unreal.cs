﻿using System.Runtime.InteropServices;
using System.Collections;
using System.Text;

namespace Unreal.ObjectsEmitter.Interfaces.Types;

public unsafe class UnrealObject
{
    public UnrealObject(string name, UObject* obj)
    {
        this.Name = name;
        this.Self = obj;
    }

    public string Name { get; set; }

    public UObject* Self { get; set; }
}

// From Unreal.ObjectDumpToJson by rirurin
// https://github.com/rirurin/Unreal.ObjectDumpToJson

// For g_namePool
[StructLayout(LayoutKind.Explicit, Size = 0x8)]
public unsafe struct FName // FNameEntryIds
{
    [FieldOffset(0x0)] public uint pool_location;
    [FieldOffset(0x4)] public uint unk1;
}

// By AnimatedSwine
[StructLayout(LayoutKind.Sequential)]
public unsafe struct FString
{
    TArray<char> Data; // characters are either ANSICHAR or WIDECHAR depending on platform. See definition in Core\Public\HAL\Platform.h

    public FString(string str)
    {
        Data.Max = str.Length + 1;
        Data.Num = Data.Max;
        Data.AllocatorInstance = (char*)Marshal.StringToHGlobalUni(str);
    }

    public readonly override string ToString()
        => Marshal.PtrToStringUni((nint)Data.AllocatorInstance, Data.Num);

    public readonly void Dispose()
        => Marshal.FreeHGlobal((nint)Data.AllocatorInstance);
}

/// <summary>
/// Enumerable wrapper for TArray.
/// </summary>
public unsafe class TArrayWrapper<T> : IEnumerable<T>, IEnumerator<T>
    where T : unmanaged
{
    private readonly TArray<T> array;
    private int pos = 0;

    public T Current => this.array.AllocatorInstance[pos];

    object IEnumerator.Current => this.Current;

    public TArrayWrapper(TArray<T> array)
    {
        this.array = array;
    }

    public bool MoveNext() => ++this.pos < this.array.Num;

    public void Reset() => this.pos = 0;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public IEnumerator<T> GetEnumerator() => this;

    IEnumerator IEnumerable.GetEnumerator() => this;
}

public enum EFindName
{
    /// <summary>
    /// Find name or return empty FName.
    /// </summary>
    FName_Find,

    /// <summary>
    /// Find or add name.
    /// </summary>
    FName_Add
};

/// <summary>
/// From Unreal.ObjectDumpToJson by rirurin
/// https://github.com/rirurin/Unreal.ObjectDumpToJson
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 0x10)]
public unsafe struct FNamePool
{
    [FieldOffset(0x8)] public uint pool_count;
    [FieldOffset(0xc)] public uint name_count;
    public nint GetPool(uint pool_idx) { fixed (FNamePool* self = &this) return *((nint*)(self + 1) + pool_idx); }
    public string GetString(FName name) => GetString(name.pool_location);
    public string GetString(uint pool_loc)
    {
        fixed (FNamePool* self = &this)
        {
            // Get appropriate pool
            nint ptr = GetPool(pool_loc >> 0x10); // 0xABB2B - pool 0xA
                                                  // Go to name entry in pool
            ptr += (nint)((pool_loc & 0xFFFF) * 2);
            return ((FString*)ptr)->GetString() ?? string.Empty;
        }
    }

    public void SetFName(uint fnamePoolLoc, string newString)
    {
        fixed (FNamePool* self = &this)
        {
            // Get appropriate pool
            nint ptr = GetPool(fnamePoolLoc >> 0x10); // 0xABB2B - pool 0xA
                                                  // Go to name entry in pool
            ptr += (nint)((fnamePoolLoc & 0xFFFF) * 2);
            var newStr = new DataTableString(newString);
            *(nint*)ptr = (nint)(&newStr);
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x2)]
    private unsafe struct FString
    {
        // Flags:
        // bIsWide : 1;
        // probeHashBits : 5;
        // Length : 10;
        // Get Length: flags >> 6
        [FieldOffset(0x0)] public short flags;
        public string GetString() { fixed (FString* self = &this) return Marshal.PtrToStringAnsi((nint)(self + 1), flags >> 6); }
    }
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct DataTableString
{
    public const int MAX_LENGTH = 1023;

    public DataTableString(string newString)
    {
        this.Flags = (1023 << 6) | 32;
        this.SetString(newString);
    }

    public ushort Flags;

    public void SetString(string newString)
    {
        if (newString.Length > MAX_LENGTH)
        {
            return;
        }

        var flagsMask = 0b111111;
        var flagsValue = this.Flags & flagsMask;
        var newLength = newString.Length;
        var newDtFlags = (ushort)((newLength << 6) | flagsValue);
        this.Flags = newDtFlags;

        var bytes = Encoding.ASCII.GetBytes(newString);
        fixed (DataTableString* self = &this)
        {
            var strStart = (nint)self + 2;
            Marshal.Copy(bytes, 0, strStart, bytes.Length);
        }
    }

    public string? GetString()
    {
        fixed (DataTableString* self = &this)
        {
            var strStart = (byte*)self + 2;
            var length = this.GetLength();
            var buffer = new byte[length];
            Marshal.Copy((nint)strStart, buffer, 0, length);
            return Encoding.ASCII.GetString(buffer);
        }
    }

    public readonly int GetLength() => this.Flags >> 6;

    public static DataTableString* Create()
    {
        var instance = new DataTableString();
        var pointer = (DataTableString*)Marshal.AllocHGlobal(MAX_LENGTH + 2);
        *pointer = instance;
        return pointer;
    }
}

[StructLayout(LayoutKind.Sequential, Size = 0x10)]
public unsafe struct TArray<T> where T : unmanaged
{
    public T* AllocatorInstance;
    public int Num;
    public int Max;
}

[StructLayout(LayoutKind.Sequential)]
public struct TMapElement<TMapInnerKeyType, TMapInnerValueType>
    where TMapInnerKeyType : unmanaged
    where TMapInnerValueType : unmanaged
{
    public TMapInnerKeyType Key;
    public TMapInnerValueType Value;
    public long Padding; // Figure out what this is later
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct TMap
{
    public TMapElement<nint, nint>* Elements;
    public int MapNum;
    public int MapMax;
}

public enum EObjectFlags : uint
{
    Public = 1 << 0x0,
    Standalone = 1 << 0x1,
    MarkAsNative = 1 << 0x2,
    Transactional = 1 << 0x3,
    ClassDefaultObject = 1 << 0x4,
    ArchetypeObject = 1 << 0x5,
    Transient = 1 << 0x6,
    MarkAsRootSet = 1 << 0x7,
    TagGarbageTemp = 1 << 0x8,
    NeedInitialization = 1 << 0x9,
    NeedLoad = 1 << 0xa,
    KeepForCooker = 1 << 0xb,
    NeedPostLoad = 1 << 0xc,
    NeedPostLoadSubobjects = 1 << 0xd,
    NewerVersionExists = 1 << 0xe,
    BeginDestroyed = 1 << 0xf,
    FinishDestroyed = 1 << 0x10,
    BeingRegenerated = 1 << 0x11,
    DefaultSubObject = 1 << 0x12,
    WasLoaded = 1 << 0x13,
    TextExportTransient = 1 << 0x14,
    LoadCompleted = 1 << 0x15,
    InheritableComponentTemplate = 1 << 0x16,
    DuplicateTransient = 1 << 0x17,
    StrongRefOnFrame = 1 << 0x18,
    NonPIEDuplicateTransient = 1 << 0x19,
    Dynamic = 1 << 0x1a,
    WillBeLoaded = 1 << 0x1b,
    HasExternalPackage = 1 << 0x1c,
    PendingKill = 1 << 0x1d,
    Garbage = 1 << 0x1e,
    AllocatedInSharedPage = (uint)1 << 0x1f
}

[StructLayout(LayoutKind.Explicit, Size = 0x28)]
public unsafe struct UObject
{
    [FieldOffset(0x0008) ]public EObjectFlags ObjectFlags; // @ 0x8
    [FieldOffset(0x0018)] public FName NamePrivate;
}

[StructLayout(LayoutKind.Explicit, Size = 0xB0)]
public unsafe struct UDataTable
{
    [FieldOffset(0x0000)] public UObject BaseObj;
    [FieldOffset(0x0028)] public UScriptStruct* RowStruct;
    [FieldOffset(0x0030)] public TMap RowMap;
};

[StructLayout(LayoutKind.Explicit, Size = 0xC0)]
public unsafe struct UScriptStruct
{
    [FieldOffset(0x0000)] public UObject baseObj;
}