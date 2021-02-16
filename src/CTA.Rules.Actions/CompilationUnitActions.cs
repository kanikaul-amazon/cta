﻿using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace CTA.Rules.Actions
{
    /// <summary>
    /// List of actions that can run on Compilation Units
    /// </summary>
    public class CompilationUnitActions
    {
        public Func<SyntaxGenerator, CompilationUnitSyntax, CompilationUnitSyntax> GetAddDirectiveAction(string @namespace)
        {
            CompilationUnitSyntax AddDirective(SyntaxGenerator syntaxGenerator, CompilationUnitSyntax node)
            {
                var allUsings = node.Usings;

                var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(@namespace)).NormalizeWhitespace();
                allUsings = allUsings.Add(usingDirective);

                node = node.WithUsings(allUsings).NormalizeWhitespace();
                return node;
            }
            return AddDirective;
        }

        public Func<SyntaxGenerator, CompilationUnitSyntax, CompilationUnitSyntax> GetRemoveDirectiveAction(string @namespace)
        {
            CompilationUnitSyntax RemoveDirective(SyntaxGenerator syntaxGenerator, CompilationUnitSyntax node)
            {
                var allUsings = node.Usings;
                var removeList = allUsings.Where(u => @namespace == u.Name.ToString());

                foreach (var item in removeList)
                {
                    allUsings = allUsings.Remove(item);
                }
                node = node.WithUsings(allUsings).NormalizeWhitespace();
                return node;
            }
            return RemoveDirective;
        }
    }
}
