using System;

public interface IAcadDocument
{
    string Name { get; }
    void ExecuteInTransaction(Action<IAcadTransaction> action);
    object GetModelSpace();
    void Regen();
}