﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Polyglot.CSharp
{
    public record ClassStructure(string Name, DeclarationContextKind Kind, IEnumerable<string> Modifiers, IEnumerable<FieldStructure> Fields, IEnumerable<MethodStructure> Methods, IEnumerable<ConstructorStructure> Constructors, IEnumerable<ClassStructure> NestedClasses)
    {
        public virtual bool Equals(ClassStructure other)
        {
            return new ClassStructureComparer().Equals(this, other);
        }
    }

    public record VariableStructure(string Name, DeclarationContextKind Kind, string Type)
    {
        public virtual bool Equals(VariableStructure other)
        {
            return new VariableStructureComparer().Equals(this, other);
        }
    }

    public record FieldStructure(VariableStructure Variable, IEnumerable<string> Modifiers)
    {
        // uses Variable.Kind to avoid duplicates
        public DeclarationContextKind Kind => Variable.Kind;

        public virtual bool Equals(FieldStructure other)
        {
            return new FieldStructureComparer().Equals(this, other);
        }
    }

    public record MethodStructure(string Name, DeclarationContextKind Kind, string ReturnType, IEnumerable<string> Modifiers, IEnumerable<VariableStructure> Parameters, MethodBodyStructure Body)
    {
        public virtual bool Equals(MethodStructure other)
        {
            return new MethodStructureComparer().Equals(this, other);
        }
    }

    public record ConstructorStructure(IEnumerable<VariableStructure> Parameters)
    {
        public DeclarationContextKind Kind => DeclarationContextKind.Type;

        public virtual bool Equals(ConstructorStructure other)
        {
            return new ConstructorStructureComparer().Equals(this, other);
        }
    }

    public record MethodBodyStructure(IEnumerable<VariableStructure> LocalVariables)
    {
        public DeclarationContextKind Kind => DeclarationContextKind.Method;

        public virtual bool Equals(MethodBodyStructure other)
        {
            return new MethodBodyStructureComparer().Equals(this, other);
        }
    }



    internal class ClassStructureComparer : IEqualityComparer<ClassStructure>
    {
        public bool Equals(ClassStructure x, ClassStructure y)
        {
            return x.Name == y.Name
                && x.Kind == y.Kind
                && x.Modifiers.SequenceEqual(y.Modifiers)
                && x.Fields.SequenceEqual(y.Fields, new FieldStructureComparer())
                && x.Methods.SequenceEqual(y.Methods, new MethodStructureComparer())
                && x.Constructors.SequenceEqual(y.Constructors, new ConstructorStructureComparer())
                && x.NestedClasses.SequenceEqual(y.NestedClasses, new ClassStructureComparer());
        }

        public int GetHashCode([DisallowNull] ClassStructure obj)
        {
            throw new NotImplementedException();
        }
    }

    internal class VariableStructureComparer : IEqualityComparer<VariableStructure>
    {
        public bool Equals(VariableStructure x, VariableStructure y)
        {
            return x.Name == y.Name
                && x.Kind == y.Kind
                && x.Type == y.Type;
        }

        public int GetHashCode([DisallowNull] VariableStructure obj)
        {
            throw new NotImplementedException();
        }
    }

    internal class FieldStructureComparer : IEqualityComparer<FieldStructure>
    {
        public bool Equals(FieldStructure x, FieldStructure y)
        {
            return x.Variable.Equals(y.Variable)
                && x.Kind == y.Kind
                && x.Modifiers.SequenceEqual(y.Modifiers);
        }

        public int GetHashCode([DisallowNull] FieldStructure obj)
        {
            throw new NotImplementedException();
        }
    }

    internal class MethodStructureComparer : IEqualityComparer<MethodStructure>
    {
        public bool Equals(MethodStructure x, MethodStructure y)
        {
            return x.Name == y.Name
                && x.Kind == y.Kind
                && x.ReturnType == y.ReturnType
                && x.Modifiers.SequenceEqual(y.Modifiers)
                && x.Parameters.SequenceEqual(y.Parameters, new VariableStructureComparer())
                && x.Body.Equals(y.Body);
        }

        public int GetHashCode([DisallowNull] MethodStructure obj)
        {
            throw new NotImplementedException();
        }
    }

    internal class ConstructorStructureComparer : IEqualityComparer<ConstructorStructure>
    {
        public bool Equals(ConstructorStructure x, ConstructorStructure y)
        {
            return x.Parameters.SequenceEqual(y.Parameters, new VariableStructureComparer())
                && x.Kind == y.Kind;
        }

        public int GetHashCode([DisallowNull] ConstructorStructure obj)
        {
            throw new NotImplementedException();
        }
    }
    
    internal class MethodBodyStructureComparer : IEqualityComparer<MethodBodyStructure>
    {
        public bool Equals(MethodBodyStructure x, MethodBodyStructure y)
        {
            return x.LocalVariables.SequenceEqual(y.LocalVariables, new VariableStructureComparer())
                && x.Kind == y.Kind;
        }

        public int GetHashCode([DisallowNull] MethodBodyStructure obj)
        {
            throw new NotImplementedException();
        }
    }
}