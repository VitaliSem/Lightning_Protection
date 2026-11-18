// LP.Plugin/Acad/AcadDocumentAdapter.cs — concrete IAcadDocument / IAcadTransaction for LP.Core.
// C# 12, nullable, implicit usings; compiles for net48 and net8.0. Requires a ProjectReference to LP.Core.
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using LP.Core.Interfaces;

namespace LP.Plugin.Acad;

public sealed class AcadDocumentAdapter(Document doc) : IAcadDocument
{
    private readonly Document _doc = doc ?? throw new ArgumentNullException(nameof(doc));

    public string Name => _doc.Name;

    /// <summary>
    /// Runs the action in one transaction and commits it. The document is locked, so the adapter is safe from
    /// modeless UI (LP.UI forms, palettes) as well as from commands. An exception skips Commit -> rollback.
    /// </summary>
    public void ExecuteInTransaction(Action<IAcadTransaction> action)
    {
        using DocumentLock _ = _doc.LockDocument();
        using Transaction tr = _doc.Database.TransactionManager.StartTransaction();
        using var wrapper = new AcadTransactionAdapter(tr, _doc.Database);

        action(wrapper);
        tr.Commit();
    }

    /// <summary>Model space ObjectId boxed as object (LP.Core has no AutoCAD types).</summary>
    public object GetModelSpace() => SymbolUtilityServices.GetBlockModelSpaceId(_doc.Database);

    public void Regen() => _doc.Editor.Regen();
}

public sealed class AcadTransactionAdapter(Transaction tr, Database db) : IAcadTransaction
{
    /// <summary>id must be a boxed ObjectId. Returns null if the object is of another type.</summary>
    public T GetObject<T>(object id, bool forWrite = false) where T : class
    {
        if (id is not ObjectId objectId)
            throw new ArgumentException("Expected an Autodesk.AutoCAD.DatabaseServices.ObjectId.", nameof(id));

        // LP.Core is not nullable-aware, so the interface returns an oblivious T; callers must null-check.
        return (tr.GetObject(objectId, forWrite ? OpenMode.ForWrite : OpenMode.ForRead) as T)!;
    }

    /// <summary>entity must be a new Entity; it is appended to model space.</summary>
    public void AddEntity(object entity)
    {
        if (entity is not Entity ent)
            throw new ArgumentException("Expected an Autodesk.AutoCAD.DatabaseServices.Entity.", nameof(entity));

        PrimitivesHelper.AppendToModelSpace(tr, db, ent);
    }

    public void Dispose()
    {
        // The underlying Transaction is owned and disposed by AcadDocumentAdapter.
    }
}
