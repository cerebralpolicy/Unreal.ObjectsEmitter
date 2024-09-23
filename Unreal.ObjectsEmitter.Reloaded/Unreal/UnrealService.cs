using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Unreal.ObjectsEmitter.Interfaces;
using Unreal.ObjectsEmitter.Interfaces.Types;

namespace Unreal.ObjectsEmitter.Reloaded.Unreal;

internal unsafe class UnrealService : IUnreal
{
    [Function(CallingConventions.Microsoft)]
    private delegate FName* FName_ctor(FName* name, nint str, EFindName findTyp);
    private IHook<FName_ctor>? fname;

    [Function(CallingConventions.Microsoft)]
    private delegate nint FMemory_Malloc(long size, int alignment);
    private FMemory_Malloc? fmalloc;

    private readonly AssignmentList fnameAssigns = new();
    private readonly Dictionary<string, string> fnameDict = new();
    private readonly ConcurrentDictionary<string, nint> fnameCache = new();
    private FNamePool* g_namePool;

    public UnrealService()
    {
        ScanHooks.Add(
            "FGlobalNamePool",
            "4C 8D 05 ?? ?? ?? ?? EB ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 4C 8B C0 C6 05 " +
            "?? ?? ?? ?? 01 48 8B 44 24 ?? 48 8B D3 48 C1 E8 20 8D 0C ?? 49 03 4C ?? ?? E8 " +
            "?? ?? ?? ?? 48 8B C3",
            (hooks, result) =>
            {
                var namePoolPtr = GetGlobalAddress(result + 3);
                g_namePool = (FNamePool*)namePoolPtr;
            });

        ScanHooks.Add(
            "FName Constructor",
            "48 89 5C 24 ?? 57 48 83 EC 30 48 8B D9 48 89 54 24 ?? 33 C9 41 8B F8 4C 8B DA",
            (hooks, result) =>
            {
                this.fname = hooks.CreateHook<FName_ctor>(this.FName, result).Activate();
            });

        ScanHooks.Add(
            nameof(FMemory_Malloc),
            "48 89 5C 24 ?? 57 48 83 EC 20 48 8B F9 8B DA 48 8B 0D ?? ?? ?? ?? 48 85 C9",
            (hooks, result) => this.fmalloc = hooks.CreateWrapper<FMemory_Malloc>(result, out _));
    }

    public nint FMalloc(long size, int alignment) => this.fmalloc!(size, alignment);

    public void AssignFName(string modName, string fnameString, string newString)
    {
        if (fnameString == newString)
        {
            Log.Error($"Attempted to redirect FName to itself. This is considered an error and should be fixed. Mod: {modName} || String: {newString}");
            return;
        }

//      this.fnameDict[fnameString] = newString;
        this.fnameAssigns.TryAddRedirect(fnameString, newString, this);
        Log.Debug($"Assigned FName: {fnameString}\nMod: {modName} || New: {newString}");
    }

    public void UnassignFName(string modName, string fnameString)
    {
        this.fnameAssigns.TryGetRedirectFor(fnameString, out var redirect);
        if (redirect == null)
        {
            return;
        }
        var info = redirect.Value;
        var newString = info.Assign.Name;
        this.fnameAssigns.TryRemoveRedirectFor(fnameString); //
        this.fnameCache.TryRemove(fnameString, out var prevAddress);
        Log.Debug($"Reverted FName: {fnameString}\nMod: {modName} || Previous: {newString} ({prevAddress})");
    }

    public FName* FName(string str, EFindName findType = EFindName.FName_Add)
    {
        var fname = (FName*)this.fmalloc!(8, 16);
        var strPtr = StringsCache.GetStringPtrUni(str);
        this.fname!.OriginalFunction(fname, strPtr, findType);
        this.fnameCache[str] = (nint)fname;
        return fname;
    }

    public FName* FName(FName* name, nint str, EFindName findType)
    {
        var nameString = Marshal.PtrToStringUni(str) ?? string.Empty;

        if (!string.IsNullOrEmpty(nameString) && this.fnameAssigns.TryGetRedirect(nameString, out var newString))
        {
            // Honestly not sure why there's a bug here or why we have to manually
            // reuse previous FNames, when creating a FName should do that auomatically. Very weird.
            // I can't even move this code into FName(str), what???
            // Bug: str1 -> str2 || str2 -> str1 || str1 no longer works as expected.
            if (this.fnameCache.TryGetValue(nameString, out var cachedFname))
            {
                *name = *(FName*)cachedFname;
            }
            else
            {
                *name = *this.FName(newString);
            }
        }
        else
        {
            this.fname!.OriginalFunction(name, str, findType);
        }

        return name;
    }

    public string GetName(FName* name)
        => g_namePool->GetString(name->pool_location);

    public string GetName(FName name)
        => g_namePool->GetString(name.pool_location);

