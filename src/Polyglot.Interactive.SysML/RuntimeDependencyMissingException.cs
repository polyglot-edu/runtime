using System;
using System.Runtime.Serialization;

namespace Polyglot.Interactive.SysML;

[Serializable]
public class RuntimeDependencyMissingException : Exception
{
    public RuntimeDependencyMissingException()
    {
    }

    public RuntimeDependencyMissingException(string message) : base(message)
    {
    }

    public RuntimeDependencyMissingException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected RuntimeDependencyMissingException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}