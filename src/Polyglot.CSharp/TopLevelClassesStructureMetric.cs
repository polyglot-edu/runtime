﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Polyglot.CSharp
{
    public class TopLevelClassesStructureMetric
    {
        private readonly CSharpParseOptions _parserOptions;
        public string Name => "topLevelClassesStructureMetric";

        public TopLevelClassesStructureMetric(SourceCodeKind sourceCodeKind = SourceCodeKind.Script)
        {
            _parserOptions = CSharpParseOptions.Default.WithKind(sourceCodeKind);
        }

        public object Calculate(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code, _parserOptions);
            var declarationWalker = new DeclarationWalker();

            declarationWalker.Visit(tree.GetRoot());
            var root = declarationWalker.DeclarationsRoot;
            return new List<ClassStructure>(root.NestedClasses);
        }

        public Task<object> CalculateAsync(string code)
        {
            return Task.FromResult<object>(Calculate(code));
        }
    }
}