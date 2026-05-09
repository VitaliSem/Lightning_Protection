using System;

public interface IAcadTransaction : IDisposable
{
    T GetObject<T>(object id, bool forWrite = false) where T : class;
    void AddEntity(object entity);
}