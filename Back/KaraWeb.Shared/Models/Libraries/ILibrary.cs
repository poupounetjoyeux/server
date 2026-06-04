using System;

namespace KaraWeb.Shared.Models.Libraries
{
    public interface ILibrary
    {
        Guid Id { get; }
        string Name { get; }
        string Description { get; }
        string Path { get; }
    }
}