﻿namespace Frontend
{
    public interface IEntityRegistry
    {
        string Default { get; }
        int this[string id] { get; }
    }
}