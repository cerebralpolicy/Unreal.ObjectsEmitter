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
public unsafe class DataTable<TRow>
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