    public string GetName(uint poolLocation)
        => g_namePool->GetString(poolLocation);

    public FNamePool* GetPool() => g_namePool;

    private static nuint GetGlobalAddress(nint ptrAddress)
    {
        return (nuint)(*(int*)ptrAddress + ptrAddress + 4);
    }

    public FString FString(string str) => new(this, str);

    // CEREBRAL'S FNAME REVERSION
    public class AssignmentList : List<FNameAssign>
    {
        public bool TryAddRedirect(string originalName, string newName, IUnreal unreal)
        {
            if (ContainsRedirectFor(originalName)) // If the asset has already been redirected
            {
                this[originalName] = new(originalName, newName, unreal);
                return false; // No redirect has been added
            }
            Add(new(originalName, newName, unreal));
            return true; /// A redirect has been added
        }

        public bool TryGetRedirect(string originalName, [NotNullWhen(true)] out string? redirectName)
        {
            if (ContainsRedirectFor(originalName))
            {
                redirectName = Find(x => x.Original.Name == originalName).Assign.Name;
                return true;
            }
            redirectName = null;
            return false;
        }

        public FNameAssign this[string originalName]
        {
            get
            {
                TryGetRedirectFor(originalName, out var redirect);
                if (redirect == null)
                    throw new NullReferenceException(nameof(redirect));
                return redirect.Value;
            }
            set
            {
                TryGetRedirectFor(originalName, out var newRedirect);
                if (newRedirect == null)
                {
                    Add(value);
                    return;
                }
                int index = IndexOf(newRedirect.Value);
                this[index] = value;
                return;
            }
        }
        public bool ContainsRedirectFor(string originalName)
            => Exists(x => x.Original.Name == originalName);
        public bool ContainsRedirectTo(string newName)
            => Exists(x => x.Assign.Name == newName);
        public bool TryGetRedirectFor(string originalName, [NotNullWhen(true)] out FNameAssign? redirect)
        {
            redirect = null;
            if (ContainsRedirectFor(originalName))
            {
                redirect = Find(x => x.Original.Name == originalName);
                return true;
            }
            return false;
        }
        public bool TryGetRedirectTo(string newName, [NotNullWhen(true)] out FNameAssign? redirect)
        {
            redirect = null;
            if (ContainsRedirectTo(newName))
            {
                redirect = Find(x => x.Assign.Name == newName);
                return true;
            }
            return false;
        }
        public bool TryRemoveRedirectFor(string originalName)
        {
            var canRemove = TryGetRedirectFor(originalName, out var redirect);
            if (canRemove && redirect.HasValue)
            {
                Remove(redirect.Value);
                return true;
            }
            return false;
        }
        public bool TryRemoveRedirectTo(string newName)
        {
            var canRemove = TryGetRedirectTo(newName, out var redirect);
            if (canRemove && redirect.HasValue)
            {
                Remove(redirect.Value);
                return true;
            }
            return false;

        }
    }

    public readonly struct FNameAssign : IEquatable<FNameAssign>
    {
        public FNameWrapper Original { get; init; }
        public FNameWrapper Assign { get; init; }

        public FNameAssign(string name, string assignment, IUnreal unreal)
        {
            Original = new(name, unreal);
            Assign = new(assignment, unreal);
        }
        public class FNameWrapper : IEquatable<FNameWrapper?>
        {
            public FName* Self { get; init; }
            public string Name { get; init; }
            public nint Pointer { get; init; }
            public FNameWrapper(string name, IUnreal unreal)
            {
                this.Name = name;
                this.Self = unreal.FName(name); // Automatically malloc
                this.Pointer = StringsCache.GetStringPtrUni(name);
            }

            public override bool Equals(object? obj)
            {
                return Equals(obj as FNameWrapper);
            }

            public bool Equals(FNameWrapper? other)
            {
                return other is not null &&
                       Pointer.Equals(other.Pointer);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Pointer);
            }

            public static bool operator ==(FNameWrapper? left, FNameWrapper? right)
            {
                return EqualityComparer<FNameWrapper>.Default.Equals(left, right);
            }

            public static bool operator !=(FNameWrapper? left, FNameWrapper? right)
            {
                return !(left == right);
            }
        }


        public override string? ToString()
        {
            return Original.Name;
        }

        public override bool Equals(object? obj)
        {
            return obj is FNameAssign assign && Equals(assign);
        }

        public bool Equals(FNameAssign other)
        {
            return EqualityComparer<FNameWrapper>.Default.Equals(Original, other.Original) &&
                   EqualityComparer<FNameWrapper>.Default.Equals(Assign, other.Assign);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Original, Assign);
        }

        public static bool operator ==(FNameAssign left, FNameAssign right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FNameAssign left, FNameAssign right)
        {
            return !(left == right);
        }
    }
}
