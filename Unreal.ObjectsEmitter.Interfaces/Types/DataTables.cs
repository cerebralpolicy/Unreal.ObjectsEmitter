using System.Collections;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace Unreal.ObjectsEmitter.Interfaces.Types;

/// <summary>
/// DataTable instance with pointer rows.
/// </summary>
public unsafe class DataTable
{
    /// <summary>
    /// DataTable constructor.
    /// </summary>
    /// <param name="name">Table name.</param>
    /// <param name="obj">Table object pointer.</param>
    /// <param name="rows">Rows array.</param>
    public DataTable(string name, UDataTable* obj, Row[] rows)
    {
        this.Name = name;
        this.Self = obj;
        this.Rows = rows;
    }

    /// <summary>
    /// Gets table name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets table object pointer.
    /// </summary>
    public UDataTable* Self { get; }

    /// <summary>
    /// Gets table rows.
    /// </summary>
    public Row[] Rows { get; }
}

/// <summary>
/// DataTable row entry with pointer reference.
/// </summary>
public unsafe class Row
{
    /// <summary>
    /// Row constructor.
    /// </summary>
    /// <param name="name">Row name.</param>
    /// <param name="obj">Row object pointer.</param>
    public Row(string name, UObject* obj)
    {
        this.Name = name;
        this.Self = obj;
    }

    /// <summary>
    /// Gets row name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets row object pointer.
    /// </summary>
    public UObject* Self { get; }
}

/// <summary>
/// DataTable instance with typed row pointers.
/// </summary>
/// <typeparam name="TRow">Row type.</typeparam>
public unsafe class DataTable<TRow> : IDictionary<string, TRow>
    where TRow : unmanaged
{
    /// <summary>
    /// DataTable constructor.
    /// </summary>
    /// <param name="name">Table name.</param>
    /// <param name="obj">Table object pointer.</param>
    /// <param name="rows">Table rows.</param>
    public DataTable(string name, UDataTable* obj, Row<TRow>[] rows)
    {
        this.Name = name;
        this.Self = obj;
        this.Rows = rows;
    }

    /// <inheritdoc/>
    public TRow this[string key]
    {
        get => *this.Rows.First(x => x.Name == key).Self;
        set => *this.Rows.First(x => x.Name == key).Self = value;
    }

    /// <summary>
    /// Gets table name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets table object pointer.
    /// </summary>
    public UDataTable* Self { get; }

    /// <summary>
    /// Gets table rows.
    /// </summary>
    public Row<TRow>[] Rows { get; }

    /// <inheritdoc/>
    public ICollection<string> Keys => this.Rows.Select(x => x.Name).ToArray();

    /// <inheritdoc/>
    public ICollection<TRow> Values => this.Rows.Select(x => *x.Self).ToArray();

    /// <inheritdoc/>
    public int Count => this.Rows.Count();

    /// <inheritdoc/>
    public bool IsReadOnly { get; } = true;

    /// <inheritdoc/>
    public void Add(string key, TRow value)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void Add(KeyValuePair<string, TRow> item)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void Clear()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public bool Contains(KeyValuePair<string, TRow> item)
        => this.Rows.FirstOrDefault(x => x.Name == item.Key && (*x.Self).Equals(item.Value)) != null;

    /// <inheritdoc/>
    public bool ContainsKey(string key) => this.Rows.FirstOrDefault(x => x.Name == key) != null;

    /// <inheritdoc/>
    public void CopyTo(KeyValuePair<string, TRow>[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, TRow>> GetEnumerator()
        => this.Rows.Select(x => new KeyValuePair<string, TRow>(x.Name, *x.Self)).GetEnumerator();

    /// <inheritdoc/>
    public bool Remove(string key)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public bool Remove(KeyValuePair<string, TRow> item)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out TRow value)
    {
        var row = this.Rows.FirstOrDefault(x => x.Name == key);
        if (row == null)
        {
            value = default;
            return false;
        }

        value = *row.Self;
        return true;
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}

/// <summary>
/// DataTable row with typed self pointer.
/// </summary>
/// <typeparam name="TRow">Row type.</typeparam>
public unsafe class Row<TRow>
    where TRow : unmanaged
{
    /// <summary>
    /// Row constructor.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="rowPtr"></param>
    public Row(string name, TRow* rowPtr)
    {
        this.Name= name;
        this.Self = rowPtr;
    }

    /// <summary>
    /// Gets row name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets row pointer.
    /// </summary>
    public TRow* Self { get; }
}
